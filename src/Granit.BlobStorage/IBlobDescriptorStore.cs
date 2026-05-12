namespace Granit.BlobStorage;

/// <summary>
/// Combined persistence abstraction for <see cref="Domain.BlobDescriptor"/> records.
/// Extends both <see cref="IBlobDescriptorReader"/> and <see cref="IBlobDescriptorWriter"/>.
/// </summary>
/// <remarks>
/// Prefer injecting <see cref="IBlobDescriptorReader"/> or <see cref="IBlobDescriptorWriter"/>
/// separately to make the read/write intent explicit. This combined interface exists for
/// implementations that need to register a single class for both roles.
/// </remarks>
public interface IBlobDescriptorStore : IBlobDescriptorReader, IBlobDescriptorWriter;
