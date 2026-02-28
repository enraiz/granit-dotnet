import type { WorkflowStatusBarProps } from "./types";

/**
 * Headless workflow status bar component (Odoo-style).
 *
 * Renders the workflow states as a series of chips/steps with action buttons
 * for available transitions. Fully headless — provides render props for
 * custom styling.
 *
 * @example
 * ```tsx
 * <WorkflowStatusBar
 *   entityType="document"
 *   entityId={doc.id}
 *   currentState="Draft"
 *   states={["Draft", "PendingReview", "Published", "Archived"]}
 *   transitions={transitions}
 *   onTransition={handleTransition}
 * />
 * ```
 */
export function WorkflowStatusBar({
  currentState,
  states,
  transitions,
  onTransition,
  isLoading = false,
  className,
}: WorkflowStatusBarProps) {
  const currentIndex = states.indexOf(currentState);

  return (
    <div className={className} data-testid="workflow-status-bar">
      {/* State chips */}
      <div data-testid="workflow-states">
        {states.map((state, index) => {
          const isCurrent = state === currentState;
          const isPast = index < currentIndex;

          return (
            <span
              key={state}
              data-state={state}
              data-current={isCurrent}
              data-past={isPast}
              data-testid={`workflow-state-${state}`}
            >
              {state}
            </span>
          );
        })}
      </div>

      {/* Transition buttons */}
      <div data-testid="workflow-actions">
        {transitions.map((transition) => {
          const label =
            transition.requiresApproval && !transition.allowed
              ? "Demander l'approbation"
              : transition.name;

          return (
            <button
              key={transition.targetState}
              type="button"
              disabled={isLoading}
              data-target={transition.targetState}
              data-requires-approval={transition.requiresApproval}
              data-testid={`workflow-action-${transition.targetState}`}
              onClick={() => onTransition?.(transition.targetState)}
            >
              {label}
            </button>
          );
        })}
      </div>
    </div>
  );
}
