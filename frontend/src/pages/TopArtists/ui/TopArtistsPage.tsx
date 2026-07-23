import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { getTopArtists } from "@/entities/artist/api/artistApi";
import { useDateRangeParams } from "@/features/date-range-filter/model/useDateRangeParams";
import { formatCount } from "@/shared/lib/formatCount";
import { ErrorState } from "@/shared/ui/ErrorState";
import { MediaCard } from "@/shared/ui/MediaCard";
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
      <h1 className="text-xl font-semibold">Artistes les plus écoutés</h1>
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 sm:gap-5 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6">
        {data?.items.map((artist, index) => (
          <MediaCard
            key={artist.id}
            variant="grid"
            shape="circle"
            href={`/artists/${artist.id}`}
            coverUrl={artist.coverUrl}
            title={artist.name}
            meta={`${formatCount(artist.playCount)} écoutes`}
            rank={(page - 1) * (data.pageSize || 20) + index + 1}
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
