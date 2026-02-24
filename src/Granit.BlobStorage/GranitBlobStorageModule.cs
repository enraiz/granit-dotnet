using Granit.Core.Modularity;

namespace Granit.BlobStorage;

/// <summary>
/// Granit module for blob storage (provider-agnostic core).
/// </summary>
/// <remarks>
/// Defines the <see cref="IBlobStorage"/>, <see cref="IBlobDescriptorStore"/>,
/// <see cref="IBlobKeyStrategy"/>, and <see cref="IBlobValidator"/> abstractions.
/// Register a concrete provider (e.g. <c>Granit.BlobStorage.S3</c>) and a
/// persistence adapter (e.g. <c>Granit.BlobStorage.EntityFrameworkCore</c>) alongside this module.
/// </remarks>
public sealed class GranitBlobStorageModule : GranitModule;
