/** Miroir de ArtistSummary (openapi.yaml). */
export interface ArtistSummary {
  id: string;
  name: string;
  coverUrl: string | null;
  playCount: number;
}

/** Miroir de ArtistTrackItem. */
export interface ArtistTrackItem {
  id: string;
  title: string;
  albumTitle: string;
  playCount: number;
}

/** Miroir de ArtistDetail. Pas de durée/date de sortie (voir ADR ticket 2.3 du backend) : remplacées
 * par des agrégats propres à l'artiste. */
export interface ArtistDetail {
  id: string;
  name: string;
  coverUrl: string | null;
  distinctAlbumsCount: number;
  distinctTracksCount: number;
  totalListeningDurationHours: number;
  totalPlayCount: number;
  tracks: ArtistTrackItem[];
}
