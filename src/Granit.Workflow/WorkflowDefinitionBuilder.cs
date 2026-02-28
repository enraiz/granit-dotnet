namespace Granit.Workflow;

/// <summary>
/// Fluent builder for constructing an immutable <see cref="WorkflowDefinition{TState}"/>.
/// Validates the graph at <see cref="Build"/> time: initial state must be set,
/// at least one transition must be defined, no duplicate transitions,
/// and warns about unreachable states.
/// </summary>
/// <typeparam name="TState">Enum type representing the workflow states.</typeparam>
public sealed class WorkflowDefinitionBuilder<TState> where TState : struct, Enum
{
    private TState? _initialState;
    private readonly List<WorkflowTransition<TState>> _transitions = [];

    /// <summary>Sets the initial state for newly created entities.</summary>
    public WorkflowDefinitionBuilder<TState> InitialState(TState state)
    {
        _initialState = state;
        return this;
    }

    /// <summary>
    /// Adds a transition from <paramref name="from"/> to <paramref name="to"/>.
    /// </summary>
    public WorkflowDefinitionBuilder<TState> Transition(
        TState from,
        TState to,
        Action<TransitionBuilder<TState>>? configure = null)
    {
        TransitionBuilder<TState> transitionBuilder = new(from, to);
        configure?.Invoke(transitionBuilder);
        _transitions.Add(transitionBuilder.Build());
        return this;
    }

    /// <summary>
    /// Builds and validates the workflow definition.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the definition is invalid.
    /// </exception>
    internal WorkflowDefinition<TState> Build()
    {
        if (_initialState is null)
        {
            throw new InvalidOperationException(
                "Workflow definition must have an initial state. Call InitialState() before Build().");
        }

        if (_transitions.Count == 0)
        {
            throw new InvalidOperationException(
                "Workflow definition must have at least one transition.");
        }

        // Check for duplicate transitions (same From+To)
        HashSet<(TState From, TState To)> seen = [];
        foreach (WorkflowTransition<TState> transition in _transitions)
        {
            if (!seen.Add((transition.From, transition.To)))
            {
                throw new InvalidOperationException(
                    $"Duplicate transition: {transition.From} → {transition.To}. " +
                    "Each From+To pair must be unique.");
            }
        }

        // Detect unreachable states (states with no incoming transition except initial state)
        HashSet<TState> allStates = [_initialState.Value];
        foreach (WorkflowTransition<TState> transition in _transitions)
        {
            allStates.Add(transition.From);
            allStates.Add(transition.To);
        }

        HashSet<TState> reachable = [_initialState.Value];
        // BFS from initial state
        Queue<TState> queue = new();
        queue.Enqueue(_initialState.Value);

        while (queue.Count > 0)
        {
            TState current = queue.Dequeue();
            foreach (WorkflowTransition<TState> transition in _transitions)
            {
                if (EqualityComparer<TState>.Default.Equals(transition.From, current)
                    && reachable.Add(transition.To))
                {
                    queue.Enqueue(transition.To);
                }
            }
        }

        HashSet<TState> unreachable = new(allStates);
        unreachable.ExceptWith(reachable);

        if (unreachable.Count > 0)
        {
            string unreachableNames = string.Join(", ", unreachable);
            throw new InvalidOperationException(
                $"Unreachable states detected: {unreachableNames}. " +
                "These states have no incoming transition path from the initial state.");
        }

        return new WorkflowDefinition<TState>(_initialState.Value, _transitions.AsReadOnly());
    }
}
