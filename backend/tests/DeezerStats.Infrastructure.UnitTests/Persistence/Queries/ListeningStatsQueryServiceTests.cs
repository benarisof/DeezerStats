using DeezerStats.Application.DTOs.Stats;
using DeezerStats.Domain.Aggregates.AlbumAggregate;
using DeezerStats.Domain.Aggregates.ArtistAggregate;
using DeezerStats.Domain.Aggregates.ListeningEventAggregate;
using DeezerStats.Domain.Aggregates.TrackAggregate;
using DeezerStats.Domain.ValueObjects;
using DeezerStats.Infrastructure.Persistence;
using DeezerStats.Infrastructure.Persistence.Queries;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DeezerStats.Infrastructure.UnitTests.Persistence.Queries
{
    public class ListeningStatsQueryServiceTests
    {
        private static readonly DateTime _baseline = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task GetTopAlbumsAsyncShouldRankAlbumsByPlayCountDescending()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var service = new ListeningStatsQueryService(context);
            var userId = Guid.NewGuid();

            var artist = new Artist(Guid.NewGuid(), "Daft Punk");
            var albumA = new Album(Guid.NewGuid(), "Discovery", artist.Id);
            var albumB = new Album(Guid.NewGuid(), "Homework", artist.Id);
            Track trackA = NewTrack("USUM70000001", "One More Time", artist.Id, albumA.Id);
            Track trackB = NewTrack("USUM70000002", "Da Funk", artist.Id, albumB.Id);
            context.AddRange(artist, albumA, albumB, trackA, trackB);

            AddListeningEvents(context, userId, trackA.Id, count: 3);
            AddListeningEvents(context, userId, trackB.Id, count: 1);
            await context.SaveChangesAsync();

            // Act
            PagedResult<AlbumSummary> result = await service.GetTopAlbumsAsync(userId, new DateRange(null, null), page: 1, pageSize: 20);

            // Assert
            result.TotalItems.Should().Be(2);
            result.Items.Select(a => a.Id).Should().ContainInOrder(albumA.Id, albumB.Id);
            result.Items.First(a => a.Id == albumA.Id).PlayCount.Should().Be(3);
            result.Items.First(a => a.Id == albumA.Id).ArtistName.Should().Be("Daft Punk");
        }

        [Fact]
        public async Task GetTopArtistsAsyncShouldSumPlayCountsAcrossAllAlbumsOfTheArtist()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var service = new ListeningStatsQueryService(context);
            var userId = Guid.NewGuid();

            var daftPunk = new Artist(Guid.NewGuid(), "Daft Punk");
            var weeknd = new Artist(Guid.NewGuid(), "The Weeknd");
            var albumA = new Album(Guid.NewGuid(), "Discovery", daftPunk.Id);
            var albumB = new Album(Guid.NewGuid(), "Homework", daftPunk.Id);
            var albumC = new Album(Guid.NewGuid(), "After Hours", weeknd.Id);
            Track trackA = NewTrack("USUM70000003", "One More Time", daftPunk.Id, albumA.Id);
            Track trackB = NewTrack("USUM70000004", "Da Funk", daftPunk.Id, albumB.Id);
            Track trackC = NewTrack("USUM70000005", "Blinding Lights", weeknd.Id, albumC.Id);
            context.AddRange(daftPunk, weeknd, albumA, albumB, albumC, trackA, trackB, trackC);

            // Daft Punk : 2 + 1 = 3 écoutes cumulées sur deux albums différents.
            AddListeningEvents(context, userId, trackA.Id, count: 2);
            AddListeningEvents(context, userId, trackB.Id, count: 1);
            AddListeningEvents(context, userId, trackC.Id, count: 1);
            await context.SaveChangesAsync();

            // Act
            PagedResult<ArtistSummary> result = await service.GetTopArtistsAsync(userId, new DateRange(null, null), page: 1, pageSize: 20);

            // Assert
            result.Items.Select(a => a.Id).Should().ContainInOrder(daftPunk.Id, weeknd.Id);
            result.Items.First(a => a.Id == daftPunk.Id).PlayCount.Should().Be(3);
        }

        [Fact]
        public async Task GetTopTracksAsyncShouldExcludeEventsOutsideTheRequestedDateRange()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var service = new ListeningStatsQueryService(context);
            var userId = Guid.NewGuid();

            (Artist _, Album _, Track? track) = SeedSingleTrack(context, "USUM70000006");

            context.ListeningEvents.Add(new ListeningEvent(Guid.NewGuid(), userId, track.Id, new Duration(180), new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc)));
            context.ListeningEvents.Add(new ListeningEvent(Guid.NewGuid(), userId, track.Id, new Duration(180), new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)));
            await context.SaveChangesAsync();

            var dateRange = new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

            // Act
            PagedResult<TrackSummary> result = await service.GetTopTracksAsync(userId, dateRange, page: 1, pageSize: 20);

            // Assert : seule l'écoute de janvier doit être comptée.
            result.Items.Should().ContainSingle(t => t.Id == track.Id && t.PlayCount == 1);
        }

        [Fact]
        public async Task GetTopTracksAsyncShouldNeverLeakAnotherUsersListeningEvents()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var service = new ListeningStatsQueryService(context);
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();

            (Artist _, Album _, Track? track) = SeedSingleTrack(context, "USUM70000007");

            AddListeningEvents(context, userId, track.Id, count: 1);
            AddListeningEvents(context, otherUserId, track.Id, count: 10);
            await context.SaveChangesAsync();

            // Act
            PagedResult<TrackSummary> result = await service.GetTopTracksAsync(userId, new DateRange(null, null), page: 1, pageSize: 20);

            // Assert
            result.Items.Should().ContainSingle(t => t.Id == track.Id && t.PlayCount == 1);
        }

        [Fact]
        public async Task GetTopAlbumsAsyncShouldApplyPaginationMath()
        {
            // Arrange : 5 albums, un morceau chacun, un nombre d'écoutes décroissant garantissant un
            // ordre déterministe (5, 4, 3, 2, 1).
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var service = new ListeningStatsQueryService(context);
            var userId = Guid.NewGuid();
            var artist = new Artist(Guid.NewGuid(), "Artist");
            context.Add(artist);

            List<Guid> albumIds = [];
            for (var i = 0; i < 5; i++)
            {
                var album = new Album(Guid.NewGuid(), $"Album {i}", artist.Id);
                Track track = NewTrack($"USUM7{4000000 + i}", $"Track {i}", artist.Id, album.Id);
                context.AddRange(album, track);
                AddListeningEvents(context, userId, track.Id, count: 5 - i);
                albumIds.Add(album.Id);
            }

            await context.SaveChangesAsync();

            // Act : page 2 avec pageSize 2 -> doit renvoyer les 3e et 4e éléments du classement.
            PagedResult<AlbumSummary> result = await service.GetTopAlbumsAsync(userId, new DateRange(null, null), page: 2, pageSize: 2);

            // Assert
            result.TotalItems.Should().Be(5);
            result.TotalPages.Should().Be(3);
            result.Items.Should().HaveCount(2);
            result.Items.Select(a => a.Id).Should().ContainInOrder(albumIds[2], albumIds[3]);
        }

        [Fact]
        public async Task GetTopAlbumsAsyncShouldCapTotalItemsAtBusinessRuleLimitEvenWithMoreMatches()
        {
            // Arrange : 101 albums distincts avec chacun exactement une écoute -> le classement
            // complet doit être plafonné à 100 (voir StatsRules.MaxRankedResults), peu importe le
            // volume réel de données ni la taille de page demandée.
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var service = new ListeningStatsQueryService(context);
            var userId = Guid.NewGuid();
            var artist = new Artist(Guid.NewGuid(), "Prolific Artist");
            context.Add(artist);

            for (var i = 0; i < 101; i++)
            {
                var album = new Album(Guid.NewGuid(), $"Album {i:D3}", artist.Id);
                Track track = NewTrack($"USUM7{2000000 + i}", $"Track {i:D3}", artist.Id, album.Id);
                context.AddRange(album, track);
                AddListeningEvents(context, userId, track.Id, count: 1);
            }

            await context.SaveChangesAsync();

            // Act
            PagedResult<AlbumSummary> result = await service.GetTopAlbumsAsync(userId, new DateRange(null, null), page: 1, pageSize: 200);

            // Assert
            result.TotalItems.Should().Be(100);
            result.Items.Should().HaveCount(100);
        }

        [Fact]
        public async Task GetHomeStatsAsyncShouldReturnAtMostTenItemsPerCategory()
        {
            // Arrange : 12 albums (donc 12 artistes/morceaux distincts) -> home stats doit se
            // limiter à 10 par catégorie.
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var service = new ListeningStatsQueryService(context);
            var userId = Guid.NewGuid();

            for (var i = 0; i < 12; i++)
            {
                var artist = new Artist(Guid.NewGuid(), $"Artist {i:D2}");
                var album = new Album(Guid.NewGuid(), $"Album {i:D2}", artist.Id);
                Track track = NewTrack($"USUM7{3000000 + i}", $"Track {i:D2}", artist.Id, album.Id);
                context.AddRange(artist, album, track);
                AddListeningEvents(context, userId, track.Id, count: 1);
            }

            await context.SaveChangesAsync();

            // Act
            HomeStatsResponse result = await service.GetHomeStatsAsync(userId, new DateRange(null, null));

            // Assert
            result.TopAlbums.Should().HaveCount(10);
            result.TopArtists.Should().HaveCount(10);
            result.TopTracks.Should().HaveCount(10);
        }

        [Fact]
        public async Task GetHistoryAsyncShouldOrderByListenedAtDescendingAndPaginate()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var service = new ListeningStatsQueryService(context);
            var userId = Guid.NewGuid();
            (Artist _, Album _, Track? track) = SeedSingleTrack(context, "USUM70000008");

            List<Guid> eventIdsOldestFirst = [];
            for (var i = 0; i < 3; i++)
            {
                var listeningEvent = new ListeningEvent(Guid.NewGuid(), userId, track.Id, new Duration(180), _baseline.AddDays(i));
                context.ListeningEvents.Add(listeningEvent);
                eventIdsOldestFirst.Add(listeningEvent.Id);
            }

            await context.SaveChangesAsync();

            // Act
            PagedResult<HistoryEntry> result = await service.GetHistoryAsync(userId, new DateRange(null, null), page: 1, pageSize: 20);

            // Assert : le plus récent (jour +2) doit apparaître en premier.
            result.Items.Select(h => h.Id).Should().ContainInOrder(
                eventIdsOldestFirst[2], eventIdsOldestFirst[1], eventIdsOldestFirst[0]);
        }

        [Fact]
        public async Task GetAlbumDetailAsyncWhenAlbumDoesNotExistShouldReturnNull()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var service = new ListeningStatsQueryService(context);

            // Act
            AlbumDetail? result = await service.GetAlbumDetailAsync(Guid.NewGuid(), Guid.NewGuid(), new DateRange(null, null));

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAlbumDetailAsyncShouldAggregatePlayCountsAndIncludeUnplayedTracksAtZero()
        {
            // Arrange : un album avec deux morceaux, un seul écouté par l'utilisateur courant.
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var service = new ListeningStatsQueryService(context);
            var userId = Guid.NewGuid();

            var artist = new Artist(Guid.NewGuid(), "Daft Punk");
            var album = new Album(Guid.NewGuid(), "Discovery", artist.Id);
            Track playedTrack = NewTrack("USUM70000009", "One More Time", artist.Id, album.Id);
            Track unplayedTrack = NewTrack("USUM70000010", "Aerodynamic", artist.Id, album.Id);
            context.AddRange(artist, album, playedTrack, unplayedTrack);

            context.ListeningEvents.Add(new ListeningEvent(Guid.NewGuid(), userId, playedTrack.Id, new Duration(200), _baseline));
            context.ListeningEvents.Add(new ListeningEvent(Guid.NewGuid(), userId, playedTrack.Id, new Duration(200), _baseline.AddMinutes(1)));
            await context.SaveChangesAsync();

            // Act
            AlbumDetail? result = await service.GetAlbumDetailAsync(userId, album.Id, new DateRange(null, null));

            // Assert
            result.Should().NotBeNull();
            result!.ArtistName.Should().Be("Daft Punk");
            result.TotalPlayCount.Should().Be(2);
            result.TotalListeningDurationHours.Should().BeApproximately(400.0 / 3600.0, precision: 0.0001);
            result.Tracks.Should().HaveCount(2);
            result.Tracks[0].Id.Should().Be(playedTrack.Id, "le morceau écouté doit être trié en premier");
            result.Tracks.Should().ContainSingle(t => t.Id == unplayedTrack.Id && t.PlayCount == 0);
        }

        [Fact]
        public async Task GetArtistDetailAsyncWhenArtistDoesNotExistShouldReturnNull()
        {
            // Arrange
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var service = new ListeningStatsQueryService(context);

            // Act
            ArtistDetail? result = await service.GetArtistDetailAsync(Guid.NewGuid(), Guid.NewGuid(), new DateRange(null, null));

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetArtistDetailAsyncShouldCountOnlyDistinctPlayedAlbumsAndTracks()
        {
            // Arrange : l'artiste a 2 albums / 3 morceaux au catalogue, mais l'utilisateur n'en a
            // réellement écouté que 2 (répartis sur 2 albums différents) -> distinctAlbumsCount et
            // distinctTracksCount ne doivent refléter que ce qui a été écouté (voir le commentaire de
            // GetArtistDetailAsync).
            using ApplicationDbContext context = CreateInMemoryDbContext();
            var service = new ListeningStatsQueryService(context);
            var userId = Guid.NewGuid();

            var artist = new Artist(Guid.NewGuid(), "Daft Punk");
            var albumA = new Album(Guid.NewGuid(), "Discovery", artist.Id);
            var albumB = new Album(Guid.NewGuid(), "Homework", artist.Id);
            Track trackA1 = NewTrack("USUM70000011", "One More Time", artist.Id, albumA.Id);
            Track trackA2 = NewTrack("USUM70000012", "Aerodynamic", artist.Id, albumA.Id);
            Track trackB1 = NewTrack("USUM70000013", "Da Funk", artist.Id, albumB.Id);
            context.AddRange(artist, albumA, albumB, trackA1, trackA2, trackB1);

            AddListeningEvents(context, userId, trackA1.Id, count: 2);
            AddListeningEvents(context, userId, trackB1.Id, count: 1);

            await context.SaveChangesAsync();

            // Act
            ArtistDetail? result = await service.GetArtistDetailAsync(userId, artist.Id, new DateRange(null, null));

            // Assert
            result.Should().NotBeNull();
            result!.DistinctTracksCount.Should().Be(2);
            result.DistinctAlbumsCount.Should().Be(2);
            result.TotalPlayCount.Should().Be(3);
            result.Tracks.Should().HaveCount(3);
            result.Tracks.Should().ContainSingle(t => t.Id == trackA2.Id && t.PlayCount == 0);
        }

        private static (Artist Artist, Album Album, Track Track) SeedSingleTrack(ApplicationDbContext context, string isrc)
        {
            var artist = new Artist(Guid.NewGuid(), "Solo Artist");
            var album = new Album(Guid.NewGuid(), "Solo Album", artist.Id);
            Track track = NewTrack(isrc, "Solo Track", artist.Id, album.Id);
            context.AddRange(artist, album, track);
            return (artist, album, track);
        }

        private static Track NewTrack(string isrc, string title, Guid artistId, Guid albumId) =>
            new(Guid.NewGuid(), new Isrc(isrc), title, artistId, albumId);

        private static void AddListeningEvents(ApplicationDbContext context, Guid userId, Guid trackId, int count)
        {
            for (var i = 0; i < count; i++)
            {
                context.ListeningEvents.Add(new ListeningEvent(
                    Guid.NewGuid(), userId, trackId, new Duration(180), _baseline.AddMinutes(i)));
            }
        }

        private static ApplicationDbContext CreateInMemoryDbContext()
        {
            DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }
    }
}
