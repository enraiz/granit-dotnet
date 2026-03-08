using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Shouldly;
using Xunit;

namespace Granit.Localization.SourceGenerator.Tests;

public sealed class LocalizationKeysGeneratorTests
{
    [Fact]
    public void Generator_ProducesConstants_ForSimpleColonSeparatedKeys()
    {
        // Arrange
        string json = """
            {
              "culture": "fr",
              "texts": {
                "Granit:EntityNotFound": "L'entité...",
                "Granit:ValidationError": "Erreur de validation."
              }
            }
            """;

        // Act
        string generatedSource = RunGenerator(null, json);

        // Assert
        generatedSource.ShouldContain("public static class Granit");
        generatedSource.ShouldContain("public const string EntityNotFound = \"Granit:EntityNotFound\";");
        generatedSource.ShouldContain("public const string ValidationError = \"Granit:ValidationError\";");
    }

    [Fact]
    public void Generator_ProducesNestedClasses_ForDotSeparatedKeys()
    {
        // Arrange
        string json = """
            {
              "culture": "fr",
              "texts": {
                "Validation": {
                  "Required": "Ce champ est obligatoire.",
                  "MaxLength": "Maximum {0} caractères."
                }
              }
            }
            """;

        // Act
        string generatedSource = RunGenerator(null, json);

        // Assert
        generatedSource.ShouldContain("public static class Validation");
        generatedSource.ShouldContain("public const string Required = \"Validation.Required\";");
        generatedSource.ShouldContain("public const string MaxLength = \"Validation.MaxLength\";");
    }

    [Fact]
    public void Generator_ProducesDeepNesting_ForColonAndDotKeys()
    {
        // Arrange
        string json = """
            {
              "culture": "fr",
              "texts": {
                "Granit:Validation.Required": "Obligatoire",
                "Granit:Validation.MaxLength": "Maximum {0}"
              }
            }
            """;

        // Act
        string generatedSource = RunGenerator(null, json);

        // Assert
        generatedSource.ShouldContain("public static class Granit");
        generatedSource.ShouldContain("public static class Validation");
        generatedSource.ShouldContain("public const string Required = \"Granit:Validation.Required\";");
        generatedSource.ShouldContain("public const string MaxLength = \"Granit:Validation.MaxLength\";");
    }

    [Fact]
    public void Generator_MergesKeys_FromMultipleJsonFiles()
    {
        // Arrange
        string frJson = """
            {
              "culture": "fr",
              "texts": {
                "Granit:EntityNotFound": "Entité introuvable",
                "Granit:Forbidden": "Accès interdit"
              }
            }
            """;

        string enJson = """
            {
              "culture": "en",
              "texts": {
                "Granit:EntityNotFound": "Entity not found",
                "Granit:Unauthorized": "Unauthorized"
              }
            }
            """;

        // Act
        string generatedSource = RunGenerator(null, frJson, enJson);

        // Assert — all unique keys from both files
        generatedSource.ShouldContain("public const string EntityNotFound = \"Granit:EntityNotFound\";");
        generatedSource.ShouldContain("public const string Forbidden = \"Granit:Forbidden\";");
        generatedSource.ShouldContain("public const string Unauthorized = \"Granit:Unauthorized\";");
    }

    [Fact]
    public void Generator_ProducesNoOutput_WhenNoJsonFilesProvided()
    {
        // Arrange & Act
        string generatedSource = RunGenerator(null);

        // Assert
        generatedSource.ShouldBeEmpty();
    }

    [Fact]
    public void Generator_SkipsJsonFiles_WithoutTextsProperty()
    {
        // Arrange
        string json = """
            {
              "culture": "fr",
              "settings": { "key": "value" }
            }
            """;

        // Act
        string generatedSource = RunGenerator(null, json);

        // Assert
        generatedSource.ShouldBeEmpty();
    }

    [Fact]
    public void Generator_SkipsMalformedJson()
    {
        // Arrange
        string json = "{ invalid json !!!";

        // Act
        string generatedSource = RunGenerator(null, json);

        // Assert — no crash, no output
        generatedSource.ShouldBeEmpty();
    }

    [Fact]
    public void Generator_UsesRootNamespace_WhenProvided()
    {
        // Arrange
        string json = """
            {
              "culture": "fr",
              "texts": {
                "App:Hello": "Bonjour"
              }
            }
            """;

        // Act
        string generatedSource = RunGenerator("MyApp.Domain", json);

        // Assert
        generatedSource.ShouldContain("namespace MyApp.Domain;");
    }

