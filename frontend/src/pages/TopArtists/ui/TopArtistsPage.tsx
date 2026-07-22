import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { getTopArtists } from "@/entities/artist/api/artistApi";
import { useDateRangeParams } from "@/features/date-range-filter/model/useDateRangeParams";
import { ErrorState } from "@/shared/ui/ErrorState";
import { Pagination } from "@/shared/ui/Pagination";
import { Spinner } from "@/shared/ui/Spinner";

export function TopArtistsPage() {
  const { range } = useDateRangeParams();
  const [page, setPage] = useState(1);

  useEffect(() => setPage(1), [range.from, range.to]);

  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ["artists", "top", range, page],
    queryFn: () => getTopArtists({ ...range, page }),
  });

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
      <h1 className="text-xl">Artistes les plus écoutés</h1>
      <ol className="flex flex-col divide-y divide-border">
        {data?.items.map((artist, index) => (
          <li key={artist.id}>
            <Link
              to={`/artists/${artist.id}`}
              className="flex items-center gap-3 px-2 py-2 hover:bg-surface"
            >
              <span className="w-8 text-muted-foreground">
                {(page - 1) * (data.pageSize || 20) + index + 1}
              </span>
              <span className="flex-1 truncate text-foreground">
                {artist.name}
              </span>
              <span className="text-muted-foreground">
                {artist.playCount} écoutes
              </span>
            </Link>
          </li>
        ))}
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
