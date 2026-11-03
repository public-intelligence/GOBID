using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityHub.Application.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    LegalName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SocialName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RNC = table.Column<int>(type: "int", fixedLength: true, maxLength: 13, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nchar(17)", fixedLength: true, maxLength: 17, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Active")
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organizations");
        }
    }
}
