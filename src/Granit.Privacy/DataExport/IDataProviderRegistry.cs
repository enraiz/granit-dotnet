namespace Granit.Privacy.DataExport;

/// <summary>
/// Declarative registry of modules participating in data subject export/deletion.
/// Each module registers its provider name at startup.
/// The Saga uses <see cref="Count"/> to know how many fragments to expect.
/// </summary>
public interface IDataProviderRegistry
{
    /// <summary>Registers a data provider. Throws if the name is already registered.</summary>
    void Register(string providerName);

    /// <summary>Returns all registered provider names.</summary>
    IReadOnlyList<string> GetAll();

    /// <summary>Returns the number of registered providers.</summary>
    int Count { get; }
}
