/** Miroir de AlbumSummary (openapi.yaml). */
export interface AlbumSummary {
  id: string;
  title: string;
  artistName: string;
  coverUrl: string | null;
  playCount: number;
}

/** Miroir de AlbumTrackItem. */
export interface AlbumTrackItem {
  id: string;
  title: string;
  playCount: number;
}

/** Miroir de AlbumDetail. */
export interface AlbumDetail {
  id: string;
  title: string;
  artistId: string;
  artistName: string;
  coverUrl: string | null;
  durationSeconds: number | null;
  releaseDate: string | null;
  totalListeningDurationHours: number;
  totalPlayCount: number;
  tracks: AlbumTrackItem[];
}
