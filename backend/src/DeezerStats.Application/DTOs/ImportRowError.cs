namespace DeezerStats.Application.DTOs
{
    /// <summary>
    /// Décrit une ligne du fichier d'import rejetée. Noms alignés sur le schéma ImportRowError du
    /// contrat OpenAPI (voir docs/api/openapi.yaml, POST /imports).
    /// </summary>
    public record ImportRowError(int RowNumber, string Message);
}
