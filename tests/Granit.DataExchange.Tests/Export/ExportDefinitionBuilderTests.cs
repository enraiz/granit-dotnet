using Granit.DataExchange.Export;
using Shouldly;
using Xunit;

namespace Granit.DataExchange.Tests.Export;

public sealed class ExportDefinitionBuilderTests
{
    // ---- Simple Field ------------------------------------------------

    [Fact]
    public void Field_simple_property_adds_descriptor()
    {
        // Arrange
        ExportDefinitionBuilder<TestEntity> builder = new();

        // Act
        builder.Field(e => e.Name);

        // Assert
        builder.Fields.Count.ShouldBe(1);
        ExportFieldDescriptor field = builder.Fields[0];
        field.PropertyPath.ShouldBe("Name");
        field.ClrTypeName.ShouldBe("String");
        field.Header.ShouldBeNull();
        field.Format.ShouldBeNull();
        field.IsNavigation.ShouldBeFalse();
    }

    [Fact]
    public void Field_with_header_sets_header()
    {
        ExportDefinitionBuilder<TestEntity> builder = new();

        builder.Field(e => e.Name, f => f.Header("Nom"));

        builder.Fields[0].Header.ShouldBe("Nom");
    }

    [Fact]
    public void Field_with_format_sets_format()
    {
        ExportDefinitionBuilder<TestEntity> builder = new();

        builder.Field(e => e.BirthDate, f => f.Format("dd/MM/yyyy"));

        builder.Fields[0].Format.ShouldBe("dd/MM/yyyy");
    }

    [Fact]
    public void Field_with_order_sets_explicit_order()
    {
        ExportDefinitionBuilder<TestEntity> builder = new();

        builder.Field(e => e.Name, f => f.Order(10));

        builder.Fields[0].Order.ShouldBe(10);
    }

    [Fact]
    public void Fields_auto_increment_order()
    {
        ExportDefinitionBuilder<TestEntity> builder = new();

        builder.Field(e => e.Name);
        builder.Field(e => e.Email);

        builder.Fields[0].Order.ShouldBe(0);
        builder.Fields[1].Order.ShouldBe(1);
    }

    // ---- Navigation Field --------------------------------------------

    [Fact]
    public void Field_navigation_uses_dot_notation()
    {
        ExportDefinitionBuilder<TestEntity> builder = new();

        builder.Field(e => e.Company, c => c.Name);

        builder.Fields[0].PropertyPath.ShouldBe("Company.Name");
        builder.Fields[0].IsNavigation.ShouldBeTrue();
    }

    [Fact]
    public void Field_navigation_with_header()
    {
        ExportDefinitionBuilder<TestEntity> builder = new();

        builder.Field(e => e.Company, c => c.Name, f => f.Header("Société"));

        builder.Fields[0].Header.ShouldBe("Société");
        builder.Fields[0].IsNavigation.ShouldBeTrue();
    }

    // ---- IncludeId / IncludeBusinessKey ------------------------------

    [Fact]
    public void IncludeId_sets_flag()
    {
        ExportDefinitionBuilder<TestEntity> builder = new();

        builder.IncludeId();

        builder.IncludeIdFlag.ShouldBeTrue();
    }

    [Fact]
    public void IncludeBusinessKey_sets_flag()
    {
        ExportDefinitionBuilder<TestEntity> builder = new();

        builder.IncludeBusinessKey();

        builder.IncludeBusinessKeyFlag.ShouldBeTrue();
    }

    // ---- Fluent chaining ---------------------------------------------

    [Fact]
    public void Fluent_chaining_builds_multiple_fields()
    {
        ExportDefinitionBuilder<TestEntity> builder = new();

        builder
            .IncludeId()
            .IncludeBusinessKey()
            .Field(e => e.Name, f => f.Header("Nom"))
            .Field(e => e.Email)
            .Field(e => e.BirthDate, f => f.Header("Date de naissance").Format("dd/MM/yyyy"))
            .Field(e => e.Company, c => c.Name, f => f.Header("Société"));

        builder.Fields.Count.ShouldBe(4);
        builder.IncludeIdFlag.ShouldBeTrue();
        builder.IncludeBusinessKeyFlag.ShouldBeTrue();
        builder.Fields[0].PropertyPath.ShouldBe("Name");
        builder.Fields[1].PropertyPath.ShouldBe("Email");
        builder.Fields[2].PropertyPath.ShouldBe("BirthDate");
        builder.Fields[3].PropertyPath.ShouldBe("Company.Name");
    }

    // ---- ExportDefinition integration --------------------------------

    [Fact]
    public void ExportDefinition_GetFields_returns_configured_fields()
    {
        TestExportDefinition definition = new();

        IReadOnlyList<ExportFieldDescriptor> fields = definition.GetFields();

        fields.Count.ShouldBe(3);
        fields[0].PropertyPath.ShouldBe("Name");
        fields[1].PropertyPath.ShouldBe("Email");
        fields[2].PropertyPath.ShouldBe("Company.Name");
    }

    [Fact]
    public void ExportDefinition_properties()
    {
        TestExportDefinition definition = new();

        definition.Name.ShouldBe("Test.Export");
        definition.EntityType.ShouldBe(typeof(TestEntity));
        definition.QueryDefinitionName.ShouldBeNull();
        definition.SupportedFormats.ShouldBe(["xlsx", "csv"]);
    }

    // ---- Test helpers ------------------------------------------------

    private sealed class TestEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateOnly? BirthDate { get; set; }
        public TestCompany? Company { get; set; }
    }

    private sealed class TestCompany
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestExportDefinition : ExportDefinition<TestEntity>
    {
        public override string Name => "Test.Export";

        protected override void Configure(ExportDefinitionBuilder<TestEntity> builder) =>
            builder
                .Field(e => e.Name, f => f.Header("Nom"))
                .Field(e => e.Email)
                .Field(e => e.Company, c => c.Name, f => f.Header("Société"));
    }
}
