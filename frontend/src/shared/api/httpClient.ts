import { env } from "@/shared/config/env";
import { ApiError, type ProblemDetails } from "./apiError";
import { emitSessionExpired } from "./authEvents";
import {
  clearSessionTokens,
  getAccessToken,
  getRefreshToken,
  setSessionTokens,
} from "./session";

interface RequestOptions {
  method?: "GET" | "POST" | "PUT" | "DELETE";
  body?: unknown;
  /** Pour l'upload de fichiers (voir features/upload) : envoyé tel quel, sans JSON.stringify ni
   * Content-Type manuel (le navigateur fixe le boundary multipart lui-même). */
  formData?: FormData;
  signal?: AbortSignal;
  /** Usage interne : évite de retenter indéfiniment un refresh qui échoue lui-même en 401. */
  skipAuthRetry?: boolean;
}

/** Un seul refresh en vol à la fois : si plusieurs requêtes échouent en 401 en parallèle, elles
 * partagent la même tentative de refresh au lieu d'en déclencher une chacune. */
let refreshPromise: Promise<boolean> | null = null;

async function refreshAccessToken(): Promise<boolean> {
  refreshPromise ??= (async () => {
    const refreshToken = getRefreshToken();

    if (!refreshToken) {
      return false;
    }

    try {
      const response = await fetch(`${env.apiUrl}/auth/refresh`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken }),
      });

      if (!response.ok) {
        return false;
      }

      const tokens = (await response.json()) as {
        accessToken: string;
        refreshToken: string;
      };
      setSessionTokens(tokens);
      return true;
    } catch {
      return false;
    }
  })();

  try {
    return await refreshPromise;
  } finally {
    refreshPromise = null;
  }
}

async function parseProblemDetails(
  response: Response,
): Promise<ProblemDetails | null> {
  try {
    return (await response.json()) as ProblemDetails;
  } catch {
    return null;
  }
}

async function request<T>(
  path: string,
  options: RequestOptions = {},
): Promise<T> {
  const headers = new Headers();
  const accessToken = getAccessToken();

  if (accessToken) {
    headers.set("Authorization", `Bearer ${accessToken}`);
  }

  let body: BodyInit | undefined;

  if (options.formData) {
    body = options.formData;
  } else if (options.body !== undefined) {
    headers.set("Content-Type", "application/json");
    body = JSON.stringify(options.body);
  }

  const response = await fetch(`${env.apiUrl}${path}`, {
    method: options.method ?? "GET",
    headers,
    body,
    signal: options.signal,
  });

  if (response.status === 401 && !options.skipAuthRetry) {
    const refreshed = await refreshAccessToken();

    if (refreshed) {
      return request<T>(path, { ...options, skipAuthRetry: true });
    }

    clearSessionTokens();
    emitSessionExpired();
  }

  if (!response.ok) {
    throw new ApiError(response.status, await parseProblemDetails(response));
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export const httpClient = {
  get: <T>(path: string, signal?: AbortSignal) =>
    request<T>(path, { method: "GET", signal }),

  post: <T>(path: string, body?: unknown, signal?: AbortSignal) =>
    request<T>(path, { method: "POST", body, signal }),

  postForm: <T>(path: string, formData: FormData, signal?: AbortSignal) =>
    request<T>(path, { method: "POST", formData, signal }),

  put: <T>(path: string, body?: unknown, signal?: AbortSignal) =>
    request<T>(path, { method: "PUT", body, signal }),

  delete: <T>(path: string, signal?: AbortSignal) =>
    request<T>(path, { method: "DELETE", signal }),
};
