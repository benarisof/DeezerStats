import { useQuery } from "@tanstack/react-query";
import { useDateRangeParams } from "@/features/date-range-filter/model/useDateRangeParams";
import { formatCount } from "@/shared/lib/formatCount";
import { Carousel } from "@/shared/ui/Carousel";
import { ErrorState } from "@/shared/ui/ErrorState";
import { MediaCard } from "@/shared/ui/MediaCard";
import { Spinner } from "@/shared/ui/Spinner";
import { getHomeStats } from "../api/getHomeStats";

function EmptySection() {
  return (
    <p className="text-sm text-muted-foreground">
      Aucune écoute enregistrée sur cette période.
    </p>
  );
}

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
    <div className="flex flex-col gap-8">
      <section className="flex flex-col gap-3">
        <h2 className="text-xl font-semibold">Top albums</h2>
        {data && data.topAlbums.length === 0 ? (
          <EmptySection />
        ) : (
          <Carousel ariaLabel="Top albums">
            {data?.topAlbums.map((album) => (
              <MediaCard
                key={album.id}
                variant="carousel"
                shape="square"
                href={`/albums/${album.id}`}
                coverUrl={album.coverUrl}
                title={album.title}
                subtitle={album.artistName}
                meta={`${formatCount(album.playCount)} écoutes`}
              />
            ))}
          </Carousel>
        )}
      </section>

      <section className="flex flex-col gap-3">
        <h2 className="text-xl font-semibold">Top artistes</h2>
        {data && data.topArtists.length === 0 ? (
          <EmptySection />
        ) : (
          <Carousel ariaLabel="Top artistes">
            {data?.topArtists.map((artist) => (
              <MediaCard
                key={artist.id}
                variant="carousel"
                shape="circle"
                href={`/artists/${artist.id}`}
                coverUrl={artist.coverUrl}
                title={artist.name}
                meta={`${formatCount(artist.playCount)} écoutes`}
              />
            ))}
          </Carousel>
        )}
      </section>

      <section className="flex flex-col gap-3">
        <h2 className="text-xl font-semibold">Top morceaux</h2>
        {data && data.topTracks.length === 0 ? (
          <EmptySection />
        ) : (
          <Carousel ariaLabel="Top morceaux">
            {data?.topTracks.map((track) => (
              <MediaCard
                key={track.id}
                variant="carousel"
                shape="square"
                coverUrl={track.coverUrl}
                title={track.title}
                subtitle={`${track.artistName} — ${track.albumTitle}`}
                meta={`${formatCount(track.playCount)} écoutes`}
              />
            ))}
          </Carousel>
        )}
      </section>
    </div>
  );
}
