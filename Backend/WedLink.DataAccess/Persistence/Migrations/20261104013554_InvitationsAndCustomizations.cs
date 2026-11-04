using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WedLink.DataAccess.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InvitationsAndCustomizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Invitations",
                columns: table => new
                {
                    InvitationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    TemplateVersionId = table.Column<int>(type: "int", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    WeddingDateTimeLocal = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CoupleNames = table.Column<string>(type: "nvarchar(301)", maxLength: 301, nullable: false),
                    VenueName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    VenueAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LocationUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MediaDeleteAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PausedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FinalPngStorageKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitations", x => x.InvitationId);
                    table.ForeignKey(
                        name: "FK_Invitations_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invitations_TemplateVersions_TemplateVersionId",
                        column: x => x.TemplateVersionId,
                        principalTable: "TemplateVersions",
                        principalColumn: "TemplateVersionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvitationCustomizations",
                columns: table => new
                {
                    InvitationCustomizationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvitationId = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ValueJson = table.Column<string>(type: "nvarchar(max)", maxLength: 20000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvitationCustomizations", x => x.InvitationCustomizationId);
                    table.ForeignKey(
                        name: "FK_InvitationCustomizations_Invitations_InvitationId",
                        column: x => x.InvitationId,
                        principalTable: "Invitations",
                        principalColumn: "InvitationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvitationMedia",
                columns: table => new
                {
                    InvitationMediaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvitationId = table.Column<int>(type: "int", nullable: false),
                    MediaType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvitationMedia", x => x.InvitationMediaId);
                    table.CheckConstraint("CK_InvitationMedia_Size_NonNegative", "[SizeBytes] >= 0");
                    table.CheckConstraint("CK_InvitationMedia_SortOrder_NonNegative", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_InvitationMedia_Invitations_InvitationId",
                        column: x => x.InvitationId,
                        principalTable: "Invitations",
                        principalColumn: "InvitationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvitationCustomizations_InvitationId_Key",
                table: "InvitationCustomizations",
                columns: new[] { "InvitationId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvitationMedia_InvitationId_DeletedAt_SortOrder",
                table: "InvitationMedia",
                columns: new[] { "InvitationId", "DeletedAt", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_OrderId",
                table: "Invitations",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_Slug",
                table: "Invitations",
                column: "Slug",
                unique: true,
                filter: "[Slug] <> N''");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_Status_ExpiresAt",
                table: "Invitations",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_TemplateVersionId",
                table: "Invitations",
                column: "TemplateVersionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvitationCustomizations");

            migrationBuilder.DropTable(
                name: "InvitationMedia");

            migrationBuilder.DropTable(
                name: "Invitations");
        }
    }
}
