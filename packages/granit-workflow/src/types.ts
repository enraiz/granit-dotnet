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
  entityType: string;
  /** Entity identifier. */
  entityId: string;
  /** Current state of the entity. */
  currentState: string;
  /** All possible states in order for the status bar display. */
  states: string[];
  /** Available transitions for the current user. */
  transitions: TransitionDto[];
  /** Callback when the user triggers a transition. */
  onTransition?: (targetState: string, comment?: string) => void;
  /** Whether a transition is currently in progress. */
  isLoading?: boolean;
  /** Optional CSS class name. */
  className?: string;
}
