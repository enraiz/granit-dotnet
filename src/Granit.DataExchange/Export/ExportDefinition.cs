namespace Granit.DataExchange.Export;

/// <summary>
/// Base class for declaring how an entity type is exported.
/// Uses the Fluent API pattern — no attributes on the domain model.
/// </summary>
/// <typeparam name="TEntity">The source entity type.</typeparam>
/// <typeparam name="TFilter">
/// The filter type used to restrict exported data.
/// Use <see cref="EmptyExportFilter"/> when no filtering is needed.
/// </typeparam>
/// <remarks>
/// <para>
/// Each export definition is registered as a singleton via
/// <c>services.AddExportDefinition&lt;TEntity, TFilter, TDefinition&gt;()</c>.
/// The <see cref="Configure"/> method is called once at startup.
/// </para>
/// <para>
/// Inspired by Django's <c>ExportResource</c> pattern: only fields explicitly
/// declared in <see cref="Configure"/> are available for export (whitelist).
/// </para>
/// <para>
/// Example:
/// <code>
/// public sealed class PatientExportDefinition : ExportDefinition&lt;Patient, PatientExportFilter&gt;
/// {
///     public override string Name =&gt; "Guava.PatientExport";
///     protected override void Configure(ExportDefinitionBuilder&lt;Patient&gt; builder)
///     {
///         builder
///             .IncludeBusinessKey()
///             .Field(p =&gt; p.LastName, f =&gt; f.Header("Nom"))
///             .Field(p =&gt; p.Email)
///             .Field(p =&gt; p.Company, c =&gt; c.Name, f =&gt; f.Header("Société"));
///     }
/// }
/// </code>
/// </para>
/// </remarks>
public abstract class ExportDefinition<TEntity, TFilter> : IExportDefinitionDescriptor
    where TEntity : class
    where TFilter : class
{
    private ExportDefinitionBuilder<TEntity>? _builder;

    /// <summary>
    /// Unique name identifying this export definition (e.g. <c>"Guava.PatientExport"</c>).
    /// </summary>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public Type EntityType => typeof(TEntity);

    /// <inheritdoc/>
    public Type FilterType => typeof(TFilter);

    /// <summary>
    /// Supported output formats. Default: <c>["xlsx", "csv"]</c>.
    /// </summary>
    public virtual IReadOnlyList<string> SupportedFormats => ["xlsx", "csv"];

    /// <summary>
    /// Configures the export definition using the fluent builder.
    /// Called once at startup.
    /// </summary>
    /// <param name="builder">The definition builder.</param>
    protected abstract void Configure(ExportDefinitionBuilder<TEntity> builder);

    /// <summary>
    /// Gets the built definition metadata (lazily initialized).
    /// </summary>
    internal ExportDefinitionBuilder<TEntity> GetBuilder()
    {
        if (_builder is not null)
        {
            return _builder;
        }

        _builder = new ExportDefinitionBuilder<TEntity>();
        Configure(_builder);
        return _builder;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ExportFieldDescriptor> GetFields() =>
        GetBuilder().Fields.AsReadOnly();

    /// <summary>
    /// Gets whether the entity <c>Id</c> should be included for import-compatible export.
    /// </summary>
    public bool GetIncludeId() => GetBuilder().IncludeIdFlag;

    /// <summary>
    /// Gets whether business key columns should be included for roundtrip import.
    /// </summary>
    public bool GetIncludeBusinessKey() => GetBuilder().IncludeBusinessKeyFlag;
}

/// <summary>
/// Convenience base class for export definitions that don't require filtering.
/// </summary>
/// <typeparam name="TEntity">The source entity type.</typeparam>
public abstract class ExportDefinition<TEntity> : ExportDefinition<TEntity, EmptyExportFilter>
    where TEntity : class;
