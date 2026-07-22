import type { AlbumSummary } from "@/entities/album/model/types";
import type { ArtistSummary } from "@/entities/artist/model/types";
import type { TrackSummary } from "@/entities/track/model/types";

/** Miroir de HomeStatsResponse (openapi.yaml) : agrège les tops 10 de chaque entité, propre à la
 * page d'accueil (pas un concept réutilisé ailleurs, donc pas placé dans entities/). */
export interface HomeStatsResponse {
  topAlbums: AlbumSummary[];
  topArtists: ArtistSummary[];
  topTracks: TrackSummary[];
}
