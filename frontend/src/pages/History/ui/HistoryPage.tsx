import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { getHistory } from "@/entities/track/api/trackApi";
import { useDateRangeParams } from "@/features/date-range-filter/model/useDateRangeParams";
import { ErrorState } from "@/shared/ui/ErrorState";
import { Pagination } from "@/shared/ui/Pagination";
import { Spinner } from "@/shared/ui/Spinner";

const dateTimeFormatter = new Intl.DateTimeFormat("fr-FR", {
  dateStyle: "medium",
  timeStyle: "short",
});

export function HistoryPage() {
  const { range } = useDateRangeParams();
  const [page, setPage] = useState(1);

  useEffect(() => setPage(1), [range.from, range.to]);

  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ["history", range, page],
    queryFn: () => getHistory({ ...range, page }),
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
      <h1 className="text-xl">Historique d'écoute</h1>
      <ol className="flex flex-col divide-y divide-border">
        {data?.items.map((entry) => (
          <li key={entry.id} className="flex items-center gap-3 px-2 py-2">
            <div className="flex-1 truncate">
              <p className="truncate text-foreground">{entry.title}</p>
              <p className="truncate text-sm text-muted-foreground">
                {entry.artistName} — {entry.albumTitle}
              </p>
            </div>
            <span className="text-sm text-muted-foreground">
              {dateTimeFormatter.format(new Date(entry.listenedAt))}
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
