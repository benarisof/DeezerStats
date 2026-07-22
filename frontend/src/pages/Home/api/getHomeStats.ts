import { httpClient } from "@/shared/api/httpClient";
import { toQueryString, type DateRangeParams } from "@/shared/api/types";
import type { HomeStatsResponse } from "../model/types";

export function getHomeStats(
  range: DateRangeParams,
): Promise<HomeStatsResponse> {
  return httpClient.get(`/stats/home${toQueryString(range)}`);
}
