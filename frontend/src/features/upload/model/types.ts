/** Miroir de ImportRowError (openapi.yaml). */
export interface ImportRowError {
  rowNumber: number;
  message: string;
}

/** Miroir de ImportReport. */
export interface ImportReport {
  importedCount: number;
  skippedCount: number;
  errorCount: number;
  errors: ImportRowError[];
}
