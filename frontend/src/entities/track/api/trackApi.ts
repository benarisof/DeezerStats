import { httpClient } from "@/shared/api/httpClient";
import {
  toQueryString,
  type DateRangeParams,
  type PagedResult,
} from "@/shared/api/types";
import type { HistoryEntry, TrackSummary } from "../model/types";

export interface GetTopTracksParams extends DateRangeParams {
  page?: number;
  pageSize?: number;
}

export function getTopTracks(
  params: GetTopTracksParams,
): Promise<PagedResult<TrackSummary>> {
  return httpClient.get(`/tracks/top${toQueryString(params)}`);
}

export interface GetHistoryParams extends DateRangeParams {
  page?: number;
  pageSize?: number;
}

export function getHistory(
  params: GetHistoryParams,
): Promise<PagedResult<HistoryEntry>> {
  return httpClient.get(`/history${toQueryString(params)}`);
}
