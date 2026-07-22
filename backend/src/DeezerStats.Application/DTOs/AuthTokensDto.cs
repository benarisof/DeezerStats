namespace DeezerStats.Application.DTOs
{
    /// <summary>
    /// Couple (access token, refresh token) retourné par register/login/refresh — voir le schéma
    /// AuthTokens du contrat OpenAPI.
    /// </summary>
    public record AuthTokensDto(
        string AccessToken,
        string RefreshToken,
        int ExpiresInSeconds);
}
