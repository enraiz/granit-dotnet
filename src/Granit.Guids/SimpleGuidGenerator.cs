namespace Granit.Guids;

/// <summary>
/// Implementation de <see cref="IGuidGenerator"/> basee sur <see cref="Guid.NewGuid()"/>.
/// Disponible via <see cref="Instance"/> pour les contextes sans DI.
/// </summary>
public sealed class SimpleGuidGenerator : IGuidGenerator
{
    /// <summary>
    /// Instance statique pour les contextes sans injection de dependances.
    /// </summary>
    public static SimpleGuidGenerator Instance { get; } = new();

    /// <inheritdoc />
    public Guid Create() => Guid.NewGuid();
}
