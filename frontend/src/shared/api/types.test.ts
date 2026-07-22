import { describe, expect, it } from "vitest";
import { toQueryString } from "./types";

describe("toQueryString", () => {
  it("returns an empty string when there are no defined params", () => {
    expect(toQueryString({})).toBe("");
    expect(toQueryString({ from: undefined, to: undefined })).toBe("");
  });

  it("omits empty strings", () => {
    expect(toQueryString({ q: "" })).toBe("");
  });

  it("serializes defined string and number params", () => {
    const result = toQueryString({ from: "2026-01-01", page: 2 });
    const params = new URLSearchParams(result.slice(1));

    expect(result.startsWith("?")).toBe(true);
    expect(params.get("from")).toBe("2026-01-01");
    expect(params.get("page")).toBe("2");
  });

  it("skips undefined params but keeps the others", () => {
    const result = toQueryString({
      from: "2026-01-01",
      to: undefined,
      page: 1,
    });
    const params = new URLSearchParams(result.slice(1));

    expect(params.has("to")).toBe(false);
    expect(params.get("from")).toBe("2026-01-01");
    expect(params.get("page")).toBe("1");
  });
});
