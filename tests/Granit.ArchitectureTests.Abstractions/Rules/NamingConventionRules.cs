using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Syntax.Elements.Types.Classes;
using ArchUnitNET.xUnit;
using Shouldly;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Granit.ArchitectureTests.Abstractions.Rules;

/// <summary>
/// Reusable naming convention rules: interface prefix, CQRS Reader/Writer, no Dto suffix.
/// </summary>
public static class NamingConventionRules
{
    /// <summary>
    /// All interfaces in the given type prefix must start with 'I'.
    /// </summary>
    public static void InterfacesShouldStartWithI(ArchUnitNET.Domain.Architecture architecture, string typePrefix)
    {
        IEnumerable<Interface> violations = architecture.Interfaces
            .Where(i => i.FullName.StartsWith(typePrefix, StringComparison.Ordinal)
                && !i.Name.StartsWith('I'));

        violations.ShouldBeEmpty(
            "All interfaces must start with 'I' prefix. " +
            $"Violators: {string.Join(", ", violations.Select(i => i.FullName))}");
    }

    /// <summary>
    /// Interfaces containing "Reader" in their name must end with "Reader".
    /// </summary>
    public static void ReaderInterfacesShouldEndWithReader(ArchUnitNET.Domain.Architecture architecture)
    {
        IEnumerable<Interface> readerInterfaces = architecture.Interfaces
            .Where(i => StripGenericArity(i.Name).Contains("Reader", StringComparison.Ordinal));

        foreach (Interface iface in readerInterfaces)
        {
            string baseName = StripGenericArity(iface.Name);
            baseName.ShouldEndWith("Reader",
                customMessage: $"CQRS convention: {iface.FullName} should end with Reader");
        }
    }

    /// <summary>
    /// Interfaces containing "Writer" in their name must end with "Writer".
    /// </summary>
    public static void WriterInterfacesShouldEndWithWriter(ArchUnitNET.Domain.Architecture architecture)
    {
        IEnumerable<Interface> writerInterfaces = architecture.Interfaces
            .Where(i => StripGenericArity(i.Name).Contains("Writer", StringComparison.Ordinal));

        foreach (Interface iface in writerInterfaces)
        {
            string baseName = StripGenericArity(iface.Name);
            baseName.ShouldEndWith("Writer",
                customMessage: $"CQRS convention: {iface.FullName} should end with Writer");
        }
    }

    /// <summary>
    /// All classes inheriting from <see cref="Exception"/> must have the "Exception" suffix.
    /// </summary>
    public static void ExceptionClassesShouldEndWithException(ArchUnitNET.Domain.Architecture architecture, string typePrefix)
    {
        IArchRule rule = Classes()
            .That().AreAssignableTo(typeof(Exception))
            .And().HaveFullNameStartingWith(typePrefix)
            .Should().HaveNameEndingWith("Exception")
            .Because("all exception classes must follow the .NET naming convention");

        rule.Check(architecture);
    }

    /// <summary>
    /// Types in endpoint namespaces must not use the "Dto" suffix.
    /// </summary>
    public static void EndpointTypesShouldNotUseDtoSuffix(
        ArchUnitNET.Domain.Architecture architecture,
        params string[] endpointNamespaces)
    {
        IObjectProvider<Class> endpointClasses = BuildEndpointProvider(endpointNamespaces);

        IArchRule rule = Classes().That().Are(endpointClasses)
            .Should().NotHaveNameEndingWith("Dto")
            .Because("use *Request / *Response suffixes, never *Dto");

        rule.Check(architecture);
    }

    private static GivenClassesConjunctionWithDescription BuildEndpointProvider(string[] namespaces)
    {
        if (namespaces.Length == 0)
        {
            return Classes().That().ResideInNamespace("__nonexistent__").As("no endpoints");
        }

        GivenClassesConjunction chain = Classes().That().ResideInNamespace(namespaces[0]);
        for (int i = 1; i < namespaces.Length; i++)
        {
            chain = chain.Or().ResideInNamespace(namespaces[i]);
        }

        return chain.As("endpoint types");
    }

    private static string StripGenericArity(string name)
    {
        int backtickIndex = name.IndexOf('`', StringComparison.Ordinal);
        return backtickIndex >= 0 ? name[..backtickIndex] : name;
    }
}
