import { httpClient } from "@/shared/api/httpClient";
import { toQueryString } from "@/shared/api/types";
import type { SearchResultsPage, SearchSuggestion } from "../model/types";

/** Seuil d'autocomplétion (voir openapi.yaml, /search/suggestions : "au moins 4 caractères"). */
export const SEARCH_MIN_QUERY_LENGTH = 4;

export function getSearchSuggestions(
  query: string,
): Promise<SearchSuggestion[]> {
  return httpClient.get(`/search/suggestions${toQueryString({ q: query })}`);
}

export function searchCatalog(
  query: string,
  page = 1,
  pageSize = 20,
): Promise<SearchResultsPage> {
  return httpClient.get(
    `/search${toQueryString({ q: query, page, pageSize })}`,
  );
}