    [Fact]
    public void Generator_SanitizesInvalidIdentifiers()
    {
        // Arrange
        string json = """
            {
              "culture": "fr",
              "texts": {
                "my-resource:my-key": "value",
                "123:starts-with-digit": "value"
              }
            }
            """;

        // Act
        string generatedSource = RunGenerator(null, json);

        // Assert — hyphens replaced with underscores, leading digit prefixed
        generatedSource.ShouldContain("public static class my_resource");
        generatedSource.ShouldContain("public const string my_key = \"my-resource:my-key\";");
        generatedSource.ShouldContain("public static class _123");
        generatedSource.ShouldContain("public const string starts_with_digit = \"123:starts-with-digit\";");
    }

    [Fact]
    public void Generator_ProducesValidCompilableSource()
    {
        // Arrange
        string json = """
            {
              "culture": "fr",
              "texts": {
                "Granit:EntityNotFound": "L'entité...",
                "Granit:ValidationError": "Erreur",
                "Granit:Unauthorized": "Non autorisé",
                "Granit:Forbidden": "Interdit",
                "Granit:InternalError": "Erreur interne"
              }
            }
            """;

        // Act
        string generatedSource = RunGenerator(null, json);

        // Assert — the generated source should compile without errors
        System.Threading.CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        SyntaxTree tree = CSharpSyntaxTree.ParseText(generatedSource, cancellationToken: cancellationToken);
        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratedAssembly",
            syntaxTrees: new[] { tree },
            references: GetNetCoreReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        ImmutableArray<Diagnostic> diagnostics = compilation.GetDiagnostics(cancellationToken);
        IEnumerable<Diagnostic> errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
        errors.ShouldBeEmpty("generated source should compile without errors");
    }

    private static string RunGenerator(string? rootNamespace, params string[] jsonContents)
    {
        SyntaxTree dummyTree = CSharpSyntaxTree.ParseText("");
        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { dummyTree },
            references: GetNetCoreReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        LocalizationKeysGenerator generator = new();

        List<AdditionalText> additionalTexts = [];
        for (int i = 0; i < jsonContents.Length; i++)
        {
            additionalTexts.Add(new InMemoryAdditionalText("Localization/file" + i + ".json", jsonContents[i]));
        }

        InMemoryAnalyzerConfigOptionsProvider optionsProvider = new(rootNamespace);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new[] { generator.AsSourceGenerator() },
            additionalTexts: additionalTexts,
            optionsProvider: optionsProvider);
        driver = driver.RunGenerators(compilation);

        GeneratorDriverRunResult result = driver.GetRunResult();

        foreach (GeneratorRunResult gr in result.Results)
        {
            if (gr.Exception is not null)
            {
                throw new InvalidOperationException(
                    $"Generator threw: {gr.Exception.Message}", gr.Exception);
            }

            foreach (GeneratedSourceResult src in gr.GeneratedSources)
            {
                if (src.HintName == "LocalizationKeys.g.cs")
                {
                    return src.SourceText.ToString();
                }
            }
        }

        return string.Empty;
    }

    private static ImmutableArray<MetadataReference> GetNetCoreReferences()
    {
        string? assembliesStr = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (assembliesStr is null)
        {
            return ImmutableArray<MetadataReference>.Empty;
        }

        ImmutableArray<MetadataReference>.Builder builder = ImmutableArray.CreateBuilder<MetadataReference>();
        foreach (string path in assembliesStr.Split(Path.PathSeparator))
        {
            builder.Add(MetadataReference.CreateFromFile(path));
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// In-memory implementation of <see cref="AdditionalText"/> for testing.
    /// </summary>
    private sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly SourceText _text;

        public InMemoryAdditionalText(string path, string content)
        {
            Path = path;
            _text = SourceText.From(content);
        }

        public override string Path { get; }

        public override SourceText? GetText(System.Threading.CancellationToken cancellationToken = default) =>
            _text;
    }

    /// <summary>
    /// In-memory implementation of <see cref="AnalyzerConfigOptionsProvider"/> for testing.
    /// Provides <c>build_property.RootNamespace</c> to the generator.
    /// </summary>
    private sealed class InMemoryAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly InMemoryGlobalOptions _globalOptions;

        public InMemoryAnalyzerConfigOptionsProvider(string? rootNamespace)
        {
            _globalOptions = new InMemoryGlobalOptions(rootNamespace);
        }

        public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) =>
            EmptyOptions.Instance;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) =>
            EmptyOptions.Instance;

        private sealed class InMemoryGlobalOptions : AnalyzerConfigOptions
        {
            private readonly Dictionary<string, string> _values;

            public InMemoryGlobalOptions(string? rootNamespace)
            {
                _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (rootNamespace is not null)
                {
                    _values["build_property.RootNamespace"] = rootNamespace;
                }
            }

            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) =>
                _values.TryGetValue(key, out value);
        }

        private sealed class EmptyOptions : AnalyzerConfigOptions
        {
            public static EmptyOptions Instance { get; } = new();

            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            {
                value = null;
                return false;
            }
        }
    }
}
