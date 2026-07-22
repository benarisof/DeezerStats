import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@/shared/config/env", () => ({
  env: { apiUrl: "http://test.local/api/v1" },
}));

// Importés après le mock de env : httpClient lit `env.apiUrl` au moment de l'appel, pas à l'import,
// mais on garde l'ordre pour rester explicite sur la dépendance.
import { httpClient } from "./httpClient";
import { onSessionExpired } from "./authEvents";
import {
  clearSessionTokens,
  getAccessToken,
  getRefreshToken,
  setSessionTokens,
} from "./session";

function mockResponse(status: number, body?: unknown): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    json: async () => body,
  } as Response;
}

describe("httpClient", () => {
  beforeEach(() => {
    clearSessionTokens();
    localStorage.clear();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("attaches the Authorization header when an access token is set", async () => {
    setSessionTokens({ accessToken: "abc", refreshToken: "refresh-1" });
    const fetchMock = vi
      .fn()
      .mockResolvedValue(mockResponse(200, { ok: true }));
    vi.stubGlobal("fetch", fetchMock);

    await httpClient.get("/foo");

    const options = fetchMock.mock.calls[0][1] as RequestInit;
    expect((options.headers as Headers).get("Authorization")).toBe(
      "Bearer abc",
    );
  });

  it("omits the Authorization header when there is no access token", async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValue(mockResponse(200, { ok: true }));
    vi.stubGlobal("fetch", fetchMock);

    await httpClient.get("/foo");

    const options = fetchMock.mock.calls[0][1] as RequestInit;
    expect((options.headers as Headers).get("Authorization")).toBeNull();
  });

  it("sends a JSON body with a Content-Type header for post()", async () => {
    const fetchMock = vi.fn().mockResolvedValue(mockResponse(200, {}));
    vi.stubGlobal("fetch", fetchMock);

    await httpClient.post("/foo", { a: 1 });

    const options = fetchMock.mock.calls[0][1] as RequestInit;
    expect((options.headers as Headers).get("Content-Type")).toBe(
      "application/json",
    );
    expect(options.body).toBe(JSON.stringify({ a: 1 }));
  });

  it("sends FormData as-is for postForm(), without a manual Content-Type", async () => {
    const fetchMock = vi.fn().mockResolvedValue(mockResponse(200, {}));
    vi.stubGlobal("fetch", fetchMock);

    const formData = new FormData();
    formData.set("file", new Blob(["contenu"]), "historique.xlsx");

    await httpClient.postForm("/imports", formData);

    const options = fetchMock.mock.calls[0][1] as RequestInit;
    expect(options.body).toBe(formData);
    expect((options.headers as Headers).get("Content-Type")).toBeNull();
  });

  it("throws an ApiError with the parsed ProblemDetails on a non-2xx response", async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValue(
        mockResponse(404, { title: "Ressource non trouvée", status: 404 }),
      );
    vi.stubGlobal("fetch", fetchMock);

    await expect(httpClient.get("/missing")).rejects.toMatchObject({
      status: 404,
      message: "Ressource non trouvée",
    });
  });

  it("returns undefined for a 204 response without parsing the body", async () => {
    const jsonSpy = vi.fn();
    const fetchMock = vi
      .fn()
      .mockResolvedValue({ ok: true, status: 204, json: jsonSpy });
    vi.stubGlobal("fetch", fetchMock);

    const result = await httpClient.post("/logout");

    expect(result).toBeUndefined();
    expect(jsonSpy).not.toHaveBeenCalled();
  });

  it("on a 401, refreshes the access token and retries the original request once", async () => {
    setSessionTokens({
      accessToken: "expired",
      refreshToken: "refresh-token-1",
    });

    let callCount = 0;
    const fetchMock = vi.fn(async (url: string, _options?: RequestInit) => {
      callCount += 1;

      if (callCount === 1) {
        return mockResponse(401);
      }

      if (url.includes("/auth/refresh")) {
        return mockResponse(200, {
          accessToken: "new-access",
          refreshToken: "new-refresh",
        });
      }

      return mockResponse(200, { data: "ok" });
    });
    vi.stubGlobal("fetch", fetchMock);

    const result = await httpClient.get("/protected");

    expect(result).toEqual({ data: "ok" });
    expect(fetchMock).toHaveBeenCalledTimes(3);
    expect(getAccessToken()).toBe("new-access");
    expect(getRefreshToken()).toBe("new-refresh");

    const retryOptions = fetchMock.mock.calls[2][1] as RequestInit;
    expect((retryOptions.headers as Headers).get("Authorization")).toBe(
      "Bearer new-access",
    );
  });

  it("clears the session and emits sessionExpired when the refresh call itself fails", async () => {
    setSessionTokens({
      accessToken: "expired",
      refreshToken: "refresh-token-1",
    });
    const fetchMock = vi.fn().mockResolvedValue(mockResponse(401));
    vi.stubGlobal("fetch", fetchMock);

    const expiredListener = vi.fn();
    const unsubscribe = onSessionExpired(expiredListener);

    await expect(httpClient.get("/protected")).rejects.toMatchObject({
      status: 401,
    });

    expect(getAccessToken()).toBeNull();
    expect(getRefreshToken()).toBeNull();
    expect(expiredListener).toHaveBeenCalledTimes(1);

    unsubscribe();
  });

  it("skips the refresh attempt entirely when there is no refresh token", async () => {
    const fetchMock = vi.fn().mockResolvedValue(mockResponse(401));
    vi.stubGlobal("fetch", fetchMock);

    const expiredListener = vi.fn();
    const unsubscribe = onSessionExpired(expiredListener);

    await expect(httpClient.get("/protected")).rejects.toMatchObject({
      status: 401,
    });

    // Un seul appel : la requête initiale. Aucune tentative de refresh sans refresh token.
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(expiredListener).toHaveBeenCalledTimes(1);

    unsubscribe();
  });

  it("shares a single refresh call across concurrent 401s", async () => {
    setSessionTokens({
      accessToken: "expired",
      refreshToken: "refresh-token-1",
    });

    let refreshCalls = 0;
    const seenOnce = new Set<string>();
    const fetchMock = vi.fn(async (url: string, _options?: RequestInit) => {
      if (url.includes("/auth/refresh")) {
        refreshCalls += 1;
        return mockResponse(200, {
          accessToken: "new-access",
          refreshToken: "new-refresh",
        });
      }

      if (!seenOnce.has(url)) {
        seenOnce.add(url);
        return mockResponse(401);
      }

      return mockResponse(200, { data: url });
    });
    vi.stubGlobal("fetch", fetchMock);

    const [resultA, resultB] = await Promise.all([
      httpClient.get("/protected-a"),
      httpClient.get("/protected-b"),
    ]);

    expect(resultA).toEqual({ data: "http://test.local/api/v1/protected-a" });
    expect(resultB).toEqual({ data: "http://test.local/api/v1/protected-b" });
    expect(refreshCalls).toBe(1);
  });
});
