/** Miroir de TrackSummary (openapi.yaml). */
export interface TrackSummary {
  id: string;
  title: string;
  artistName: string;
  albumTitle: string;
  coverUrl: string | null;
  playCount: number;
}

/** Miroir de HistoryEntry. */
export interface HistoryEntry {
  id: string;
  trackId: string;
  title: string;
  artistName: string;
  albumTitle: string;
  coverUrl: string | null;
  listenedAt: string;
}
