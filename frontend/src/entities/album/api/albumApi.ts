import { httpClient } from "@/shared/api/httpClient";
import {
  toQueryString,
  type DateRangeParams,
  type PagedResult,
} from "@/shared/api/types";
import type { AlbumDetail, AlbumSummary } from "../model/types";

export interface GetTopAlbumsParams extends DateRangeParams {
  page?: number;
  pageSize?: number;
}

export function getTopAlbums(
  params: GetTopAlbumsParams,
): Promise<PagedResult<AlbumSummary>> {
  return httpClient.get(`/albums/top${toQueryString(params)}`);
}

export function getAlbumDetail(
  albumId: string,
  dateRange: DateRangeParams,
): Promise<AlbumDetail> {
  return httpClient.get(`/albums/${albumId}${toQueryString(dateRange)}`);
}
