using Granit.BlobStorage.EntityFrameworkCore.Internal;
using Granit.Core.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Granit.BlobStorage.EntityFrameworkCore.Tests;

public sealed class EfBlobDescriptorStoreTests
{
    // =========================================================================
    // Test infrastructure
    // =========================================================================

    private sealed class InMemoryContextFactory(string dbName, ICurrentTenant? currentTenant = null)
        : IDbContextFactory<BlobStorageDbContext>
    {
        public BlobStorageDbContext CreateDbContext()
        {
            DbContextOptions<BlobStorageDbContext> options =
                new DbContextOptionsBuilder<BlobStorageDbContext>()
                    .UseInMemoryDatabase(dbName)
                    .Options;
            return new BlobStorageDbContext(options, currentTenant);
        }

        public Task<BlobStorageDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }

    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherTenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static ICurrentTenant MakeTenant(Guid id)
    {
        ICurrentTenant tenant = Substitute.For<ICurrentTenant>();
        tenant.IsAvailable.Returns(true);
        tenant.Id.Returns(id);
        return tenant;
    }

    private static EfBlobDescriptorStore CreateStore(string dbName, Guid? tenantId = null) =>
        new(new InMemoryContextFactory(dbName, MakeTenant(tenantId ?? TenantId)));

    private static BlobDescriptor MakeDescriptor(
        Guid? id = null,
        Guid? tenantId = null,
        string containerName = "prescriptions")
    {
        Guid tid = tenantId ?? TenantId;
        Guid bid = id ?? Guid.NewGuid();
        return BlobDescriptor.Create(
            id: bid,
            tenantId: tid,
            containerName: containerName,
            objectKey: $"{tid}/{containerName}/2026/02/{bid}",
            request: new BlobUploadRequest("prescription.pdf", "application/pdf", 5_000_000L),
            createdAt: new DateTimeOffset(2026, 2, 23, 10, 0, 0, TimeSpan.Zero));
    }

    // =========================================================================
    // FindAsync — not found
    // =========================================================================

