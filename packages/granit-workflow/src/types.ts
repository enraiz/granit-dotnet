/** Represents a single available workflow transition. */
export interface TransitionDto {
  targetState: string;
  name: string;
  allowed: boolean;
  requiresApproval: boolean;
}

/** Current workflow status of an entity. */
export interface WorkflowStatusDto {
  currentState: string;
  availableTransitions: TransitionDto[];
}

/** Result of a transition attempt. */
export interface TransitionResultDto {
  succeeded: boolean;
  resultingState: string;
  outcome: "Completed" | "ApprovalRequested" | "Denied" | "InvalidTransition";
}

/** Single entry in the workflow transition history (HDS audit trail). */
export interface TransitionHistoryDto {
  previousState: string;
  newState: string;
  transitionedAt: string;
  transitionedBy: string;
  comment: string | null;
}

/** Props for the WorkflowStatusBar component. */
export interface WorkflowStatusBarProps {
  /** Logical entity type name (e.g. "document"). */
  readonly entityType: string;
  /** Entity identifier. */
  readonly entityId: string;
  /** Current state of the entity. */
  readonly currentState: string;
  /** All possible states in order for the status bar display. */
  readonly states: readonly string[];
  /** Available transitions for the current user. */
  readonly transitions: readonly TransitionDto[];
  /** Callback when the user triggers a transition. */
  readonly onTransition?: (targetState: string, comment?: string) => void;
  /** Whether a transition is currently in progress. */
  readonly isLoading?: boolean;
  /** Optional CSS class name. */
  readonly className?: string;
}
