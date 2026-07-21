using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeezerStats.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsrcFromListeningEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_listening_events_UserId_Isrc_ListenedAt",
                table: "listening_events");

            migrationBuilder.DropColumn(
                name: "Isrc",
                table: "listening_events");

            migrationBuilder.CreateIndex(
                name: "IX_listening_events_TrackId",
                table: "listening_events",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_listening_events_UserId_TrackId_ListenedAt",
                table: "listening_events",
                columns: new[] { "UserId", "TrackId", "ListenedAt" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_listening_events_tracks_TrackId",
                table: "listening_events",
                column: "TrackId",
                principalTable: "tracks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_listening_events_tracks_TrackId",
                table: "listening_events");

            migrationBuilder.DropIndex(
                name: "IX_listening_events_TrackId",
                table: "listening_events");

            migrationBuilder.DropIndex(
                name: "IX_listening_events_UserId_TrackId_ListenedAt",
                table: "listening_events");

            migrationBuilder.AddColumn<string>(
                name: "Isrc",
                table: "listening_events",
                type: "character varying(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_listening_events_UserId_Isrc_ListenedAt",
                table: "listening_events",
                columns: new[] { "UserId", "Isrc", "ListenedAt" },
                unique: true);
        }
    }
}
