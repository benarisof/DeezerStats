import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  DATE_PRESET_OPTIONS,
  detectActivePreset,
  rangeForPreset,
} from "./datePresets";

describe("rangeForPreset", () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it("clears both bounds for allTime", () => {
    expect(rangeForPreset("allTime")).toEqual({
      from: undefined,
      to: undefined,
    });
  });

  it("computes a 30-day window ending today, leaving 'to' unset", () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date(2026, 6, 23)); // 23 juillet 2026, minuit local

    expect(rangeForPreset("last30")).toEqual({
      from: "2026-06-24",
      to: undefined,
    });
  });

  it("computes 90/365-day windows the same way", () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date(2026, 6, 23));

    expect(rangeForPreset("last90").from).toBe("2026-04-25");
    expect(rangeForPreset("last365").from).toBe("2025-07-24");
  });

  it("stays on the correct local calendar day even when local and UTC dates differ", () => {
    // Régression : Date.toISOString() convertit en UTC, ce qui décalait la date d'un jour dès que
    // l'heure locale tombait après minuit alors qu'il était encore la veille en UTC (ex. 00h30 en
    // UTC+2 le 23 juillet correspond à 22h30 UTC le 22 juillet). rangeForPreset doit toujours
    // raisonner en date calendaire locale, jamais en UTC.
    const originalTz = process.env.TZ;
    process.env.TZ = "Etc/GMT-2"; // UTC+2 (notation POSIX inversée)

    vi.useFakeTimers();
    vi.setSystemTime(new Date(2026, 6, 23, 0, 30)); // 23 juillet 00h30 local -> 22 juillet 22h30 UTC

    expect(rangeForPreset("last30")).toEqual({
      from: "2026-06-24",
      to: undefined,
    });

    process.env.TZ = originalTz;
  });
});

describe("detectActivePreset", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date(2026, 6, 23));
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("returns allTime when both bounds are absent", () => {
    expect(detectActivePreset({})).toBe("allTime");
    expect(detectActivePreset({ from: undefined, to: undefined })).toBe(
      "allTime",
    );
  });

  it("returns custom whenever 'to' is set, regardless of 'from'", () => {
    expect(detectActivePreset({ to: "2026-07-23" })).toBe("custom");
    expect(
      detectActivePreset({ from: "2026-06-24", to: "2026-07-23" }),
    ).toBe("custom");
  });

  it("round-trips every rolling preset produced by rangeForPreset", () => {
    for (const option of DATE_PRESET_OPTIONS) {
      expect(detectActivePreset(rangeForPreset(option.key))).toBe(
        option.key,
      );
    }
  });

  it("returns custom when 'from' does not match any known preset", () => {
    expect(detectActivePreset({ from: "2020-01-01" })).toBe("custom");
  });
});
