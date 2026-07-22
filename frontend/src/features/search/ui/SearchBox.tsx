import { useRef, useState, type KeyboardEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { useDebouncedValue } from "@/shared/lib/useDebouncedValue";
import { Spinner } from "@/shared/ui/Spinner";
import {
  getSearchSuggestions,
  SEARCH_MIN_QUERY_LENGTH,
} from "../api/searchApi";
import type { SearchSuggestion } from "../model/types";

function suggestionHref(suggestion: SearchSuggestion): string | null {
  switch (suggestion.type) {
    case "album":
      return `/albums/${suggestion.id}`;
    case "artist":
      return `/artists/${suggestion.id}`;
    default:
      // Un morceau n'a pas de page dédiée (voir PLAN.md) : on renvoie vers les résultats complets.
      return null;
  }
}

export function SearchBox() {
  const navigate = useNavigate();
  const [query, setQuery] = useState("");
  const [isOpen, setIsOpen] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  const debouncedQuery = useDebouncedValue(query, 250);
  const shouldFetch = debouncedQuery.trim().length >= SEARCH_MIN_QUERY_LENGTH;

  const { data: suggestions, isFetching } = useQuery({
    queryKey: ["search-suggestions", debouncedQuery],
    queryFn: () => getSearchSuggestions(debouncedQuery.trim()),
    enabled: shouldFetch,
  });

  function goToResults(searchQuery: string) {
    if (!searchQuery.trim()) {
      return;
    }

    setIsOpen(false);
    inputRef.current?.blur();
    navigate(`/search?q=${encodeURIComponent(searchQuery.trim())}`);
  }

  function handleSuggestionClick(suggestion: SearchSuggestion) {
    const href = suggestionHref(suggestion);
    setIsOpen(false);

    if (href) {
      navigate(href);
    } else {
      goToResults(suggestion.label);
    }
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === "Enter") {
      goToResults(query);
    } else if (event.key === "Escape") {
      setIsOpen(false);
    }
  }

  const showDropdown = isOpen && shouldFetch;

  return (
    <div className="relative w-full max-w-sm">
      <input
        ref={inputRef}
        type="search"
        placeholder="Rechercher un album, artiste, morceau..."
        className="w-full rounded-md border border-border bg-background px-3 py-2 text-sm text-foreground outline-none focus:border-accent-border focus:ring-2 focus:ring-accent-bg"
        value={query}
        onChange={(event) => {
          setQuery(event.target.value);
          setIsOpen(true);
        }}
        onFocus={() => setIsOpen(true)}
        onBlur={() => setTimeout(() => setIsOpen(false), 150)}
        onKeyDown={handleKeyDown}
      />

      {showDropdown && (
        <ul className="absolute z-10 mt-1 max-h-80 w-full overflow-auto rounded-md border border-border bg-background shadow-lg">
          {isFetching && (
            <li className="flex items-center justify-center p-3">
              <Spinner />
            </li>
          )}

          {!isFetching && suggestions?.length === 0 && (
            <li className="p-3 text-center text-sm text-muted-foreground">
              Aucun résultat
            </li>
          )}

          {!isFetching &&
            suggestions?.map((suggestion) => (
              <li key={`${suggestion.type}-${suggestion.id}`}>
                <button
                  type="button"
                  className="flex w-full items-center gap-2 px-3 py-2 text-left text-sm hover:bg-surface"
                  onMouseDown={(event) => event.preventDefault()}
                  onClick={() => handleSuggestionClick(suggestion)}
                >
                  <span className="flex-1 truncate text-foreground">
                    {suggestion.label}
                  </span>
                  {suggestion.subtitle && (
                    <span className="truncate text-xs text-muted-foreground">
                      {suggestion.subtitle}
                    </span>
                  )}
                </button>
              </li>
            ))}
        </ul>
      )}
    </div>
  );
}
