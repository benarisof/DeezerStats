import { useCallback, useMemo } from "react";
import { useSearchParams } from "react-router-dom";
import type { DateRangeParams } from "@/shared/api/types";

/** Synchronise la plage de dates avec les query params `from`/`to` de l'URL (yyyy-MM-dd) : source de
 * vérité unique, partagée par toutes les pages de consultation (voir ticket 12.7 du plan) et
 * bookmarkable/partageable puisqu'elle vit dans l'URL plutôt que dans un store en mémoire. */
export function useDateRangeParams() {
  const [searchParams, setSearchParams] = useSearchParams();

  const range: DateRangeParams = useMemo(
    () => ({
      from: searchParams.get("from") ?? undefined,
      to: searchParams.get("to") ?? undefined,
    }),
    [searchParams],
  );

  const setRange = useCallback(
    (next: DateRangeParams) => {
      setSearchParams(
        (previous) => {
          const params = new URLSearchParams(previous);

          for (const key of ["from", "to"] as const) {
            const value = next[key];
            if (value) {
              params.set(key, value);
            } else {
              params.delete(key);
            }
          }

          return params;
        },
        { replace: true },
      );
    },
    [setSearchParams],
  );

  return { range, setRange };
}
