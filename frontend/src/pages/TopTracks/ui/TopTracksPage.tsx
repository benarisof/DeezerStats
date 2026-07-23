import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { getTopTracks } from "@/entities/track/api/trackApi";
import { useDateRangeParams } from "@/features/date-range-filter/model/useDateRangeParams";
import { formatCount } from "@/shared/lib/formatCount";
import { ErrorState } from "@/shared/ui/ErrorState";
import { MediaCard } from "@/shared/ui/MediaCard";
import { Pagination } from "@/shared/ui/Pagination";
import { Spinner } from "@/shared/ui/Spinner";

/** Taille de page augmentée par rapport au défaut backend (20, voir TracksController) — voir aussi
 * TopAlbumsPage/TopArtistsPage, qui appliquent la même valeur. */
const PAGE_SIZE = 25;

export function TopTracksPage() {
  const { range } = useDateRangeParams();
  const [page, setPage] = useState(1);

  useEffect(() => setPage(1), [range.from, range.to]);

  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ["tracks", "top", range, page, PAGE_SIZE],
    queryFn: () => getTopTracks({ ...range, page, pageSize: PAGE_SIZE }),
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
      <h1 className="text-xl font-semibold">Morceaux les plus écoutés</h1>
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 sm:gap-5 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6">
        {data?.items.map((track, index) => (
          <MediaCard
            key={track.id}
            variant="grid"
            shape="square"
            coverUrl={track.coverUrl}
            title={track.title}
            subtitle={`${track.artistName} — ${track.albumTitle}`}
            meta={`${formatCount(track.playCount)} écoutes`}
            rank={(page - 1) * (data.pageSize || PAGE_SIZE) + index + 1}
          />
        ))}
      </div>
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
