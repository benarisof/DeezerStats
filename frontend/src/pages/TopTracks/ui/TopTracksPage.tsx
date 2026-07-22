import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { getTopTracks } from "@/entities/track/api/trackApi";
import { useDateRangeParams } from "@/features/date-range-filter/model/useDateRangeParams";
import { ErrorState } from "@/shared/ui/ErrorState";
import { Pagination } from "@/shared/ui/Pagination";
import { Spinner } from "@/shared/ui/Spinner";

export function TopTracksPage() {
  const { range } = useDateRangeParams();
  const [page, setPage] = useState(1);

  useEffect(() => setPage(1), [range.from, range.to]);

  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ["tracks", "top", range, page],
    queryFn: () => getTopTracks({ ...range, page }),
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
      <h1 className="text-xl">Morceaux les plus écoutés</h1>
      <ol className="flex flex-col divide-y divide-border">
        {data?.items.map((track, index) => (
          <li key={track.id} className="flex items-center gap-3 px-2 py-2">
            <span className="w-8 text-muted-foreground">
              {(page - 1) * (data.pageSize || 20) + index + 1}
            </span>
            <div className="flex-1 truncate">
              <p className="truncate text-foreground">{track.title}</p>
              <p className="truncate text-sm text-muted-foreground">
                {track.artistName} — {track.albumTitle}
              </p>
            </div>
            <span className="text-muted-foreground">
              {track.playCount} écoutes
            </span>
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