    [Fact]
    public async Task FindAsync_UnknownBlob_ReturnsNull()
    {
        EfBlobDescriptorStore store = CreateStore(Guid.NewGuid().ToString());

        BlobDescriptor? result = await store.FindAsync(
            Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    // =========================================================================
    // SaveAsync + FindAsync — roundtrip
    // =========================================================================

    [Fact]
    public async Task SaveAsync_PersistsDescriptor_CanBeRetrievedByBlobId()
    {
        string db = Guid.NewGuid().ToString();
        EfBlobDescriptorStore store = CreateStore(db);
        var blobId = Guid.NewGuid();
        BlobDescriptor descriptor = MakeDescriptor(id: blobId);

        await store.SaveAsync(descriptor, TestContext.Current.CancellationToken);

        BlobDescriptor? retrieved = await store.FindAsync(
            blobId, TestContext.Current.CancellationToken);
        retrieved.ShouldNotBeNull();
        retrieved!.Id.ShouldBe(blobId);
        retrieved.Status.ShouldBe(BlobStatus.Pending);
        retrieved.OriginalFileName.ShouldBe("prescription.pdf");
        retrieved.DeclaredContentType.ShouldBe("application/pdf");
    }

    [Fact]
    public async Task SaveAsync_PreservesAllMandatoryFields()
    {
        string db = Guid.NewGuid().ToString();
        EfBlobDescriptorStore store = CreateStore(db);
        var blobId = Guid.NewGuid();
        DateTimeOffset createdAt = new(2026, 2, 23, 10, 0, 0, TimeSpan.Zero);
        var descriptor = BlobDescriptor.Create(
            id: blobId,
            tenantId: TenantId,
            containerName: "medical-images",
            objectKey: $"{TenantId}/medical-images/2026/02/{blobId}",
            request: new BlobUploadRequest("scan.dcm", "application/dicom", 20_000_000L),
            createdAt: createdAt);

        await store.SaveAsync(descriptor, TestContext.Current.CancellationToken);

        BlobDescriptor? retrieved = await store.FindAsync(
            blobId, TestContext.Current.CancellationToken);
        retrieved!.TenantId.ShouldBe(TenantId);
        retrieved.ContainerName.ShouldBe("medical-images");
        retrieved.ObjectKey.ShouldBe($"{TenantId}/medical-images/2026/02/{blobId}");
        retrieved.OriginalFileName.ShouldBe("scan.dcm");
        retrieved.DeclaredContentType.ShouldBe("application/dicom");
        retrieved.CreatedAt.ShouldBe(createdAt);
    }

    // =========================================================================
    // UpdateAsync — state transitions
    // =========================================================================

    [Fact]
    public async Task UpdateAsync_AfterMarkAsUploading_PersistsNewStatus()
    {
        string db = Guid.NewGuid().ToString();
        EfBlobDescriptorStore store = CreateStore(db);
        var blobId = Guid.NewGuid();
        await store.SaveAsync(MakeDescriptor(id: blobId), TestContext.Current.CancellationToken);

        BlobDescriptor? descriptor = await store.FindAsync(
            blobId, TestContext.Current.CancellationToken);
        descriptor!.MarkAsUploading();
        await store.UpdateAsync(descriptor, TestContext.Current.CancellationToken);

        BlobDescriptor? updated = await store.FindAsync(
            blobId, TestContext.Current.CancellationToken);
        updated!.Status.ShouldBe(BlobStatus.Uploading);
    }

    [Fact]
    public async Task UpdateAsync_AfterMarkAsValid_PersistsVerifiedTypeAndSize()
    {
        string db = Guid.NewGuid().ToString();
        EfBlobDescriptorStore store = CreateStore(db);
        var blobId = Guid.NewGuid();
        await store.SaveAsync(MakeDescriptor(id: blobId), TestContext.Current.CancellationToken);

        BlobDescriptor? descriptor = await store.FindAsync(
            blobId, TestContext.Current.CancellationToken);
        descriptor!.MarkAsUploading();
        await store.UpdateAsync(descriptor, TestContext.Current.CancellationToken);

        BlobDescriptor? uploading = await store.FindAsync(
            blobId, TestContext.Current.CancellationToken);
        DateTimeOffset validatedAt = new(2026, 2, 23, 10, 5, 0, TimeSpan.Zero);
        uploading!.MarkAsValid("application/pdf", 204_800L, validatedAt);
        await store.UpdateAsync(uploading, TestContext.Current.CancellationToken);

        BlobDescriptor? valid = await store.FindAsync(
            blobId, TestContext.Current.CancellationToken);
        valid!.Status.ShouldBe(BlobStatus.Valid);
        valid.VerifiedContentType.ShouldBe("application/pdf");
        valid.SizeBytes.ShouldBe(204_800L);
        valid.ValidatedAt.ShouldBe(validatedAt);
    }

    [Fact]
    public async Task UpdateAsync_AfterMarkAsRejected_PersistsRejectionReason()
    {
        string db = Guid.NewGuid().ToString();
        EfBlobDescriptorStore store = CreateStore(db);
        var blobId = Guid.NewGuid();
        await store.SaveAsync(MakeDescriptor(id: blobId), TestContext.Current.CancellationToken);

        BlobDescriptor? descriptor = await store.FindAsync(
            blobId, TestContext.Current.CancellationToken);
        descriptor!.MarkAsUploading();
        await store.UpdateAsync(descriptor, TestContext.Current.CancellationToken);

        BlobDescriptor? uploading = await store.FindAsync(
            blobId, TestContext.Current.CancellationToken);
        uploading!.MarkAsRejected("Invalid magic bytes: expected PDF signature.");
        await store.UpdateAsync(uploading, TestContext.Current.CancellationToken);

        BlobDescriptor? rejected = await store.FindAsync(
            blobId, TestContext.Current.CancellationToken);
        rejected!.Status.ShouldBe(BlobStatus.Rejected);
        rejected.RejectionReason.ShouldBe("Invalid magic bytes: expected PDF signature.");
    }

    [Fact]
    public async Task UpdateAsync_AfterMarkAsDeleted_PreservesAuditRecordInDatabase()
    {
        string db = Guid.NewGuid().ToString();
        EfBlobDescriptorStore store = CreateStore(db);
        var blobId = Guid.NewGuid();
        await store.SaveAsync(MakeDescriptor(id: blobId), TestContext.Current.CancellationToken);

        // Advance through Pending -> Uploading -> Valid -> Deleted.
        BlobDescriptor? pending = await store.FindAsync(blobId, TestContext.Current.CancellationToken);
        pending!.MarkAsUploading();
        await store.UpdateAsync(pending, TestContext.Current.CancellationToken);

        BlobDescriptor? uploading = await store.FindAsync(blobId, TestContext.Current.CancellationToken);
        uploading!.MarkAsValid("application/pdf", 512L, DateTimeOffset.UtcNow);
        await store.UpdateAsync(uploading, TestContext.Current.CancellationToken);

        BlobDescriptor? valid = await store.FindAsync(blobId, TestContext.Current.CancellationToken);
        DateTimeOffset deletedAt = new(2026, 2, 23, 12, 0, 0, TimeSpan.Zero);
        valid!.MarkAsDeleted(deletedAt, "RGPD Art. 17 erasure request");
        await store.UpdateAsync(valid, TestContext.Current.CancellationToken);

        // RGPD / HDS: the audit row must remain in the database after deletion.
        BlobDescriptor? deleted = await store.FindAsync(blobId, TestContext.Current.CancellationToken);
        deleted.ShouldNotBeNull("HDS requires the audit record to be retained for 3 years");
        deleted!.Status.ShouldBe(BlobStatus.Deleted);
        deleted.DeletedAt.ShouldBe(deletedAt);
        deleted.DeletionReason.ShouldBe("RGPD Art. 17 erasure request");
    }

    // =========================================================================
    // Tenant isolation
    // =========================================================================

    [Fact]
    public async Task FindAsync_DoesNotReturnDescriptorBelongingToAnotherTenant()
    {
        string db = Guid.NewGuid().ToString();
        // Save a descriptor for OtherTenant.
        EfBlobDescriptorStore storeOther = CreateStore(db, OtherTenantId);
        var blobId = Guid.NewGuid();
        BlobDescriptor descriptorOther = MakeDescriptor(
            id: blobId, tenantId: OtherTenantId);
        await storeOther.SaveAsync(descriptorOther, TestContext.Current.CancellationToken);

        // Attempt to retrieve it as TenantId (current tenant) -> must return null.
        EfBlobDescriptorStore storeTenant = CreateStore(db, TenantId);
        BlobDescriptor? result = await storeTenant.FindAsync(
            blobId, TestContext.Current.CancellationToken);

        result.ShouldBeNull("cross-tenant access must be blocked at the store level");
    }

    // =========================================================================
    // No active tenant
    // =========================================================================

    [Fact]
    public async Task FindAsync_WithNoActiveTenant_ReturnsNull()
    {
        ICurrentTenant noTenant = Substitute.For<ICurrentTenant>();
        noTenant.IsAvailable.Returns(false);
        noTenant.Id.Returns((Guid?)null);
        EfBlobDescriptorStore store = new(
            new InMemoryContextFactory(Guid.NewGuid().ToString(), noTenant));

        BlobDescriptor? result = await store.FindAsync(
            Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.ShouldBeNull("no blob exists with matching TenantId");
    }
}
