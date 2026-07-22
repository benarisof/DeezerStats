import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { getAlbumDetail } from "@/entities/album/api/albumApi";
import { useDateRangeParams } from "@/features/date-range-filter/model/useDateRangeParams";
import { ErrorState } from "@/shared/ui/ErrorState";
import { Spinner } from "@/shared/ui/Spinner";

export function AlbumItemPage() {
  const { albumId } = useParams<{ albumId: string }>();
  const { range } = useDateRangeParams();

  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ["albums", "detail", albumId, range],
    queryFn: () => getAlbumDetail(albumId!, range),
    enabled: Boolean(albumId),
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

  if (!data) {
    return null;
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl text-foreground">{data.title}</h1>
        <p className="text-muted-foreground">{data.artistName}</p>
        <p className="mt-1 text-sm text-muted-foreground">
          {data.totalPlayCount} écoutes ·{" "}
          {data.totalListeningDurationHours.toFixed(1)} h d'écoute cumulée
          {data.releaseDate && ` · sorti le ${data.releaseDate}`}
        </p>
      </div>

      <ol className="flex flex-col divide-y divide-border">
        {data.tracks.map((track, index) => (
          <li key={track.id} className="flex items-center gap-3 px-2 py-2">
            <span className="w-8 text-muted-foreground">{index + 1}</span>
            <span className="flex-1 truncate text-foreground">
              {track.title}
            </span>
            <span className="text-muted-foreground">
              {track.playCount} écoutes
            </span>
          </li>
        ))}
      </ol>
    </div>
  );
}
