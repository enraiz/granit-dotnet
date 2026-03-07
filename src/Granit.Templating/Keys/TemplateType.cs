using System.Reflection;

namespace Granit.Templating.Keys;

/// <summary>
/// Base class for strongly-typed template declarations.
/// </summary>
/// <typeparam name="TData">
/// The data model merged with the template during rendering. Must be non-null.
/// </typeparam>
/// <remarks>
/// Declare a singleton per logical template:
/// <code>
/// public static readonly TextTemplateType&lt;WelcomeEmailData&gt; WelcomeEmail =
///     new WelcomeEmailTemplateType();
///
/// private sealed class WelcomeEmailTemplateType : TextTemplateType&lt;WelcomeEmailData&gt;
/// {
///     public override string Name => "Acme.WelcomeEmail";
/// }
/// </code>
/// </remarks>
public abstract class TemplateType<TData> where TData : notnull
{
    /// <summary>
    /// Unique logical name of the template. Convention: <c>"Module.TemplateName"</c>.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// The CLR type of the data model associated with this template.
    /// </summary>
    public Type DataType => typeof(TData);

    /// <summary>
    /// Assembly that contains the embedded template resources for this type.
    /// Defaults to the assembly that declares the concrete subclass.
    /// </summary>
    /// <remarks>
    /// Used by <c>EmbeddedTemplateResolver</c> to locate embedded HTML resources.
    /// Override if the template resources live in a different assembly than the declaration.
    /// </remarks>
    public virtual Assembly ResourceAssembly => GetType().Assembly;
}
