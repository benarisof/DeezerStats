import { useEffect, useRef, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Button } from "@/shared/ui/Button";
import { Spinner } from "@/shared/ui/Spinner";
import { ApiError } from "@/shared/api/apiError";
import { uploadListeningHistory } from "../api/uploadApi";
import type { ImportReport } from "../model/types";

/** Durée d'affichage du résultat d'import avant disparition automatique (voir l'effet ci-dessous).
 * Exportée pour que le test associé puisse avancer précisément l'horloge simulée jusqu'à ce délai. */
export const RESULT_DISMISS_DELAY_MS = 6000;

export function UploadButton() {
  const queryClient = useQueryClient();
  const inputRef = useRef<HTMLInputElement>(null);
  const [report, setReport] = useState<ImportReport | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Le résultat (succès ou erreur) ne doit pas rester affiché indéfiniment : il disparaît de
  // lui-même après quelques secondes, comme une notification classique.
  useEffect(() => {
    if (!report && !error) {
      return;
    }

    const timeoutId = window.setTimeout(() => {
      setReport(null);
      setError(null);
    }, RESULT_DISMISS_DELAY_MS);

    return () => window.clearTimeout(timeoutId);
  }, [report, error]);

  const mutation = useMutation({
    mutationFn: uploadListeningHistory,
    onSuccess: (result) => {
      setReport(result);
      setError(null);
      // Un import modifie les tops, l'historique et les stats d'accueil : on invalide tout ce qui
      // en dépend plutôt que de traquer chaque clé de requête individuellement.
      void queryClient.invalidateQueries({ queryKey: ["albums"] });
      void queryClient.invalidateQueries({ queryKey: ["artists"] });
      void queryClient.invalidateQueries({ queryKey: ["tracks"] });
      void queryClient.invalidateQueries({ queryKey: ["history"] });
      void queryClient.invalidateQueries({ queryKey: ["home-stats"] });
    },
    onError: (mutationError) => {
      setReport(null);
      setError(
        mutationError instanceof ApiError
          ? mutationError.message
          : "Import impossible.",
      );
    },
  });

  function handleFileChange() {
    const file = inputRef.current?.files?.[0];

    if (file) {
      setReport(null);
      setError(null);
      mutation.mutate(file);
    }

    if (inputRef.current) {
      inputRef.current.value = "";
    }
  }

  return (
    <div className="relative">
      <input
        ref={inputRef}
        type="file"
        accept=".xlsx"
        className="hidden"
        onChange={handleFileChange}
      />
      <Button
        type="button"
        variant="secondary"
        shape="pill"
        disabled={mutation.isPending}
        onClick={() => inputRef.current?.click()}
      >
        {mutation.isPending ? <Spinner /> : null}
        Importer historique
      </Button>

      {(report || error) && (
        <div className="absolute right-0 z-10 mt-2 w-72 rounded-md border border-border bg-background p-3 text-sm shadow-lg">
          {error && <p className="text-danger">{error}</p>}
          {report && (
            <div className="flex flex-col gap-1">
              <p className="text-foreground">
                {report.importedCount} morceau(x) importé(s),{" "}
                {report.skippedCount} déjà connu(s).
              </p>
              {report.errorCount > 0 && (
                <p className="text-danger">
                  {report.errorCount} ligne(s) en erreur.
                </p>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
