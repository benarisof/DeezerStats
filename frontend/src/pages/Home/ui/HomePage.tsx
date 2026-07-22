import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { useDateRangeParams } from "@/features/date-range-filter/model/useDateRangeParams";
import { ErrorState } from "@/shared/ui/ErrorState";
import { Spinner } from "@/shared/ui/Spinner";
import { getHomeStats } from "../api/getHomeStats";

export function HomePage() {
  const { range } = useDateRangeParams();

  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ["home-stats", range],
    queryFn: () => getHomeStats(range),
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
    <div className="grid grid-cols-1 gap-6 md:grid-cols-3">
      <section className="flex flex-col gap-2">
        <h2 className="text-lg">Top albums</h2>
        <ol className="flex flex-col gap-1">
          {data?.topAlbums.map((album, index) => (
            <li key={album.id}>
              <Link
                to={`/albums/${album.id}`}
                className="flex items-center gap-2 rounded-md px-2 py-1.5 text-sm hover:bg-surface"
              >
                <span className="w-5 text-muted-foreground">{index + 1}</span>
                <span className="flex-1 truncate text-foreground">
                  {album.title}
                </span>
                <span className="text-muted-foreground">{album.playCount}</span>
              </Link>
            </li>
          ))}
        </ol>
      </section>

      <section className="flex flex-col gap-2">
        <h2 className="text-lg">Top artistes</h2>
        <ol className="flex flex-col gap-1">
          {data?.topArtists.map((artist, index) => (
            <li key={artist.id}>
              <Link
                to={`/artists/${artist.id}`}
                className="flex items-center gap-2 rounded-md px-2 py-1.5 text-sm hover:bg-surface"
              >
                <span className="w-5 text-muted-foreground">{index + 1}</span>
                <span className="flex-1 truncate text-foreground">
                  {artist.name}
                </span>
                <span className="text-muted-foreground">
                  {artist.playCount}
                </span>
              </Link>
            </li>
          ))}
        </ol>
      </section>

      <section className="flex flex-col gap-2">
        <h2 className="text-lg">Top morceaux</h2>
        <ol className="flex flex-col gap-1">
          {data?.topTracks.map((track, index) => (
            <li
              key={track.id}
              className="flex items-center gap-2 rounded-md px-2 py-1.5 text-sm"
            >
              <span className="w-5 text-muted-foreground">{index + 1}</span>
              <span className="flex-1 truncate text-foreground">
                {track.title}
              </span>
              <span className="text-muted-foreground">{track.playCount}</span>
            </li>
          ))}
        </ol>
      </section>
    </div>
  );
}
