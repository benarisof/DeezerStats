import { httpClient } from "@/shared/api/httpClient";
import type { UserProfile } from "@/entities/user/model/types";

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  expiresInSeconds: number;
}

export interface RegisterPayload {
  email: string;
  password: string;
  displayName: string;
}

export interface LoginPayload {
  email: string;
  password: string;
}

export function register(payload: RegisterPayload): Promise<AuthTokens> {
  return httpClient.post("/auth/register", payload);
}

export function login(payload: LoginPayload): Promise<AuthTokens> {
  return httpClient.post("/auth/login", payload);
}

export function logout(refreshToken: string): Promise<void> {
  return httpClient.post("/auth/logout", { refreshToken });
}

export function getCurrentUser(): Promise<UserProfile> {
  return httpClient.get("/auth/me");
}
