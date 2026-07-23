import { create } from "zustand";
import type { DateRangeParams } from "@/shared/api/types";

interface DateRangeState {
  range: DateRangeParams;
  setRange: (range: DateRangeParams) => void;
}

/** État global de la plage de dates, en mémoire (pas dans l'URL, pas persisté sur disque) : le
 * sélecteur vit dans le header (voir widgets/header/ui/Header.tsx) et doit donc s'appliquer à
 * toutes les pages de consultation, y compris en changeant de page (navigation client-side, qui ne
 * démonte jamais ce store). Se réinitialise uniquement lors d'un rechargement complet de la page
 * (F5) puisqu'un store Zustand non persisté est recréé à chaque nouveau contexte JS -- comportement
 * voulu, pas un oubli. */
const useDateRangeStore = create<DateRangeState>((set) => ({
  range: {},
  setRange: (range) => set({ range }),
}));

/** Synchronise la plage de dates avec le store global (voir useDateRangeStore), partagé par toutes
 * les pages de consultation (voir ticket 12.7 du plan). */
export function useDateRangeParams() {
  const range = useDateRangeStore((state) => state.range);
  const setRange = useDateRangeStore((state) => state.setRange);

  return { range, setRange };
}
