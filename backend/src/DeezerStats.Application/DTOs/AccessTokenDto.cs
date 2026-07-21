namespace DeezerStats.Application.DTOs
{
    public record AccessTokenDto(
            string Token,
            DateTime ExpiresAt);
}
