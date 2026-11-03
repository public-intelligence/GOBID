using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityHub.Application.Migrations
{
    /// <inheritdoc />
    public partial class renameOrganizationToLegalEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organizations");

            migrationBuilder.CreateTable(
                name: "legalentities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    LegalName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SocialName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdentificationNumber = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(17)", maxLength: 17, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StatusDGII = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_legalentities", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_legalentities_Email",
                table: "legalentities",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_legalentities_IdentificationNumber",
                table: "legalentities",
                column: "IdentificationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_legalentities_LegalName",
                table: "legalentities",
                column: "LegalName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_legalentities_SocialName",
                table: "legalentities",
                column: "SocialName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "legalentities");

            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(17)", maxLength: 17, nullable: false),
                    RNC = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    SocialName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_organizations_Email",
                table: "organizations",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organizations_LegalName",
                table: "organizations",
                column: "LegalName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organizations_RNC",
                table: "organizations",
                column: "RNC",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organizations_SocialName",
                table: "organizations",
                column: "SocialName",
                unique: true);
        }
    }
}
