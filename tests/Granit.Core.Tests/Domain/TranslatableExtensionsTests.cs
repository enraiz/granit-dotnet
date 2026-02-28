using FluentAssertions;
using Granit.Core.Domain;
using Xunit;

namespace Granit.Core.Tests.Domain;

public sealed class TranslatableExtensionsTests
{
    // -------------------------------------------------------------------------
    // Test entities
    // -------------------------------------------------------------------------

    private sealed class TestParent : Entity, ITranslatable<TestTranslation>
    {
        public ICollection<TestTranslation> Translations { get; set; } = [];
    }

    private sealed class TestTranslation : Translation<TestParent>
    {
        public string Title { get; set; } = string.Empty;
    }

    private sealed class TestAuditedParent : AuditedEntity, ITranslatable<TestAuditedTranslation>
    {
        public ICollection<TestAuditedTranslation> Translations { get; set; } = [];
    }

    private sealed class TestAuditedTranslation : AuditedTranslation<TestAuditedParent>
    {
        public string Title { get; set; } = string.Empty;
    }

    // -------------------------------------------------------------------------
    // Hierarchy
    // -------------------------------------------------------------------------

    [Fact]
    public void Translation_InheritsFromEntity() =>
        new TestTranslation().Should().BeAssignableTo<Entity>();

    [Fact]
    public void Translation_ImplementsITranslation() =>
        new TestTranslation().Should().BeAssignableTo<ITranslation>();

    [Fact]
    public void Translation_ImplementsITranslationOfParent() =>
        new TestTranslation().Should().BeAssignableTo<ITranslation<TestParent>>();

    [Fact]
    public void AuditedTranslation_InheritsFromAuditedEntity() =>
        new TestAuditedTranslation().Should().BeAssignableTo<AuditedEntity>();

    [Fact]
    public void AuditedTranslation_ImplementsITranslation() =>
        new TestAuditedTranslation().Should().BeAssignableTo<ITranslation>();

    [Fact]
    public void AuditedTranslation_ImplementsITranslationOfParent() =>
        new TestAuditedTranslation().Should().BeAssignableTo<ITranslation<TestAuditedParent>>();

    // -------------------------------------------------------------------------
    // Default values
    // -------------------------------------------------------------------------

    [Fact]
    public void Translation_DefaultValues_AreCorrect()
    {
        TestTranslation translation = new();

        translation.Id.Should().Be(Guid.Empty);
        translation.ParentId.Should().Be(Guid.Empty);
        translation.Culture.Should().BeEmpty();
        translation.Parent.Should().BeNull();
    }

