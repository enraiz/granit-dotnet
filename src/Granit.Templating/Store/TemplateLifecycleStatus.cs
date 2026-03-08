namespace Granit.Templating.Store;

/// <summary>
/// Lifecycle status of a template managed by <see cref="IDocumentTemplateStoreWriter"/>.
/// </summary>
public enum TemplateLifecycleStatus
{
    /// <summary>
    /// Template is being edited. Never used by the rendering pipeline.
    /// Multiple drafts can coexist — only the latest draft per key is editable.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Submitted for review, awaiting approval from a user with the required permission.
    /// Only used when <c>Granit.Templating.Workflow</c> bridge is installed.
    /// </summary>
    PendingReview = 1,

    /// <summary>
    /// Active version resolved by <c>StoreTemplateResolver</c>.
    /// Exactly one published version exists per <c>TemplateKey</c> at any time.
    /// </summary>
    Published = 2,

    /// <summary>
    /// Former published version, superseded by a newer publication.
    /// Preserved indefinitely for HDS audit trail — never physically deleted.
    /// </summary>
    Archived = 3,
}
