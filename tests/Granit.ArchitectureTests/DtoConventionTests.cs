using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Granit.ArchitectureTests;

/// <summary>
/// Validates DTO naming conventions in endpoint packages:
/// no *Dto suffix, use *Request / *Response instead.
/// </summary>
public sealed class DtoConventionTests
{
    private static readonly ArchUnitNET.Domain.Architecture Architecture = GranitArchitecture.Instance;

    [Fact]
    public void Endpoint_types_should_not_use_Dto_suffix()
    {
        IArchRule rule = Classes()
            .That().ResideInNamespace("Granit.Authentication.ApiKeys.Endpoints")
            .Or().ResideInNamespace("Granit.Authorization.Endpoints")
            .Or().ResideInNamespace("Granit.BackgroundJobs.Endpoints")
            .Or().ResideInNamespace("Granit.Cookies.Endpoints")
            .Or().ResideInNamespace("Granit.DataExchange.Endpoints")
            .Or().ResideInNamespace("Granit.Identity.Endpoints")
            .Or().ResideInNamespace("Granit.Localization.Endpoints")
            .Or().ResideInNamespace("Granit.Notifications.Endpoints")
            .Or().ResideInNamespace("Granit.Querying.Endpoints")
            .Or().ResideInNamespace("Granit.ReferenceData.Endpoints")
            .Or().ResideInNamespace("Granit.Templating.Endpoints")
            .Or().ResideInNamespace("Granit.Timeline.Endpoints")
            .Or().ResideInNamespace("Granit.Workflow.Endpoints")
            .Should().NotHaveNameEndingWith("Dto")
            .Because("CLAUDE.md: use *Request / *Response suffixes, never *Dto");

        rule.Check(Architecture);
    }
}
