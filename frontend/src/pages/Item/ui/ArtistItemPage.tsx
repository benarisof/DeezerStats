import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { getArtistDetail } from "@/entities/artist/api/artistApi";
import { useDateRangeParams } from "@/features/date-range-filter/model/useDateRangeParams";
import { ErrorState } from "@/shared/ui/ErrorState";
import { Spinner } from "@/shared/ui/Spinner";

export function ArtistItemPage() {
  const { artistId } = useParams<{ artistId: string }>();
  const { range } = useDateRangeParams();

  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ["artists", "detail", artistId, range],
    queryFn: () => getArtistDetail(artistId!, range),
    enabled: Boolean(artistId),
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
        <h1 className="text-2xl text-foreground">{data.name}</h1>
        <p className="mt-1 text-sm text-muted-foreground">
          {data.totalPlayCount} écoutes · {data.distinctAlbumsCount} album(s) ·{" "}
          {data.distinctTracksCount} morceau(x) distinct(s) ·{" "}
          {data.totalListeningDurationHours.toFixed(1)} h d'écoute cumulée
        </p>
      </div>

      <ol className="flex flex-col divide-y divide-border">
        {data.tracks.map((track, index) => (
          <li key={track.id} className="flex items-center gap-3 px-2 py-2">
            <span className="w-8 text-muted-foreground">{index + 1}</span>
            <div className="flex-1 truncate">
              <p className="truncate text-foreground">{track.title}</p>
              <p className="truncate text-sm text-muted-foreground">
                {track.albumTitle}
              </p>
            </div>
            <span className="text-muted-foreground">
              {track.playCount} écoutes
            </span>
          </li>
        ))}
      </ol>
    </div>
  );
}
