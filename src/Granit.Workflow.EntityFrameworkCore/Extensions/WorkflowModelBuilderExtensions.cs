using Granit.Workflow.EntityFrameworkCore.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Granit.Workflow.EntityFrameworkCore.Extensions;

/// <summary>
/// EF Core <see cref="ModelBuilder"/> extensions for the Workflow module.
/// </summary>
public static class WorkflowModelBuilderExtensions
{
    /// <summary>
    /// Applies the <c>workflow_transition_records</c> table configuration to the model.
    /// </summary>
    /// <remarks>
    /// Call this method in <c>OnModelCreating</c> of the host application's DbContext
    /// that implements <see cref="IWorkflowDbContext"/>.
    /// </remarks>
    /// <param name="modelBuilder">The model builder.</param>
    /// <returns>The model builder for chaining.</returns>
    public static ModelBuilder ConfigureWorkflowModule(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new WorkflowTransitionRecordConfiguration());
        return modelBuilder;
    }
}
