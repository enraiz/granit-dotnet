using Granit.ArchitectureTests.Abstractions.Rules;
using Xunit;

namespace Granit.ArchitectureTests;

/// <summary>
/// Validates CQRS conventions: Reader/Writer interface naming,
/// interface 'I' prefix.
/// See docs/patterns/data/repository.md.
/// </summary>
public sealed class CqrsConventionTests
{
    private static readonly ArchUnitNET.Domain.Architecture Architecture = GranitArchitecture.Instance;

    [Fact]
    public void Reader_interfaces_should_end_with_Reader() =>
        NamingConventionRules.ReaderInterfacesShouldEndWithReader(Architecture);

    [Fact]
    public void Writer_interfaces_should_end_with_Writer() =>
        NamingConventionRules.WriterInterfacesShouldEndWithWriter(Architecture);

    [Fact]
    public void Interfaces_should_start_with_I_prefix() =>
        NamingConventionRules.InterfacesShouldStartWithI(Architecture, "Granit.");

    [Fact]
    public void Exception_classes_should_end_with_Exception() =>
        NamingConventionRules.ExceptionClassesShouldEndWithException(Architecture, "Granit.");
}
