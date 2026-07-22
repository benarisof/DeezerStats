using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeezerStats.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncTrackAndListeningEventTableNaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_listening_events_tracks_TrackId",
                table: "listening_events");

            migrationBuilder.DropForeignKey(
                name: "FK_tracks_Albums_AlbumId",
                table: "tracks");

            migrationBuilder.DropForeignKey(
                name: "FK_tracks_Artists_ArtistId",
                table: "tracks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tracks",
                table: "tracks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_listening_events",
                table: "listening_events");

            migrationBuilder.DropIndex(
                name: "IX_listening_events_UserId_TrackId_ListenedAt",
                table: "listening_events");

            migrationBuilder.RenameTable(
                name: "tracks",
                newName: "Tracks");

            migrationBuilder.RenameTable(
                name: "listening_events",
                newName: "ListeningEvents");

            migrationBuilder.RenameIndex(
                name: "IX_tracks_Isrc",
                table: "Tracks",
                newName: "IX_Tracks_Isrc");

            migrationBuilder.RenameIndex(
                name: "IX_tracks_ArtistId",
                table: "Tracks",
                newName: "IX_Tracks_ArtistId");

            migrationBuilder.RenameIndex(
                name: "IX_tracks_AlbumId",
                table: "Tracks",
                newName: "IX_Tracks_AlbumId");

            migrationBuilder.RenameIndex(
                name: "IX_listening_events_TrackId",
                table: "ListeningEvents",
                newName: "IX_ListeningEvents_TrackId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tracks",
                table: "Tracks",
                column: "Id");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ListeningEvents_UserId_TrackId_ListenedAt",
                table: "ListeningEvents",
                columns: ["UserId", "TrackId", "ListenedAt"]);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ListeningEvents",
                table: "ListeningEvents",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ListeningEvents_UserId_ListenedAt",
                table: "ListeningEvents",
                columns: ["UserId", "ListenedAt"]);

            migrationBuilder.CreateIndex(
                name: "IX_ListeningEvents_UserId_TrackId",
                table: "ListeningEvents",
                columns: ["UserId", "TrackId"]);

            migrationBuilder.AddForeignKey(
                name: "FK_ListeningEvents_Tracks_TrackId",
                table: "ListeningEvents",
                column: "TrackId",
                principalTable: "Tracks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tracks_Albums_AlbumId",
                table: "Tracks",
                column: "AlbumId",
                principalTable: "Albums",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tracks_Artists_ArtistId",
                table: "Tracks",
                column: "ArtistId",
                principalTable: "Artists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ListeningEvents_Tracks_TrackId",
                table: "ListeningEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_Tracks_Albums_AlbumId",
                table: "Tracks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tracks_Artists_ArtistId",
                table: "Tracks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tracks",
                table: "Tracks");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ListeningEvents_UserId_TrackId_ListenedAt",
                table: "ListeningEvents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ListeningEvents",
                table: "ListeningEvents");

            migrationBuilder.DropIndex(
                name: "IX_ListeningEvents_UserId_ListenedAt",
                table: "ListeningEvents");

            migrationBuilder.DropIndex(
                name: "IX_ListeningEvents_UserId_TrackId",
                table: "ListeningEvents");

            migrationBuilder.RenameTable(
                name: "Tracks",
                newName: "tracks");

            migrationBuilder.RenameTable(
                name: "ListeningEvents",
                newName: "listening_events");

            migrationBuilder.RenameIndex(
                name: "IX_Tracks_Isrc",
                table: "tracks",
                newName: "IX_tracks_Isrc");

            migrationBuilder.RenameIndex(
                name: "IX_Tracks_ArtistId",
                table: "tracks",
                newName: "IX_tracks_ArtistId");

            migrationBuilder.RenameIndex(
                name: "IX_Tracks_AlbumId",
                table: "tracks",
                newName: "IX_tracks_AlbumId");

            migrationBuilder.RenameIndex(
                name: "IX_ListeningEvents_TrackId",
                table: "listening_events",
                newName: "IX_listening_events_TrackId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tracks",
                table: "tracks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_listening_events",
                table: "listening_events",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_listening_events_UserId_TrackId_ListenedAt",
                table: "listening_events",
                columns: ["UserId", "TrackId", "ListenedAt"],
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_listening_events_tracks_TrackId",
                table: "listening_events",
                column: "TrackId",
                principalTable: "tracks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tracks_Albums_AlbumId",
                table: "tracks",
                column: "AlbumId",
                principalTable: "Albums",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tracks_Artists_ArtistId",
                table: "tracks",
                column: "ArtistId",
                principalTable: "Artists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
