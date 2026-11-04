using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WedLink.DataAccess.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GuestInteractions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MusicTrackId",
                table: "Invitations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GuestMessages",
                columns: table => new
                {
                    GuestMessageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvitationId = table.Column<int>(type: "int", nullable: false),
                    GuestName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedByAdminUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestMessages", x => x.GuestMessageId);
                    table.ForeignKey(
                        name: "FK_GuestMessages_AspNetUsers_DeletedByAdminUserId",
                        column: x => x.DeletedByAdminUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GuestMessages_Invitations_InvitationId",
                        column: x => x.InvitationId,
                        principalTable: "Invitations",
                        principalColumn: "InvitationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MusicTracks",
                columns: table => new
                {
                    MusicTrackId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Artist = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    RightsReference = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicTracks", x => x.MusicTrackId);
                });

            migrationBuilder.CreateTable(
                name: "RsvpResponses",
                columns: table => new
                {
                    RsvpResponseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvitationId = table.Column<int>(type: "int", nullable: false),
                    Response = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    GuestName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AccompanyingPeople = table.Column<int>(type: "int", nullable: false),
                    EditTokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RsvpResponses", x => x.RsvpResponseId);
                    table.CheckConstraint("CK_RsvpResponses_AccompanyingPeople", "[AccompanyingPeople] >= 0");
                    table.ForeignKey(
                        name: "FK_RsvpResponses_Invitations_InvitationId",
                        column: x => x.InvitationId,
                        principalTable: "Invitations",
                        principalColumn: "InvitationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_MusicTrackId",
                table: "Invitations",
                column: "MusicTrackId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestMessages_DeletedByAdminUserId",
                table: "GuestMessages",
                column: "DeletedByAdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestMessages_InvitationId_SubmittedAt",
                table: "GuestMessages",
                columns: new[] { "InvitationId", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MusicTracks_IsActive_Title",
                table: "MusicTracks",
                columns: new[] { "IsActive", "Title" });

            migrationBuilder.CreateIndex(
                name: "IX_RsvpResponses_InvitationId_EditTokenHash",
                table: "RsvpResponses",
                columns: new[] { "InvitationId", "EditTokenHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RsvpResponses_InvitationId_SubmittedAt",
                table: "RsvpResponses",
                columns: new[] { "InvitationId", "SubmittedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_MusicTracks_MusicTrackId",
                table: "Invitations",
                column: "MusicTrackId",
                principalTable: "MusicTracks",
                principalColumn: "MusicTrackId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_MusicTracks_MusicTrackId",
                table: "Invitations");

            migrationBuilder.DropTable(
                name: "GuestMessages");

            migrationBuilder.DropTable(
                name: "MusicTracks");

            migrationBuilder.DropTable(
                name: "RsvpResponses");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_MusicTrackId",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "MusicTrackId",
                table: "Invitations");
        }
    }
}
