using Granit.Core.Domain;
using Shouldly;
using Xunit;

namespace Granit.ReferenceData.Tests;

public sealed class ReferenceDataEntityTests
{
    /// <summary>Concrete test entity to instantiate the abstract base class.</summary>
    private sealed class TestEntity : ReferenceDataEntity;

    [Fact]
    public void Inherits_AuditedEntity()
    {
        TestEntity entity = new();

        entity.ShouldBeAssignableTo<AuditedEntity>();
    }

    [Fact]
    public void Implements_IActive()
    {
        TestEntity entity = new();

        entity.ShouldBeAssignableTo<IActive>();
    }

    [Fact]
    public void Default_IsActive_Is_True()
    {
        TestEntity entity = new();

        entity.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Default_Code_Is_Empty()
    {
        TestEntity entity = new();

        entity.Code.ShouldBe(string.Empty);
    }

    [Fact]
    public void Default_Label_Is_Empty()
    {
        TestEntity entity = new();

        entity.Label.ShouldBe(string.Empty);
    }

    [Fact]
    public void Default_SortOrder_Is_Zero()
    {
        TestEntity entity = new();

        entity.SortOrder.ShouldBe(0);
    }

    [Fact]
    public void Default_ValidFrom_Is_Null()
    {
        TestEntity entity = new();

        entity.ValidFrom.ShouldBeNull();
    }

    [Fact]
    public void Default_ValidTo_Is_Null()
    {
        TestEntity entity = new();

        entity.ValidTo.ShouldBeNull();
    }

    [Fact]
    public void Has_Guid_Id_From_Entity_Base()
    {
        TestEntity entity = new();

        entity.Id.ShouldBeOfType<Guid>();
    }

    [Fact]
    public void Properties_Are_Settable()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        TestEntity entity = new()
        {
            Code = "BE",
            Label = "Belgium",
            IsActive = false,
            SortOrder = 42,
            ValidFrom = now,
            ValidTo = now.AddYears(1)
        };

        entity.Code.ShouldBe("BE");
        entity.Label.ShouldBe("Belgium");
        entity.IsActive.ShouldBeFalse();
        entity.SortOrder.ShouldBe(42);
        entity.ValidFrom.ShouldBe(now);
        entity.ValidTo.ShouldBe(now.AddYears(1));
    }
}
