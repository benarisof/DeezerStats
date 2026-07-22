/** Miroir du type générique `PagedResult<T>` du backend (voir DeezerStats.Application.DTOs.Stats),
 * réutilisé tel quel par tous les endpoints paginés (tops, historique). */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

/** Plage de dates optionnelle partagée par les endpoints de consultation (voir
 * features/date-range-filter) — `from`/`to` au format ISO `yyyy-MM-dd`. */
export interface DateRangeParams {
  from?: string;
  to?: string;
}

/** Accepte n'importe quel objet de paramètres (typé côté appelant via une interface dédiée, ex.
 * GetTopAlbumsParams) : `object` plutôt que `Record<string, ...>` pour éviter l'exigence de
 * signature d'index de TypeScript sur les interfaces nommées, non nécessaire ici. */
export function toQueryString(params: object): string {
  const search = new URLSearchParams();

  for (const [key, value] of Object.entries(params) as Array<
    [string, string | number | undefined]
  >) {
    if (value !== undefined && value !== "") {
      search.set(key, String(value));
    }
  }

  const query = search.toString();
  return query ? `?${query}` : "";
}