    [Fact]
    public void AuditedTranslation_DefaultValues_IncludeAuditFields()
    {
        TestAuditedTranslation translation = new();

        translation.Id.Should().Be(Guid.Empty);
        translation.ParentId.Should().Be(Guid.Empty);
        translation.Culture.Should().BeEmpty();
        translation.CreatedAt.Should().Be(default);
        translation.CreatedBy.Should().BeEmpty();
        translation.ModifiedAt.Should().BeNull();
        translation.ModifiedBy.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // GetTranslation — exact match
    // -------------------------------------------------------------------------

    [Fact]
    public void GetTranslation_ExactCultureMatch_ReturnsExact()
    {
        TestParent entity = CreateEntityWithTranslations("fr", "en");

        TestTranslation? result = entity.GetTranslation("fr");

        result.Should().NotBeNull();
        result!.Culture.Should().Be("fr");
    }

    [Fact]
    public void GetTranslation_ExactCultureMatch_CaseInsensitive()
    {
        TestParent entity = CreateEntityWithTranslations("fr-BE", "en");

        TestTranslation? result = entity.GetTranslation("FR-BE");

        result.Should().NotBeNull();
        result!.Culture.Should().Be("fr-BE");
    }

    // -------------------------------------------------------------------------
    // GetTranslation — parent culture fallback
    // -------------------------------------------------------------------------

    [Fact]
    public void GetTranslation_ParentCultureFallback_ReturnsFr()
    {
        // fr-BE demandé, seul fr disponible → fallback vers fr
        TestParent entity = CreateEntityWithTranslations("fr", "en");

        TestTranslation? result = entity.GetTranslation("fr-BE");

        result.Should().NotBeNull();
        result!.Culture.Should().Be("fr");
    }

    // -------------------------------------------------------------------------
    // GetTranslation — default culture fallback
    // -------------------------------------------------------------------------

    [Fact]
    public void GetTranslation_DefaultCultureFallback_ReturnsEn()
    {
        // de demandé, pas de de ni parent, fallback vers en (défaut)
        TestParent entity = CreateEntityWithTranslations("fr", "en");

        TestTranslation? result = entity.GetTranslation("de");

        result.Should().NotBeNull();
        result!.Culture.Should().Be("en");
    }

    [Fact]
    public void GetTranslation_CustomDefaultCulture_ReturnsFr()
    {
        TestParent entity = CreateEntityWithTranslations("fr", "nl");

        TestTranslation? result = entity.GetTranslation("de", defaultCulture: "fr");

        result.Should().NotBeNull();
        result!.Culture.Should().Be("fr");
    }

    // -------------------------------------------------------------------------
    // GetTranslation — first available fallback
    // -------------------------------------------------------------------------

    [Fact]
    public void GetTranslation_NoDefaultCulture_ReturnsFirstAvailable()
    {
        // de demandé, pas de de ni en, retourne la première disponible
        TestParent entity = CreateEntityWithTranslations("fr", "nl");

        TestTranslation? result = entity.GetTranslation("de");

        result.Should().NotBeNull();
        result!.Culture.Should().BeOneOf("fr", "nl");
    }

    // -------------------------------------------------------------------------
    // GetTranslation — empty collection
    // -------------------------------------------------------------------------

    [Fact]
    public void GetTranslation_EmptyCollection_ReturnsNull()
    {
        TestParent entity = new();

        TestTranslation? result = entity.GetTranslation("fr");

        result.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // GetTranslation — strict mode (useFallback: false)
    // -------------------------------------------------------------------------

    [Fact]
    public void GetTranslation_Strict_ExactMatch_ReturnsExact()
    {
        TestParent entity = CreateEntityWithTranslations("fr", "en");

        TestTranslation? result = entity.GetTranslation("fr", useFallback: false);

        result.Should().NotBeNull();
        result!.Culture.Should().Be("fr");
    }

    [Fact]
    public void GetTranslation_Strict_NoMatch_ReturnsNull()
    {
        TestParent entity = CreateEntityWithTranslations("fr", "en");

        TestTranslation? result = entity.GetTranslation("de", useFallback: false);

        result.Should().BeNull();
    }

    [Fact]
    public void GetTranslation_Strict_ParentCultureExists_ReturnsNull()
    {
        // fr-BE demandé en mode strict, seul fr disponible → pas de fallback
        TestParent entity = CreateEntityWithTranslations("fr", "en");

        TestTranslation? result = entity.GetTranslation("fr-BE", useFallback: false);

        result.Should().BeNull();
    }

    [Fact]
    public void GetTranslation_Strict_EmptyCollection_ReturnsNull()
    {
        TestParent entity = new();

        TestTranslation? result = entity.GetTranslation("fr", useFallback: false);

        result.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // GetTranslation — same culture as default does not cause double lookup
    // -------------------------------------------------------------------------

    [Fact]
    public void GetTranslation_CultureSameAsDefault_ReturnsCorrectly()
    {
        TestParent entity = CreateEntityWithTranslations("en");

        TestTranslation? result = entity.GetTranslation("en");

        result.Should().NotBeNull();
        result!.Culture.Should().Be("en");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static TestParent CreateEntityWithTranslations(params string[] cultures)
    {
        TestParent entity = new() { Id = Guid.NewGuid() };

        foreach (string culture in cultures)
        {
            entity.Translations.Add(new TestTranslation
            {
                Id = Guid.NewGuid(),
                ParentId = entity.Id,
                Culture = culture,
                Title = $"Title in {culture}",
            });
        }

        return entity;
    }
}
