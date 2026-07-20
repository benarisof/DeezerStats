namespace DeezerStats.Application.DTOs
{
    public record ImportResultDto(
        int ImportedCount,
        int DuplicateCount,
        int ErrorCount,
        IReadOnlyList<ImportErrorDto> Errors);
}
