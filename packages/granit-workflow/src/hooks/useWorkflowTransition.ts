import { useCallback, useState } from "react";
import type { TransitionResultDto } from "../types";

interface UseWorkflowTransitionOptions {
  /** Base URL for the workflow API. Default: "/api/workflow". */
  baseUrl?: string;
  /** Custom fetch function (e.g., from @granit/api-client). */
  fetcher?: (url: string, init?: RequestInit) => Promise<Response>;
  /** Callback on successful transition. */
  onSuccess?: (result: TransitionResultDto) => void;
  /** Callback on failed transition. */
  onError?: (error: Error) => void;
}

interface UseWorkflowTransitionResult {
  /** Trigger a workflow transition. */
  transition: (targetState: string, comment?: string) => Promise<TransitionResultDto | null>;
  /** Whether a transition is in progress. */
  isLoading: boolean;
  /** Last transition result. */
  result: TransitionResultDto | null;
  /** Error if the transition failed. */
  error: Error | null;
}

/**
 * Hook for triggering workflow transitions with optimistic UI support.
 *
 * @example
 * ```tsx
 * const { transition, isLoading } = useWorkflowTransition("document", doc.id, {
 *   onSuccess: () => refetch(),
 * });
 *
 * await transition("Published", "Validated by Dr. Martin");
 * ```
 */
export function useWorkflowTransition(
  entityType: string,
  entityId: string,
  options: UseWorkflowTransitionOptions = {},
): UseWorkflowTransitionResult {
  const { baseUrl = "/api/workflow", fetcher, onSuccess, onError } = options;
  const [isLoading, setIsLoading] = useState(false);
  const [result, setResult] = useState<TransitionResultDto | null>(null);
  const [error, setError] = useState<Error | null>(null);

  const fetchFn = fetcher ?? ((url: string, init?: RequestInit) => fetch(url, init));

  const transition = useCallback(
    async (targetState: string, comment?: string): Promise<TransitionResultDto | null> => {
      setIsLoading(true);
      setError(null);
      try {
        const response = await fetchFn(
          `${baseUrl}/${entityType}/${entityId}/transition`,
          {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ targetState, comment }),
          },
        );

        if (!response.ok) {
          throw new Error(`HTTP ${response.status}`);
        }

        const data = (await response.json()) as TransitionResultDto;
        setResult(data);
        onSuccess?.(data);
        return data;
      } catch (err) {
        const wrappedError = err instanceof Error ? err : new Error(String(err));
        setError(wrappedError);
        onError?.(wrappedError);
        return null;
      } finally {
        setIsLoading(false);
      }
    },
    [baseUrl, entityType, entityId, fetchFn, onSuccess, onError],
  );

  return { transition, isLoading, result, error };
}
