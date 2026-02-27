import { useContext } from "react";
import { CookieConsentContext } from "./CookieConsentContext";
import type { CookieConsentContextValue } from "./types";

/**
 * Hook to access the cookie consent state and actions.
 * Must be used within a CookieConsentProvider.
 *
 * @example
 * ```tsx
 * const { consents, isLoaded, acceptCategory, revokeCategory } = useCookieConsent();
 *
 * if (!isLoaded) return <Spinner />;
 *
 * if (consents.analytics) {
 *   // Load analytics scripts
 * }
 * ```
 */
export function useCookieConsent(): CookieConsentContextValue {
  const context = useContext(CookieConsentContext);
  if (!context) {
    throw new Error(
      "useCookieConsent must be used within a CookieConsentProvider"
    );
  }
  return context;
}
