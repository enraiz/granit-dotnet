namespace DigitalDynamics.Foundation.Core.Exceptions;

/// <summary>
/// Exception thrown when a requested entity does not exist in the data store.
/// Maps to <c>404 Not Found</c>.
/// </summary>
/// <remarks>
/// Implements <see cref="IUserFriendlyException"/>: the generated message is safe for clients.
/// Does NOT implement <see cref="IHasErrorCode"/> intentionally: entity type names and identifiers
/// must not be used as localizable keys, as they may inadvertently leak schema information.
/// </remarks>
/// <example>
/// <code>
/// throw new EntityNotFoundException(typeof(Appointment), appointmentId);
/// </code>
/// </example>
public class EntityNotFoundException : Exception, IUserFriendlyException
{
    /// <summary>The CLR type of the entity that was not found.</summary>
    public Type EntityType { get; }

    /// <summary>The identifier that was used in the lookup.</summary>
    public object EntityId { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="EntityNotFoundException"/>.
    /// </summary>
    /// <param name="entityType">CLR type of the missing entity.</param>
    /// <param name="id">Identifier used in the lookup.</param>
    public EntityNotFoundException(Type entityType, object id)
        : base($"Entity '{entityType.Name}' with id '{id}' was not found.")
    {
        EntityType = entityType;
        EntityId = id;
    }
}
