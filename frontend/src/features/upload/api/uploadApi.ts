import { httpClient } from "@/shared/api/httpClient";
import type { ImportReport } from "../model/types";

export function uploadListeningHistory(file: File): Promise<ImportReport> {
  const formData = new FormData();
  formData.set("file", file);

  return httpClient.postForm("/imports", formData);
}
