using Granit.Localization;
using Granit.Localization.Attributes;

namespace Granit.BlobStorage;

/// <summary>
/// Marker class for the <c>BlobStorage</c> localization resource.
/// JSON files: <c>Localization/BlobStorage/{culture}.json</c>, embedded in this assembly.
/// </summary>
[LocalizationResourceName("BlobStorage")]
[InheritResource(typeof(GranitLocalizationResource))]
public sealed class BlobStorageLocalizationResource;
