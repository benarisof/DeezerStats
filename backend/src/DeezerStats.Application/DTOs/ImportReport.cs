namespace DeezerStats.Application.DTOs
{
    /// <summary>
    /// Rapport d'import d'un fichier Excel d'historique d'écoute. Noms alignés sur le schéma
    /// ImportReport du contrat OpenAPI (voir docs/api/openapi.yaml, POST /imports).
    /// </summary>
    public record ImportReport(
        int ImportedCount,
        int SkippedCount,
        int ErrorCount,
        IReadOnlyList<ImportRowError> Errors);
}
