import { create } from "zustand";
import type { DateRangeParams } from "@/shared/api/types";

interface DateRangeState {
  range: DateRangeParams;
  setRange: (range: DateRangeParams) => void;
}

/** État global en mémoire (pas dans l'URL, pas persisté) : le sélecteur vit dans le header et
 * s'applique à toutes les pages de consultation. Se réinitialise au rechargement complet (F5) --
 * comportement voulu, pas un oubli. */
const useDateRangeStore = create<DateRangeState>((set) => ({
  range: {},
  setRange: (range) => set({ range }),
}));

export function useDateRangeParams() {
  const range = useDateRangeStore((state) => state.range);
  const setRange = useDateRangeStore((state) => state.setRange);

  return { range, setRange };
}
