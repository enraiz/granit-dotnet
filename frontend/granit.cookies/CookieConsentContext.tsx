import {
  createContext,
  useCallback,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import type {
  CookieCategory,
  CookieConsentContextValue,
  CookieConsentProvider as ICookieConsentProvider,
  ConsentState,
} from "./types";

const DEFAULT_CONSENTS: ConsentState = {
  strictly_necessary: true,
  preferences: false,
  analytics: false,
  marketing: false,
};

export const CookieConsentContext =
  createContext<CookieConsentContextValue | null>(null);

interface CookieConsentProviderProps {
  /** The CMP implementation (Axeptio, Cookiebot, native, etc.). */
  readonly provider: ICookieConsentProvider;
  readonly children: ReactNode;
}

/**
 * React context provider that initializes the CMP and exposes
 * consent state to the component tree.
 *
 * @example
 * ```tsx
 * <CookieConsentProvider provider={axeptioProvider}>
 *   <App />
 * </CookieConsentProvider>
 * ```
 */
export function CookieConsentProvider({
  provider,
  children,
}: CookieConsentProviderProps) {
  const [consents, setConsents] = useState<ConsentState>(DEFAULT_CONSENTS);
  const [isLoaded, setIsLoaded] = useState(false);

  useEffect(() => {
    let unsubscribe: (() => void) | undefined;

    provider.init().then(() => {
      setConsents(provider.getConsents());
      setIsLoaded(true);
      unsubscribe = provider.onConsentChange(setConsents);
    });

    return () => unsubscribe?.();
  }, [provider]);

  const acceptCategory = useCallback(
    (category: CookieCategory) => {
      setConsents((prev) => ({ ...prev, [category]: true }));
    },
    []
  );

  const revokeCategory = useCallback(
    (category: CookieCategory) => {
      if (category === "strictly_necessary") return;
      setConsents((prev) => ({ ...prev, [category]: false }));
    },
    []
  );

  const acceptAll = useCallback(() => {
    setConsents({
      strictly_necessary: true,
      preferences: true,
      analytics: true,
      marketing: true,
    });
  }, []);

  const revokeAll = useCallback(() => {
    setConsents({
      ...DEFAULT_CONSENTS,
    });
  }, []);

  const value = useMemo<CookieConsentContextValue>(
    () => ({
      consents,
      isLoaded,
      acceptCategory,
      revokeCategory,
      acceptAll,
      revokeAll,
    }),
    [consents, isLoaded, acceptCategory, revokeCategory, acceptAll, revokeAll]
  );

  return (
    <CookieConsentContext.Provider value={value}>
      {children}
    </CookieConsentContext.Provider>
  );
}
