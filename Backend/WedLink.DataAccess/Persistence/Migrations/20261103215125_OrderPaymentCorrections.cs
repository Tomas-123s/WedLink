using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WedLink.DataAccess.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrderPaymentCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderPaymentCorrections",
                columns: table => new
                {
                    OrderPaymentCorrectionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderPaymentId = table.Column<int>(type: "int", nullable: false),
                    PreviousAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    PreviousMethod = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PreviousReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PreviousNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NewAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    NewMethod = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    NewReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CorrectedByAdminUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CorrectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderPaymentCorrections", x => x.OrderPaymentCorrectionId);
                    table.CheckConstraint("CK_OrderPaymentCorrections_NewAmount_Positive", "[NewAmount] > 0");
                    table.CheckConstraint("CK_OrderPaymentCorrections_PreviousAmount_Positive", "[PreviousAmount] > 0");
                    table.ForeignKey(
                        name: "FK_OrderPaymentCorrections_AspNetUsers_CorrectedByAdminUserId",
                        column: x => x.CorrectedByAdminUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderPaymentCorrections_OrderPayments_OrderPaymentId",
                        column: x => x.OrderPaymentId,
                        principalTable: "OrderPayments",
                        principalColumn: "OrderPaymentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderPaymentCorrections_CorrectedByAdminUserId",
                table: "OrderPaymentCorrections",
                column: "CorrectedByAdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPaymentCorrections_OrderPaymentId_CorrectedAt",
                table: "OrderPaymentCorrections",
                columns: new[] { "OrderPaymentId", "CorrectedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderPaymentCorrections");
        }
    }
}
