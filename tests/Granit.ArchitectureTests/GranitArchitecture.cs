using Granit.ArchitectureTests.Abstractions;

namespace Granit.ArchitectureTests;

/// <summary>
/// Loads the entire Granit architecture graph once per test run.
/// All test classes share this static instance to avoid repeated assembly scanning.
/// </summary>
internal static class GranitArchitecture
{
    internal static readonly ArchUnitNET.Domain.Architecture Instance =
        ArchitectureLoader.Load("Granit.", typeof(GranitArchitecture).Assembly);
}
