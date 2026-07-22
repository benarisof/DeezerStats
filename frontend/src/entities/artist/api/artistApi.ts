import { httpClient } from "@/shared/api/httpClient";
import {
  toQueryString,
  type DateRangeParams,
  type PagedResult,
} from "@/shared/api/types";
import type { ArtistDetail, ArtistSummary } from "../model/types";

export interface GetTopArtistsParams extends DateRangeParams {
  page?: number;
  pageSize?: number;
}

export function getTopArtists(
  params: GetTopArtistsParams,
): Promise<PagedResult<ArtistSummary>> {
  return httpClient.get(`/artists/top${toQueryString(params)}`);
}

export function getArtistDetail(
  artistId: string,
  dateRange: DateRangeParams,
): Promise<ArtistDetail> {
  return httpClient.get(`/artists/${artistId}${toQueryString(dateRange)}`);
}
