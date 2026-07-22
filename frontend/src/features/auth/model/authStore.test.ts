import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { UserProfile } from "@/entities/user/model/types";
import { emitSessionExpired } from "@/shared/api/authEvents";
import {
  clearSessionTokens,
  getAccessToken,
  getRefreshToken,
} from "@/shared/api/session";
import * as authApi from "../api/authApi";
import { useAuthStore } from "./authStore";

vi.mock("../api/authApi", () => ({
  login: vi.fn(),
  register: vi.fn(),
  logout: vi.fn(),
  getCurrentUser: vi.fn(),
}));

const user: UserProfile = {
  id: "11111111-1111-1111-1111-111111111111",
  email: "sofia@example.com",
  displayName: "Sofia",
};

describe("authStore", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    clearSessionTokens();
    localStorage.clear();
    useAuthStore.setState({ user: null, status: "idle" });
  });

  afterEach(() => {
    clearSessionTokens();
    localStorage.clear();
  });

  describe("bootstrap", () => {
    it("goes straight to unauthenticated when there is no persisted refresh token", async () => {
      await useAuthStore.getState().bootstrap();

      expect(useAuthStore.getState()).toMatchObject({
        user: null,
        status: "unauthenticated",
      });
      expect(authApi.getCurrentUser).not.toHaveBeenCalled();
    });

    it("restores the session when a refresh token exists and /auth/me succeeds", async () => {
      localStorage.setItem("deezerstats.refreshToken", "refresh-1");
      vi.mocked(authApi.getCurrentUser).mockResolvedValue(user);

      await useAuthStore.getState().bootstrap();

      expect(useAuthStore.getState()).toMatchObject({
        user,
        status: "authenticated",
      });
    });

    it("clears the stale refresh token and goes unauthenticated when /auth/me fails", async () => {
      localStorage.setItem("deezerstats.refreshToken", "refresh-1");
      vi.mocked(authApi.getCurrentUser).mockRejectedValue(new Error("401"));

      await useAuthStore.getState().bootstrap();

      expect(useAuthStore.getState()).toMatchObject({
        user: null,
        status: "unauthenticated",
      });
      expect(getRefreshToken()).toBeNull();
    });
  });

  describe("login", () => {
    it("persists the tokens and loads the profile on success", async () => {
      vi.mocked(authApi.login).mockResolvedValue({
        accessToken: "access-1",
        refreshToken: "refresh-1",
        expiresInSeconds: 900,
      });
      vi.mocked(authApi.getCurrentUser).mockResolvedValue(user);

      await useAuthStore
        .getState()
        .login({ email: user.email, password: "StrongPass123!" });

      expect(authApi.login).toHaveBeenCalledWith({
        email: user.email,
        password: "StrongPass123!",
      });
      expect(getAccessToken()).toBe("access-1");
      expect(getRefreshToken()).toBe("refresh-1");
      expect(useAuthStore.getState()).toMatchObject({
        user,
        status: "authenticated",
      });
    });

    it("propagates the error and leaves the session untouched when credentials are rejected", async () => {
      vi.mocked(authApi.login).mockRejectedValue(
        new Error("Email ou mot de passe invalide."),
      );

      await expect(
        useAuthStore.getState().login({ email: user.email, password: "wrong" }),
      ).rejects.toThrow("Email ou mot de passe invalide.");

      expect(getAccessToken()).toBeNull();
      expect(useAuthStore.getState()).toMatchObject({
        user: null,
        status: "idle",
      });
    });
  });

  describe("register", () => {
    it("persists the tokens and loads the profile on success (auto-login)", async () => {
      vi.mocked(authApi.register).mockResolvedValue({
        accessToken: "access-1",
        refreshToken: "refresh-1",
        expiresInSeconds: 900,
      });
      vi.mocked(authApi.getCurrentUser).mockResolvedValue(user);

      await useAuthStore.getState().register({
        email: user.email,
        password: "StrongPass123!",
        displayName: user.displayName,
      });

      expect(getAccessToken()).toBe("access-1");
      expect(useAuthStore.getState()).toMatchObject({
        user,
        status: "authenticated",
      });
    });
  });

  describe("logout", () => {
    it("revokes the refresh token server-side then clears the local session", async () => {
      localStorage.setItem("deezerstats.refreshToken", "refresh-1");
      useAuthStore.setState({ user, status: "authenticated" });
      vi.mocked(authApi.logout).mockResolvedValue(undefined);

      await useAuthStore.getState().logout();

      expect(authApi.logout).toHaveBeenCalledWith("refresh-1");
      expect(getRefreshToken()).toBeNull();
      expect(useAuthStore.getState()).toMatchObject({
        user: null,
        status: "unauthenticated",
      });
    });

    it("does not call the API when there is no refresh token, but still clears local state", async () => {
      useAuthStore.setState({ user, status: "authenticated" });

      await useAuthStore.getState().logout();

      expect(authApi.logout).not.toHaveBeenCalled();
      expect(useAuthStore.getState()).toMatchObject({
        user: null,
        status: "unauthenticated",
      });
    });

    it("still clears local state even when the API call fails", async () => {
      localStorage.setItem("deezerstats.refreshToken", "refresh-1");
      useAuthStore.setState({ user, status: "authenticated" });
      vi.mocked(authApi.logout).mockRejectedValue(new Error("network error"));

      await expect(useAuthStore.getState().logout()).rejects.toThrow(
        "network error",
      );

      expect(getRefreshToken()).toBeNull();
      expect(useAuthStore.getState()).toMatchObject({
        user: null,
        status: "unauthenticated",
      });
    });
  });

  it("resets to unauthenticated when httpClient reports an unrecoverable 401 (sessionExpired event)", () => {
    useAuthStore.setState({ user, status: "authenticated" });

    emitSessionExpired();

    expect(useAuthStore.getState()).toMatchObject({
      user: null,
      status: "unauthenticated",
    });
  });
});
