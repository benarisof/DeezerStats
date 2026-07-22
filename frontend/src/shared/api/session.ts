/**
 * Stockage bas niveau des tokens d'authentification. Volontairement sans dépendance à React ni au
 * store Zustand de `features/auth` : `shared/api/httpClient` (utilisé par toutes les couches
 * au-dessus) doit pouvoir lire/écrire le token sans jamais dépendre d'une couche `features` — voir
 * les règles de dépendance Feature-Sliced Design (une couche ne dépend que des couches en dessous
 * d'elle). `features/auth` construit son store réactif par-dessus ces fonctions.
 *
 * L'access token ne vit qu'en mémoire (perdu au rechargement de page, par choix : réduit la fenêtre
 * d'exposition en cas de XSS). Le refresh token est persisté en localStorage pour survivre au
 * rechargement ; `features/auth` s'en sert au démarrage de l'app pour restaurer la session
 * (voir authStore.bootstrap).
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
