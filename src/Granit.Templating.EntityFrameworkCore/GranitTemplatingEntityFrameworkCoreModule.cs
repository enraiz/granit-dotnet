using Granit.Core.Modularity;

namespace Granit.Templating.EntityFrameworkCore;

/// <summary>
/// Granit module for EF Core persistence of template revisions.
/// </summary>
/// <remarks>
/// Registers <c>IDocumentTemplateStore</c> (scoped) and <c>StoreTemplateResolver</c>
/// (scoped, Priority=100). Depends on <see cref="GranitTemplatingModule"/>.
/// <para>
/// Use <c>AddGranitTemplatingEntityFrameworkCore(configure)</c> to register the DbContext.
/// This module does <strong>not</strong> call the extension method — the host application
/// must configure the database provider explicitly.
/// </para>
/// </remarks>
[DependsOn(typeof(GranitTemplatingModule))]
public sealed class GranitTemplatingEntityFrameworkCoreModule : GranitModule;
