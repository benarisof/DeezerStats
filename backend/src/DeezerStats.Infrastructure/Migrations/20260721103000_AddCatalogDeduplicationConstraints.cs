using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeezerStats.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogDeduplicationConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NB : defaultValue: string.Empty permet à cette migration de s'appliquer même si des
            // lignes Artists/Albums existent déjà. En dev, la base ne contient aujourd'hui aucune
            // donnée réelle : si ce n'est plus le cas au moment d'exécuter cette migration, vérifier
            // qu'il n'existe pas déjà deux artistes/albums qui entreraient en conflit sur l'index
            // unique ci-dessous (auquel cas un script de dédoublonnage doit être joué avant).
            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Artists",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedTitle",
                table: "Albums",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: string.Empty);

            // HasAlternateKey (voir ArtistConfiguration/AlbumConfiguration) se traduit par une
            // contrainte d'unicité (AddUniqueConstraint), et non par un simple CreateIndex : c'est
            // la forme d'unicité qu'EF Core sait appliquer aussi bien en PostgreSQL qu'avec le
            // provider InMemory utilisé par les tests.
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Artists_NormalizedName",
                table: "Artists",
                column: "NormalizedName");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Albums_ArtistId_NormalizedTitle",
                table: "Albums",
                columns: ["ArtistId", "NormalizedTitle"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "AK_Albums_ArtistId_NormalizedTitle",
                table: "Albums");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Artists_NormalizedName",
                table: "Artists");

            migrationBuilder.DropColumn(
                name: "NormalizedTitle",
                table: "Albums");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Artists");
        }
    }
}
