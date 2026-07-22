import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { searchCatalog } from "@/features/search/api/searchApi";
import { ErrorState } from "@/shared/ui/ErrorState";
import { Pagination } from "@/shared/ui/Pagination";
import { Spinner } from "@/shared/ui/Spinner";

function hrefFor(type: string, id: string): string | null {
  if (type === "album") {
    return `/albums/${id}`;
  }

  if (type === "artist") {
    return `/artists/${id}`;
  }

  return null;
}

export function SearchResultsPage() {
  const [searchParams] = useSearchParams();
  const query = searchParams.get("q") ?? "";
  const [page, setPage] = useState(1);

  useEffect(() => setPage(1), [query]);

  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ["search", query, page],
    queryFn: () => searchCatalog(query, page),
    enabled: query.trim().length > 0,
  });

  if (!query.trim()) {
    return (
      <p className="text-muted-foreground">
        Saisis une recherche dans la barre en haut.
      </p>
    );
  }

  if (isLoading) {
    return (
      <div className="flex flex-1 items-center justify-center">
        <Spinner className="h-6 w-6" />
      </div>
    );
  }

  if (isError) {
    return <ErrorState error={error} onRetry={() => void refetch()} />;
  }

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-xl">
        Résultats pour « {query} »{" "}
        {data && (
          <span className="text-muted-foreground">({data.totalItems})</span>
        )}
      </h1>

      {data?.items.length === 0 && (
        <p className="text-muted-foreground">Aucun résultat.</p>
      )}

      <ol className="flex flex-col divide-y divide-border">
        {data?.items.map((item) => {
          const href = hrefFor(item.type, item.id);
          const content = (
            <>
              <span className="flex-1 truncate text-foreground">
                {item.label}
              </span>
              {item.subtitle && (
                <span className="truncate text-sm text-muted-foreground">
                  {item.subtitle}
                </span>
              )}
            </>
          );

          return (
            <li key={`${item.type}-${item.id}`}>
              {href ? (
                <Link
                  to={href}
                  className="flex items-center gap-3 px-2 py-2 hover:bg-surface"
                >
                  {content}
                </Link>
              ) : (
                <div className="flex items-center gap-3 px-2 py-2">
                  {content}
                </div>
              )}
            </li>
          );
        })}
      </ol>

      {data && (
        <Pagination
          page={data.page}
          totalPages={data.totalPages}
          onPageChange={setPage}
        />
      )}
    </div>
  );
}
