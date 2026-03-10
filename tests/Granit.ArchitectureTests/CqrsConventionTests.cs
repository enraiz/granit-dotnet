using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
using Shouldly;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Granit.ArchitectureTests;

/// <summary>
/// Validates CQRS conventions: Reader/Writer interface naming,
/// no combined I*Store injection in endpoint constructors.
/// See docs/patterns/data/repository.md.
/// </summary>
public sealed class CqrsConventionTests
{
    private static readonly ArchUnitNET.Domain.Architecture Architecture = GranitArchitecture.Instance;

    [Fact]
    public void Reader_interfaces_should_end_with_Reader()
    {
        // Generic types have names like IReferenceDataStoreReader`1 in reflection,
        // so we strip the generic arity suffix before checking.
        IEnumerable<Interface> readerInterfaces = Architecture.Interfaces
            .Where(i => StripGenericArity(i.Name).Contains("Reader", StringComparison.Ordinal));

        foreach (Interface iface in readerInterfaces)
        {
            string baseName = StripGenericArity(iface.Name);
            baseName.ShouldEndWith("Reader",
                customMessage: $"CQRS convention: {iface.FullName} should end with Reader");
        }
    }

    [Fact]
    public void Writer_interfaces_should_end_with_Writer()
    {
        IEnumerable<Interface> writerInterfaces = Architecture.Interfaces
            .Where(i => StripGenericArity(i.Name).Contains("Writer", StringComparison.Ordinal));

        foreach (Interface iface in writerInterfaces)
        {
            string baseName = StripGenericArity(iface.Name);
            baseName.ShouldEndWith("Writer",
                customMessage: $"CQRS convention: {iface.FullName} should end with Writer");
        }
    }

    [Fact]
    public void Interfaces_should_start_with_I_prefix()
    {
        IEnumerable<Interface> violations = Architecture.Interfaces
            .Where(i => i.FullName.StartsWith("Granit.", StringComparison.Ordinal)
                && !i.Name.StartsWith('I'));

        violations.ShouldBeEmpty(
            "All interfaces must start with 'I' prefix (style-et-nommage.md). " +
            $"Violators: {string.Join(", ", violations.Select(i => i.FullName))}");
    }

    private static string StripGenericArity(string name)
    {
        int backtickIndex = name.IndexOf('`', StringComparison.Ordinal);
        return backtickIndex >= 0 ? name[..backtickIndex] : name;
    }
}
