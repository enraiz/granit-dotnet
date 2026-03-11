using Granit.Workflow.Domain;
using Microsoft.EntityFrameworkCore;

namespace Granit.Workflow.EntityFrameworkCore;

/// <summary>
/// Interface to implement on the host application's <see cref="DbContext"/>
/// to enable EF Core persistence for Granit workflow transition records.
/// </summary>
/// <remarks>
/// The host application's DbContext must implement this interface and call
/// <c>modelBuilder.ConfigureWorkflowModule()</c> in <c>OnModelCreating</c>.
/// The <c>WorkflowTransitionInterceptor</c> adds <see cref="WorkflowTransitionRecord"/>
/// entities to this DbSet during <c>SaveChanges</c>.
/// </remarks>
/// <example>
/// <code>
/// public sealed class AppDbContext : DbContext, IWorkflowDbContext
/// {
///     public DbSet&lt;WorkflowTransitionRecord&gt; WorkflowTransitionRecords =&gt; Set&lt;WorkflowTransitionRecord&gt;();
///
///     protected override void OnModelCreating(ModelBuilder modelBuilder)
///     {
///         modelBuilder.ConfigureWorkflowModule();
///     }
/// }
/// </code>
/// </example>
public interface IWorkflowDbContext
{
    /// <summary>Immutable ISO 27001 audit trail of workflow state transitions.</summary>
    DbSet<WorkflowTransitionRecord> WorkflowTransitionRecords { get; }
}
