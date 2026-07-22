import type { PagedResult } from "@/shared/api/types";

/** Miroir de SearchSuggestion (openapi.yaml) : un résultat "plat", quel que soit son type réel. */
export interface SearchSuggestion {
  id: string;
  type: "album" | "artist" | "track";
  label: string;
  subtitle: string | null;
  coverUrl: string | null;
}

export type SearchResultsPage = PagedResult<SearchSuggestion>;
