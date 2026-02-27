/**
 * RGPD cookie consent categories — mirrors the C# CookieCategory enum.
 */
export type CookieCategory =
  | "strictly_necessary"
  | "preferences"
  | "analytics"
  | "marketing";

/**
 * Current consent state for each category.
 */
export type ConsentState = Record<CookieCategory, boolean>;

/**
 * Abstraction for a Consent Management Platform (Axeptio, Cookiebot, native, etc.).
 * Applications provide their own implementation.
 */
export interface CookieConsentProvider {
  /** Initializes the CMP (loads SDK, reads existing consent). */
  init(): Promise<void>;

  /** Returns the current consent state for all categories. */
  getConsents(): ConsentState;

  /**
   * Subscribes to consent changes. Returns an unsubscribe function.
   * Called when the user updates their consent preferences.
   */
  onConsentChange(callback: (consents: ConsentState) => void): () => void;
}

/**
 * Value exposed by the CookieConsentContext to React components.
 */
export interface CookieConsentContextValue {
  /** Current consent state per category. */
  consents: ConsentState;

  /** Whether the CMP has been initialized and consent state is loaded. */
  isLoaded: boolean;

  /** Grants consent for a specific category. */
  acceptCategory: (category: CookieCategory) => void;

  /** Revokes consent for a specific category. */
  revokeCategory: (category: CookieCategory) => void;

  /** Grants consent for all categories. */
  acceptAll: () => void;

  /** Revokes consent for all non-essential categories. */
  revokeAll: () => void;
}
