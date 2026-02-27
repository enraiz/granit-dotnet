using Granit.Templating.Keys;

namespace Granit.DocumentGeneration.Exceptions;

/// <summary>
/// Exception thrown when no <c>IDocumentRenderer</c> is registered for the requested
/// <see cref="DocumentFormat"/>.
/// </summary>
public sealed class DocumentRendererNotFoundException : Exception
{
    /// <summary>The format for which no renderer was found.</summary>
    public DocumentFormat Format { get; }

    /// <summary>
    /// Initializes a new <see cref="DocumentRendererNotFoundException"/>.
    /// </summary>
    public DocumentRendererNotFoundException(DocumentFormat format)
        : base($"No IDocumentRenderer is registered for format '{format}'. " +
               "Add the corresponding renderer package (e.g. Granit.DocumentGeneration.Pdf).")
    {
        Format = format;
    }
}
