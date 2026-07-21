using System.Runtime.CompilerServices;
using DeezerStats.Application.Ports.BackgroundJobs;
using DeezerStats.Application.UseCases.Albums;
using DeezerStats.Application.UseCases.Artists;
using DeezerStats.Application.UseCases.Tracks;
using DeezerStats.Domain.Aggregates.AlbumAggregate;
using DeezerStats.Domain.Aggregates.ArtistAggregate;
using DeezerStats.Domain.Aggregates.TrackAggregate;
using DeezerStats.Domain.ValueObjects;
using DeezerStats.Infrastructure.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace DeezerStats.Infrastructure.UnitTests.BackgroundJobs
{
    public class EnrichmentBackgroundServiceTests
    {
        private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

        [Fact]
        public async Task ExecuteAsyncShouldDispatchEachWorkItemToTheMatchingUseCase()
        {
            // Arrange
            var isrc = new Isrc("USCM51300736");
            var albumId = Guid.NewGuid();
            var artistId = Guid.NewGuid();

            IGetOrEnrichTrackUseCase trackUseCase = Substitute.For<IGetOrEnrichTrackUseCase>();
            IGetOrEnrichAlbumUseCase albumUseCase = Substitute.For<IGetOrEnrichAlbumUseCase>();
            IGetOrEnrichArtistUseCase artistUseCase = Substitute.For<IGetOrEnrichArtistUseCase>();

            // Le traitement d'arrière-plan est asynchrone : plutôt que de deviner un délai
            // d'attente arbitraire (source classique de tests instables), on synchronise sur un
            // signal explicite déclenché par le DERNIER élément de la séquence — voir
            // AndDoes ci-dessous — avant de vérifier les assertions et d'arrêter le service.
            var artistProcessed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            artistUseCase.ExecuteAsync(Arg.Any<GetOrEnrichArtistRequest>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Artist?>(null))
                .AndDoes(_ => artistProcessed.TrySetResult());

            var reader = new FakeEnrichmentJobReader([
                new EnrichmentWorkItem.ForTrack(isrc),
                new EnrichmentWorkItem.ForAlbum(albumId),
                new EnrichmentWorkItem.ForArtist(artistId),
            ]);

            EnrichmentBackgroundService service = CreateService(reader, trackUseCase, albumUseCase, artistUseCase);

            // Act
            await service.StartAsync(CancellationToken.None);
            await artistProcessed.Task.WaitAsync(_timeout);
            await service.StopAsync(CancellationToken.None);

            // Assert : chaque type d'élément est routé vers le use case d'enrichissement
            // correspondant, avec le bon identifiant.
            await trackUseCase.Received(1).ExecuteAsync(
                Arg.Is<GetOrEnrichTrackRequest>(r => r != null && r.Isrc == isrc),
                Arg.Any<CancellationToken>());
            await albumUseCase.Received(1).ExecuteAsync(
                Arg.Is<GetOrEnrichAlbumRequest>(r => r != null && r.AlbumId == albumId),
                Arg.Any<CancellationToken>());
            await artistUseCase.Received(1).ExecuteAsync(
                Arg.Is<GetOrEnrichArtistRequest>(r => r != null && r.ArtistId == artistId),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsyncWhenAnItemThrowsShouldStillProcessSubsequentItems()
        {
            // Arrange : le premier morceau fait échouer son enrichissement (ex. Deezer indisponible
            // au-delà de la résilience Polly) — voir EnrichmentBackgroundService, qui journalise
            // puis absorbe l'erreur : elle ne doit jamais interrompre le traitement du reste de la
            // file.
            var failingIsrc = new Isrc("USCM51300736");
            var succeedingIsrc = new Isrc("FR6V81100021");

            var secondItemProcessed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            IGetOrEnrichTrackUseCase trackUseCase = Substitute.For<IGetOrEnrichTrackUseCase>();
            trackUseCase.ExecuteAsync(
                Arg.Is<GetOrEnrichTrackRequest>(r => r != null && r.Isrc == failingIsrc),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromException<Track?>(new InvalidOperationException("Panne simulée.")));
            trackUseCase.ExecuteAsync(
                Arg.Is<GetOrEnrichTrackRequest>(r => r != null && r.Isrc == succeedingIsrc),
                Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Track?>(null))
                .AndDoes(_ => secondItemProcessed.TrySetResult());

            IGetOrEnrichAlbumUseCase albumUseCase = Substitute.For<IGetOrEnrichAlbumUseCase>();
            IGetOrEnrichArtistUseCase artistUseCase = Substitute.For<IGetOrEnrichArtistUseCase>();

            var reader = new FakeEnrichmentJobReader([
                new EnrichmentWorkItem.ForTrack(failingIsrc),
                new EnrichmentWorkItem.ForTrack(succeedingIsrc),
            ]);

            EnrichmentBackgroundService service = CreateService(reader, trackUseCase, albumUseCase, artistUseCase);

            // Act
            await service.StartAsync(CancellationToken.None);
            await secondItemProcessed.Task.WaitAsync(_timeout);
            await service.StopAsync(CancellationToken.None);

            // Assert : les deux éléments ont bien été tentés, malgré l'échec du premier — la boucle
            // n'a pas été interrompue.
            await trackUseCase.Received(1).ExecuteAsync(
                Arg.Is<GetOrEnrichTrackRequest>(r => r != null && r.Isrc == failingIsrc),
                Arg.Any<CancellationToken>());
            await trackUseCase.Received(1).ExecuteAsync(
                Arg.Is<GetOrEnrichTrackRequest>(r => r != null && r.Isrc == succeedingIsrc),
                Arg.Any<CancellationToken>());
        }

        private static EnrichmentBackgroundService CreateService(
            IEnrichmentJobReader reader,
            IGetOrEnrichTrackUseCase trackUseCase,
            IGetOrEnrichAlbumUseCase albumUseCase,
            IGetOrEnrichArtistUseCase artistUseCase)
        {
            var services = new ServiceCollection();
            services.AddScoped(_ => trackUseCase);
            services.AddScoped(_ => albumUseCase);
            services.AddScoped(_ => artistUseCase);
            ServiceProvider provider = services.BuildServiceProvider();

            return new EnrichmentBackgroundService(
                reader,
                provider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger<EnrichmentBackgroundService>.Instance);
        }

        /// <summary>
        /// Reader de test qui rejoue une séquence fixe d'éléments puis se termine naturellement
        /// (contrairement à EnrichmentJobChannel, qui ne se termine jamais en usage réel) : suffisant
        /// pour tester le routage de EnrichmentBackgroundService sans dépendre du Channel réel.
        /// </summary>
        private sealed class FakeEnrichmentJobReader(IEnumerable<EnrichmentWorkItem> items) : IEnrichmentJobReader
        {
            public async IAsyncEnumerable<EnrichmentWorkItem> ReadAllAsync(
                [EnumeratorCancellation] CancellationToken ct = default)
            {
                foreach (EnrichmentWorkItem item in items)
                {
                    ct.ThrowIfCancellationRequested();
                    yield return item;
                    await Task.Yield();
                }
            }
        }
    }
}
