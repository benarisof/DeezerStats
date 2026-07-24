/**
 * Stockage bas niveau des tokens, sans dépendance à React ni au store Zustand de `features/auth` :
 * `shared/api/httpClient` doit pouvoir lire/écrire le token sans dépendre d'une couche `features`
 * (règles de dépendance Feature-Sliced Design).
 *
 * L'access token ne vit qu'en mémoire (perdu au rechargement, par choix : réduit la fenêtre
 * d'exposition en cas de XSS). Le refresh token est persisté en localStorage pour survivre au
 * rechargement.
 */

const REFRESH_TOKEN_STORAGE_KEY = "deezerstats.refreshToken";

let accessToken: string | null = null;

export function getAccessToken(): string | null {
  return accessToken;
}

export function getRefreshToken(): string | null {
  return localStorage.getItem(REFRESH_TOKEN_STORAGE_KEY);
}

export function setSessionTokens(tokens: {
  accessToken: string;
  refreshToken: string;
}): void {
  accessToken = tokens.accessToken;
  localStorage.setItem(REFRESH_TOKEN_STORAGE_KEY, tokens.refreshToken);
}

export function clearSessionTokens(): void {
  accessToken = null;
  localStorage.removeItem(REFRESH_TOKEN_STORAGE_KEY);
}
