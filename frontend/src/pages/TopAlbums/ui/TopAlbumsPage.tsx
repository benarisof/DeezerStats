import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { getTopAlbums } from "@/entities/album/api/albumApi";
import { useDateRangeParams } from "@/features/date-range-filter/model/useDateRangeParams";
import { formatCount } from "@/shared/lib/formatCount";
import { ErrorState } from "@/shared/ui/ErrorState";
import { MediaCard } from "@/shared/ui/MediaCard";
import { Pagination } from "@/shared/ui/Pagination";
import { Spinner } from "@/shared/ui/Spinner";

/** Taille de page augmentée par rapport au défaut backend (20, voir AlbumsController) : des cartes
 * plus compactes (voir la grille ci-dessous) permettent d'en montrer davantage sans scroller plus. */
const PAGE_SIZE = 25;

export function TopAlbumsPage() {
  const { range } = useDateRangeParams();
  const [page, setPage] = useState(1);

  useEffect(() => setPage(1), [range.from, range.to]);

  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ["albums", "top", range, page, PAGE_SIZE],
    queryFn: () => getTopAlbums({ ...range, page, pageSize: PAGE_SIZE }),
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
      <h1 className="text-xl font-semibold">Albums les plus écoutés</h1>
      <div className="grid grid-cols-3 gap-3 sm:grid-cols-4 sm:gap-4 md:grid-cols-5 lg:grid-cols-6 xl:grid-cols-7">
        {data?.items.map((album, index) => (
          <MediaCard
            key={album.id}
            variant="grid"
            shape="square"
            href={`/albums/${album.id}`}
            coverUrl={album.coverUrl}
            title={album.title}
            subtitle={album.artistName}
            meta={`${formatCount(album.playCount)} écoutes`}
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
