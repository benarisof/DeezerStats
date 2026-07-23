import type { DateRangeParams } from "@/shared/api/types";

/** Clés des périodes prédéfinies proposées en plus de la sélection manuelle (voir DateRangeFilter).
 * "custom" n'est jamais choisie par l'utilisateur directement : elle apparaît uniquement quand la
 * plage actuelle (issue de l'URL, potentiellement modifiée via les champs "Du"/"au") ne correspond
 * à aucune période prédéfinie. */
export type DatePresetKey =
  "last30" | "last90" | "last365" | "allTime" | "custom";

const PRESET_DAYS: Record<"last30" | "last90" | "last365", number> = {
  last30: 30,
  last90: 90,
  last365: 365,
};

export const DATE_PRESET_OPTIONS: ReadonlyArray<{
  key: "last30" | "last90" | "last365" | "allTime";
  label: string;
}> = [
  { key: "last30", label: "30 derniers jours" },
  { key: "last90", label: "90 derniers jours" },
  { key: "last365", label: "365 derniers jours" },
  { key: "allTime", label: "Depuis le début" },
];

/** Formate en yyyy-MM-dd à partir des composants de date LOCALE : `Date.toISOString()` convertit en
 * UTC, ce qui décale la date d'un jour dès que l'heure locale et l'heure UTC ne tombent pas le même
 * jour calendaire -- exactement le cas ayant provoqué un "30 derniers jours" démarrant la veille de
 * la date attendue lors de la vérification manuelle de cette fonctionnalité. */
function toIsoDate(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

/** Calcule la date de début (incluse) d'une période glissante de `days` jours se terminant
 * aujourd'hui (ex. 30 -> aujourd'hui inclus + les 29 jours précédents). `to` reste volontairement
 * absent : l'API traite "to" absent comme "jusqu'à aujourd'hui" (voir StatsController), pas besoin
 * de le fixer explicitement côté client. */
function presetFromDate(days: number): string {
  const date = new Date();
  date.setDate(date.getDate() - (days - 1));
  return toIsoDate(date);
}

/** Traduit une période prédéfinie en plage from/to prête à passer à useDateRangeParams().setRange. */
export function rangeForPreset(
  key: Exclude<DatePresetKey, "custom">,
): DateRangeParams {
  if (key === "allTime") {
    return { from: undefined, to: undefined };
  }
  return { from: presetFromDate(PRESET_DAYS[key]), to: undefined };
}

/** Détermine quelle période prédéfinie correspond à la plage actuelle, pour refléter l'état dans le
 * menu déroulant (ex. après un rechargement de page, ou une modification manuelle des champs de
 * date). Une plage sans aucune borne correspond à "Depuis le début" ; toute plage avec un "to" ne
 * peut jamais correspondre à une période glissante (celles-ci ne fixent jamais "to") et est donc
 * toujours "custom". */
export function detectActivePreset(range: DateRangeParams): DatePresetKey {
  if (range.to) {
    return "custom";
  }
  if (!range.from) {
    return "allTime";
  }
  for (const [key, days] of Object.entries(PRESET_DAYS) as Array<
    [keyof typeof PRESET_DAYS, number]
  >) {
    if (range.from === presetFromDate(days)) {
      return key;
    }
  }
  return "custom";
}
