import { useCallback, useEffect, useState } from "react";
import type { TransitionDto } from "../types";

interface UseWorkflowStatusOptions {
  /** Base URL for the workflow API. Default: "/api/workflow". */
  baseUrl?: string;
  /** Custom fetch function (e.g., from @granit/api-client). */
  fetcher?: (url: string) => Promise<Response>;
}

interface UseWorkflowStatusResult {
  /** Available transitions for the current user. */
  transitions: TransitionDto[];
  /** Whether the data is loading. */
  isLoading: boolean;
  /** Error if the fetch failed. */
  error: Error | null;
  /** Refetch transitions. */
  refetch: () => Promise<void>;
}

/**
 * Hook to fetch available workflow transitions for an entity.
 *
 * @example
 * ```tsx
 * const { transitions, isLoading } = useWorkflowStatus("document", doc.id);
 * ```
 */
export function useWorkflowStatus(
  entityType: string,
  entityId: string,
  options: UseWorkflowStatusOptions = {},
): UseWorkflowStatusResult {
  const { baseUrl = "/api/workflow", fetcher } = options;
  const [transitions, setTransitions] = useState<TransitionDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  const fetchFn = fetcher ?? ((url: string) => fetch(url));

  const refetch = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const response = await fetchFn(
        `${baseUrl}/${entityType}/${entityId}/transitions`,
      );
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }
      const data = (await response.json()) as TransitionDto[];
      setTransitions(data);
    } catch (err) {
      setError(err instanceof Error ? err : new Error(String(err)));
    } finally {
      setIsLoading(false);
    }
  }, [baseUrl, entityType, entityId, fetchFn]);

  useEffect(() => {
    void refetch();
  }, [refetch]);

  return { transitions, isLoading, error, refetch };
}
