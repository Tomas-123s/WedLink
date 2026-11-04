using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WedLink.DataAccess.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InvitationCoupleNamesCapacity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CoupleNames",
                table: "Invitations",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(301)",
                oldMaxLength: 301);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CoupleNames",
                table: "Invitations",
                type: "nvarchar(301)",
                maxLength: 301,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);
        }
    }
}
