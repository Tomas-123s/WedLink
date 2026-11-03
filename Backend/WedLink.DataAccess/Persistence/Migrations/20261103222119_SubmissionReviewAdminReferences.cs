using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WedLink.DataAccess.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SubmissionReviewAdminReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CustomerWeddingSubmissions_AdminReviewedByUserId",
                table: "CustomerWeddingSubmissions",
                column: "AdminReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSubmissionMedia_DeletedByAdminUserId",
                table: "CustomerSubmissionMedia",
                column: "DeletedByAdminUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerSubmissionMedia_AspNetUsers_DeletedByAdminUserId",
                table: "CustomerSubmissionMedia",
                column: "DeletedByAdminUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerWeddingSubmissions_AspNetUsers_AdminReviewedByUserId",
                table: "CustomerWeddingSubmissions",
                column: "AdminReviewedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerSubmissionMedia_AspNetUsers_DeletedByAdminUserId",
                table: "CustomerSubmissionMedia");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerWeddingSubmissions_AspNetUsers_AdminReviewedByUserId",
                table: "CustomerWeddingSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_CustomerWeddingSubmissions_AdminReviewedByUserId",
                table: "CustomerWeddingSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_CustomerSubmissionMedia_DeletedByAdminUserId",
                table: "CustomerSubmissionMedia");
        }
    }
}
