import { describe, expect, it } from "vitest";
import { formatCount } from "./formatCount";

describe("formatCount", () => {
  it("formats small numbers without a separator", () => {
    expect(formatCount(42)).toBe("42");
  });

  it("groups thousands with a French narrow no-break space", () => {
    expect(formatCount(1234)).toBe("1 234");
  });

  it("formats zero as-is", () => {
    expect(formatCount(0)).toBe("0");
  });

  it("groups large numbers correctly", () => {
    expect(formatCount(1234567)).toBe("1 234 567");
  });
});
