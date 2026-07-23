const formatter = new Intl.NumberFormat("fr-FR");

/** Formatte un nombre d'écoutes avec séparateur de milliers français (ex. 1234 -> "1 234"),
 * cohérent avec le formatage des dates déjà utilisé côté historique (voir HistoryPage). */
export function formatCount(value: number): string {
  return formatter.format(value);
}
