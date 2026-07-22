/** Miroir du schéma ProblemDetails du contrat OpenAPI (RFC 7807), renvoyé par le middleware
 * d'exceptions de l'API sur toute erreur. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

/** Erreur levée par `httpClient` pour toute réponse HTTP non-2xx, avec le ProblemDetails
 * correspondant quand l'API en a renvoyé un (toujours le cas, sauf panne réseau). */
export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails | null;

  constructor(status: number, problem: ProblemDetails | null) {
    super(problem?.detail ?? problem?.title ?? `Erreur HTTP ${status}`);
    this.name = "ApiError";
    this.status = status;
    this.problem = problem;
  }

  /** Erreurs de validation par champ (voir ExceptionHandlingMiddleware, ValidationException). */
  get fieldErrors(): Record<string, string[]> {
    return this.problem?.errors ?? {};
  }
}
