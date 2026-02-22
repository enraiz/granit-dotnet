using DigitalDynamics.Foundation.Localization.Attributes;

namespace DigitalDynamics.Foundation.Localization.Tests.TestResources;

[LocalizationResourceName("Child")]
[InheritResource(typeof(ParentTestResource))]
public sealed class ChildTestResource;
