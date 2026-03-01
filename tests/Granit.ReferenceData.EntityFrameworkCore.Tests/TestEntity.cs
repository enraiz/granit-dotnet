using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Granit.ReferenceData.EntityFrameworkCore.Tests;

/// <summary>
/// Concrete test entity for unit tests. Must be non-private so NSubstitute
/// can create proxies for generic interfaces like <c>IReferenceDataStore&lt;TestEntity&gt;</c>.
/// </summary>
internal sealed class TestEntity : ReferenceDataEntity;
