using System.Threading.Channels;
using DeezerStats.Application.Ports.BackgroundJobs;

namespace DeezerStats.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Implémentation de l'enrichissement en tâche de fond adossée à un <see cref="Channel{T}"/> en
    /// mémoire (voir IEnrichmentJobScheduler / IEnrichmentJobReader). Enregistrée en Singleton (voir
    /// Infrastructure.DependencyInjection) : producteurs (imports, Scoped) et consommateur
    /// (EnrichmentBackgroundService, Singleton) partagent la même instance.
    ///
    /// Une capacité bornée avec <see cref="BoundedChannelFullMode.Wait"/> évite qu'un import massif
    /// ne fasse croître la file indéfiniment en mémoire : au-delà de la capacité, un import
    /// attendrait qu'une place se libère plutôt que de la dépasser.
    /// </summary>
    public class EnrichmentJobChannel : IEnrichmentJobScheduler, IEnrichmentJobReader
    {
        private readonly Channel<EnrichmentWorkItem> _channel = Channel.CreateBounded<EnrichmentWorkItem>(
            new BoundedChannelOptions(capacity: 1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
            });

        public async ValueTask EnqueueAsync(EnrichmentWorkItem item, CancellationToken ct = default) =>
            await _channel.Writer.WriteAsync(item, ct);

        public IAsyncEnumerable<EnrichmentWorkItem> ReadAllAsync(CancellationToken ct = default) =>
            _channel.Reader.ReadAllAsync(ct);
    }
}
