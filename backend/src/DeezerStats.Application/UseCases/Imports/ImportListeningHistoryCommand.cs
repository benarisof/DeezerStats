namespace DeezerStats.Application.UseCases.Imports
{
    public record ImportListeningHistoryCommand(Guid UserId, Stream FileStream);
}
