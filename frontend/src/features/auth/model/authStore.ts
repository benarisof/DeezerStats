import { create } from "zustand";
import { onSessionExpired } from "@/shared/api/authEvents";
import {
  clearSessionTokens,
  getRefreshToken,
  setSessionTokens,
} from "@/shared/api/session";
import type { UserProfile } from "@/entities/user/model/types";
import * as authApi from "../api/authApi";

export type AuthStatus =
  "idle" | "loading" | "authenticated" | "unauthenticated";

interface AuthState {
  user: UserProfile | null;
  status: AuthStatus;
  /** À appeler une fois au démarrage de l'app (voir app/providers) : tente de restaurer la session
   * à partir du refresh token persisté, sans jamais bloquer sur un access token expiré. */
  bootstrap: () => Promise<void>;
  login: (payload: authApi.LoginPayload) => Promise<void>;
  register: (payload: authApi.RegisterPayload) => Promise<void>;
  logout: () => Promise<void>;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  status: "idle",

  bootstrap: async () => {
    if (!getRefreshToken()) {
      set({ user: null, status: "unauthenticated" });
      return;
    }

    set({ status: "loading" });

    try {
      // Aucun access token en mémoire au chargement de la page : cet appel échoue en 401, ce qui
      // déclenche le refresh automatique de httpClient à partir du refresh token persisté (voir
      // shared/api/httpClient) avant d'être rejoué.
      const user = await authApi.getCurrentUser();
      set({ user, status: "authenticated" });
    } catch {
      clearSessionTokens();
      set({ user: null, status: "unauthenticated" });
    }
  },

  login: async (payload) => {
    const tokens = await authApi.login(payload);
    setSessionTokens(tokens);
    const user = await authApi.getCurrentUser();
    set({ user, status: "authenticated" });
  },

  register: async (payload) => {
    const tokens = await authApi.register(payload);
    setSessionTokens(tokens);
    const user = await authApi.getCurrentUser();
    set({ user, status: "authenticated" });
  },

  logout: async () => {
    const refreshToken = getRefreshToken();

    try {
      if (refreshToken) {
        await authApi.logout(refreshToken);
      }
    } finally {
      clearSessionTokens();
      set({ user: null, status: "unauthenticated" });
    }
  },
}));

// Réagit à un 401 non récupérable détecté par httpClient (refresh token lui-même invalide/expiré) :
// synchronise l'état réactif pour que ProtectedRoute redirige vers /login.
onSessionExpired(() => {
  useAuthStore.setState({ user: null, status: "unauthenticated" });
});
