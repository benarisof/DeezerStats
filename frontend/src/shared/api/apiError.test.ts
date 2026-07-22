import { describe, expect, it } from "vitest";
import { ApiError } from "./apiError";

describe("ApiError", () => {
  it("uses the ProblemDetails detail as message when present", () => {
    const error = new ApiError(400, {
      detail: "Le mot de passe est trop court.",
    });
    expect(error.message).toBe("Le mot de passe est trop court.");
  });

  it("falls back to the title when detail is absent", () => {
    const error = new ApiError(401, { title: "Non authentifié" });
    expect(error.message).toBe("Non authentifié");
  });

  it("falls back to a generic message when no ProblemDetails is available", () => {
    const error = new ApiError(500, null);
    expect(error.message).toBe("Erreur HTTP 500");
  });

  it("exposes fieldErrors from the ProblemDetails errors map", () => {
    const error = new ApiError(400, {
      errors: { Email: ["L'adresse email n'est pas valide."] },
    });
    expect(error.fieldErrors).toEqual({
      Email: ["L'adresse email n'est pas valide."],
    });
  });

  it("returns an empty object for fieldErrors when there is no errors map", () => {
    const error = new ApiError(500, null);
    expect(error.fieldErrors).toEqual({});
  });
});
