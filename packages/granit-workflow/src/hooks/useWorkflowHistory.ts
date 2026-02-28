import { useCallback, useEffect, useState } from "react";
import type { TransitionHistoryDto } from "../types";

interface UseWorkflowHistoryOptions {
  /** Base URL for the workflow API. Default: "/api/workflow". */
  baseUrl?: string;
  /** Custom fetch function (e.g., from @granit/api-client). */
  fetcher?: (url: string) => Promise<Response>;
  /** Whether to fetch immediately. Default: true. */
  enabled?: boolean;
}

interface UseWorkflowHistoryResult {
  /** Transition history entries, ordered chronologically. */
  history: TransitionHistoryDto[];
  /** Whether the data is loading. */
  isLoading: boolean;
  /** Error if the fetch failed. */
  error: Error | null;
  /** Refetch history. */
  refetch: () => Promise<void>;
}

/**
 * Hook to fetch the workflow transition history (HDS audit trail) for an entity.
 *
 * @example
 * ```tsx
 * const { history, isLoading } = useWorkflowHistory("document", doc.id);
 * ```
 */
export function useWorkflowHistory(
  entityType: string,
  entityId: string,
  options: UseWorkflowHistoryOptions = {},
): UseWorkflowHistoryResult {
  const { baseUrl = "/api/workflow", fetcher, enabled = true } = options;
  const [history, setHistory] = useState<TransitionHistoryDto[]>([]);
  const [isLoading, setIsLoading] = useState(enabled);
  const [error, setError] = useState<Error | null>(null);

  const fetchFn = fetcher ?? ((url: string) => fetch(url));

  const refetch = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const response = await fetchFn(
        `${baseUrl}/${entityType}/${entityId}/history`,
      );
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }
      const data = (await response.json()) as TransitionHistoryDto[];
      setHistory(data);
    } catch (err) {
      setError(err instanceof Error ? err : new Error(String(err)));
    } finally {
      setIsLoading(false);
    }
  }, [baseUrl, entityType, entityId, fetchFn]);

  useEffect(() => {
    if (enabled) {
      refetch().catch(() => {});
    }
  }, [enabled, refetch]);

  return { history, isLoading, error, refetch };
}
