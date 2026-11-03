using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WedLink.DataAccess.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TemplateCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AddOns",
                columns: table => new
                {
                    AddOnId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CurrentPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "EGP"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddOns", x => x.AddOnId);
                    table.CheckConstraint("CK_AddOns_CurrentPrice", "[CurrentPrice] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Features",
                columns: table => new
                {
                    FeatureId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Features", x => x.FeatureId);
                });

            migrationBuilder.CreateTable(
                name: "Templates",
                columns: table => new
                {
                    TemplateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PreviewImageReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Templates", x => x.TemplateId);
                });

            migrationBuilder.CreateTable(
                name: "TemplateVersions",
                columns: table => new
                {
                    TemplateVersionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PackageKey = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateVersions", x => x.TemplateVersionId);
                    table.ForeignKey(
                        name: "FK_TemplateVersions_Templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "Templates",
                        principalColumn: "TemplateId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateAddOns",
                columns: table => new
                {
                    TemplateVersionId = table.Column<int>(type: "int", nullable: false),
                    AddOnId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateAddOns", x => new { x.TemplateVersionId, x.AddOnId });
                    table.ForeignKey(
                        name: "FK_TemplateAddOns_AddOns_AddOnId",
                        column: x => x.AddOnId,
                        principalTable: "AddOns",
                        principalColumn: "AddOnId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateAddOns_TemplateVersions_TemplateVersionId",
                        column: x => x.TemplateVersionId,
                        principalTable: "TemplateVersions",
                        principalColumn: "TemplateVersionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateFeatures",
                columns: table => new
                {
                    TemplateVersionId = table.Column<int>(type: "int", nullable: false),
                    FeatureId = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MaximumCount = table.Column<int>(type: "int", nullable: true),
                    MaximumFileSizeMb = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateFeatures", x => new { x.TemplateVersionId, x.FeatureId });
                    table.CheckConstraint("CK_TemplateFeatures_MaximumCount", "[MaximumCount] IS NULL OR [MaximumCount] >= 0");
                    table.CheckConstraint("CK_TemplateFeatures_MaximumFileSizeMb", "[MaximumFileSizeMb] IS NULL OR [MaximumFileSizeMb] > 0");
                    table.ForeignKey(
                        name: "FK_TemplateFeatures_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "FeatureId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateFeatures_TemplateVersions_TemplateVersionId",
                        column: x => x.TemplateVersionId,
                        principalTable: "TemplateVersions",
                        principalColumn: "TemplateVersionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Features",
                columns: new[] { "FeatureId", "Description", "Key", "Name" },
                values: new object[,]
                {
                    { 1, null, "gallery", "معرض الصور" },
                    { 2, null, "video", "الفيديو" },
                    { 3, null, "music", "الموسيقى" },
                    { 4, null, "countdown", "العد التنازلي" },
                    { 5, null, "rsvp", "تأكيد الحضور" },
                    { 6, null, "guest-messages", "رسائل التهنئة" },
                    { 7, null, "guest-count", "عدد الحضور" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Features_Key",
                table: "Features",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAddOns_AddOnId",
                table: "TemplateAddOns",
                column: "AddOnId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateFeatures_FeatureId",
                table: "TemplateFeatures",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_Name",
                table: "Templates",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateVersions_TemplateId",
                table: "TemplateVersions",
                column: "TemplateId",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateVersions_TemplateId_VersionNumber",
                table: "TemplateVersions",
                columns: new[] { "TemplateId", "VersionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemplateAddOns");

            migrationBuilder.DropTable(
                name: "TemplateFeatures");

            migrationBuilder.DropTable(
                name: "AddOns");

            migrationBuilder.DropTable(
                name: "Features");

            migrationBuilder.DropTable(
                name: "TemplateVersions");

            migrationBuilder.DropTable(
                name: "Templates");
        }
    }
}
