import { afterEach, describe, expect, it, vi } from "vitest";
import { act, fireEvent, render, screen } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import * as uploadApi from "../api/uploadApi";
import { UploadButton, RESULT_DISMISS_DELAY_MS } from "./UploadButton";

/** vi.advanceTimersByTimeAsync flushe les micro-tâches (promesses) à chaque pas, mais les mises à
 * jour d'état React qui en découlent (ex. le setTimeout de disparition automatique) doivent être
 * explicitement enveloppées dans act() pour être commises avant les assertions suivantes. */
async function advanceTimers(ms: number): Promise<void> {
  await act(async () => {
    await vi.advanceTimersByTimeAsync(ms);
  });
}

function renderWithQueryClient() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <UploadButton />
    </QueryClientProvider>,
  );
}

function selectFile(): void {
  const file = new File(["dummy"], "history.xlsx", {
    type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  });
  const input = document.querySelector('input[type="file"]');
  if (!input) {
    throw new Error("File input not found.");
  }
  fireEvent.change(input, { target: { files: [file] } });
}

describe("UploadButton", () => {
  afterEach(() => {
    vi.restoreAllMocks();
    vi.useRealTimers();
  });

  it("shows the import result then dismisses it automatically after a few seconds", async () => {
    vi.useFakeTimers();
    vi.spyOn(uploadApi, "uploadListeningHistory").mockResolvedValue({
      importedCount: 3,
      skippedCount: 1,
      errorCount: 0,
      errors: [],
    });

    renderWithQueryClient();
    selectFile();

    // Laisse la promesse de mutation se résoudre (et React re-rendre) sans faire avancer le délai
    // d'auto-disparition -- advanceTimersByTimeAsync(0) flushe les micro-tâches en attente.
    await advanceTimers(0);

    expect(
      screen.getByText(/3 morceau\(x\) importé\(s\), 1 déjà connu\(s\)\./),
    ).toBeInTheDocument();

    await advanceTimers(RESULT_DISMISS_DELAY_MS);

    expect(
      screen.queryByText(/3 morceau\(x\) importé\(s\)/),
    ).not.toBeInTheDocument();
  });

  it("does not dismiss the result before the delay has elapsed", async () => {
    vi.useFakeTimers();
    vi.spyOn(uploadApi, "uploadListeningHistory").mockResolvedValue({
      importedCount: 3,
      skippedCount: 0,
      errorCount: 0,
      errors: [],
    });

    renderWithQueryClient();
    selectFile();
    await advanceTimers(0);

    await advanceTimers(RESULT_DISMISS_DELAY_MS - 1000);

    expect(screen.getByText(/3 morceau\(x\) importé\(s\)/)).toBeInTheDocument();
  });

  it("shows an error message and dismisses it automatically too", async () => {
    vi.useFakeTimers();
    vi.spyOn(uploadApi, "uploadListeningHistory").mockRejectedValue(
      new Error("Import impossible."),
    );

    renderWithQueryClient();
    selectFile();
    await advanceTimers(0);

    expect(screen.getByText("Import impossible.")).toBeInTheDocument();

    await advanceTimers(RESULT_DISMISS_DELAY_MS);

    expect(screen.queryByText("Import impossible.")).not.toBeInTheDocument();
  });
});
