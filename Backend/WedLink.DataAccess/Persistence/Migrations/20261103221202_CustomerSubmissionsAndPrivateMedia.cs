using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WedLink.DataAccess.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomerSubmissionsAndPrivateMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerSubmissionMedia",
                columns: table => new
                {
                    CustomerSubmissionMediaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MediaKind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByAdminUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerSubmissionMedia", x => x.CustomerSubmissionMediaId);
                    table.CheckConstraint("CK_CustomerSubmissionMedia_Size_NonNegative", "[SizeBytes] >= 0");
                    table.ForeignKey(
                        name: "FK_CustomerSubmissionMedia_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerWeddingSubmissions",
                columns: table => new
                {
                    CustomerWeddingSubmissionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    BrideName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    GroomName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    WeddingDateTimeLocal = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VenueName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    VenueAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LocationUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CustomizationsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 20000, nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AdminReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AdminReviewedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerWeddingSubmissions", x => x.CustomerWeddingSubmissionId);
                    table.ForeignKey(
                        name: "FK_CustomerWeddingSubmissions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrivateLinkTokens",
                columns: table => new
                {
                    PrivateLinkTokenId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivateLinkTokens", x => x.PrivateLinkTokenId);
                    table.ForeignKey(
                        name: "FK_PrivateLinkTokens_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSubmissionMedia_OrderId_DeletedAt",
                table: "CustomerSubmissionMedia",
                columns: new[] { "OrderId", "DeletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerWeddingSubmissions_OrderId",
                table: "CustomerWeddingSubmissions",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrivateLinkTokens_OrderId_Purpose_ExpiresAt",
                table: "PrivateLinkTokens",
                columns: new[] { "OrderId", "Purpose", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PrivateLinkTokens_TokenHash",
                table: "PrivateLinkTokens",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerSubmissionMedia");

            migrationBuilder.DropTable(
                name: "CustomerWeddingSubmissions");

            migrationBuilder.DropTable(
                name: "PrivateLinkTokens");
        }
    }
}
