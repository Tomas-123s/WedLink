using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WedLink.DataAccess.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PreviewReviewEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PreviewReviewEvents",
                columns: table => new
                {
                    PreviewReviewEventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvitationId = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AdminUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreviewReviewEvents", x => x.PreviewReviewEventId);
                    table.ForeignKey(
                        name: "FK_PreviewReviewEvents_AspNetUsers_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PreviewReviewEvents_Invitations_InvitationId",
                        column: x => x.InvitationId,
                        principalTable: "Invitations",
                        principalColumn: "InvitationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PreviewReviewEvents_AdminUserId",
                table: "PreviewReviewEvents",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PreviewReviewEvents_InvitationId_CreatedAt",
                table: "PreviewReviewEvents",
                columns: new[] { "InvitationId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreviewReviewEvents");
        }
    }
}
