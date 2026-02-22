namespace DigitalDynamics.Foundation.Guids;

/// <summary>
/// Abstraction for GUID identifier generation.
/// Replaces <see cref="Guid.NewGuid()"/> to centralize generation
/// and enable sequential GUIDs.
/// </summary>
public interface IGuidGenerator
{
    /// <summary>Creates a new <see cref="Guid"/>.</summary>
    Guid Create();
}
