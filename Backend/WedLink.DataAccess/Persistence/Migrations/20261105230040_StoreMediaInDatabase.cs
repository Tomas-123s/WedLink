using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WedLink.DataAccess.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StoreMediaInDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StoredMediaFiles",
                columns: table => new
                {
                    StorageKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredMediaFiles", x => x.StorageKey);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoredMediaFiles");
        }
    }
}
