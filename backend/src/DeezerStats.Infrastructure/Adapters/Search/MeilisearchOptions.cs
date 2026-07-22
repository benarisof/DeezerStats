namespace DeezerStats.Infrastructure.Adapters.Search
{
    public class MeilisearchOptions
    {
        public const string SectionName = "Meilisearch";

        public string Url { get; set; } = string.Empty;

        public string MasterKey { get; set; } = string.Empty;

        public string IndexName { get; set; } = "catalog";
    }
}
