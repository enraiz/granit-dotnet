using Granit.Core.Modularity;

namespace Granit.BlobStorage.EntityFrameworkCore;

/// <summary>
/// Granit module for EF Core persistence of blob descriptors.
/// Registers <c>BlobStorageDbContext</c> and <c>EfBlobDescriptorStore</c>.
/// </summary>
[DependsOn(typeof(GranitBlobStorageModule))]
public sealed class GranitBlobStorageEntityFrameworkCoreModule : GranitModule;
