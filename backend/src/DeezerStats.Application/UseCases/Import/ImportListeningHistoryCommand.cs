namespace DeezerStats.Application.UseCases.Import
{
    public record ImportListeningHistoryCommand(Guid UserId, Stream FileStream);
}
