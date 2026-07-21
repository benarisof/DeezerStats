using DeezerStats.Application.Ports.BackgroundJobs;
using DeezerStats.Domain.ValueObjects;
using DeezerStats.Infrastructure.BackgroundJobs;
using FluentAssertions;

namespace DeezerStats.Infrastructure.UnitTests.BackgroundJobs
{
    public class EnrichmentJobChannelTests
    {
        [Fact]
        public async Task EnqueueAsyncThenReadAllAsyncShouldReturnItemsInOrder()
        {
            // Arrange
            var channel = new EnrichmentJobChannel();
            var trackItem = new EnrichmentWorkItem.ForTrack(new Isrc("USCM51300736"));
            var albumItem = new EnrichmentWorkItem.ForAlbum(Guid.NewGuid());

            await channel.EnqueueAsync(trackItem);
            await channel.EnqueueAsync(albumItem);

            using var cts = new CancellationTokenSource();
            var received = new List<EnrichmentWorkItem>();

            // Act : on ne lit que les deux éléments planifiés, puis on arrête la lecture (le Channel
            // n'est jamais "complété" dans l'usage réel — voir EnrichmentBackgroundService, qui lit
            // en continu jusqu'à l'arrêt de l'hôte).
            await foreach (EnrichmentWorkItem item in channel.ReadAllAsync(cts.Token))
            {
                received.Add(item);
                if (received.Count == 2)
                {
                    break;
                }
            }

            // Assert
            received.Should().Equal(trackItem, albumItem);
        }
    }
}
