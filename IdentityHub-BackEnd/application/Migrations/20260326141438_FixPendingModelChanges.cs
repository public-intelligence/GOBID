using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityHub.Application.Migrations
{
    /// <inheritdoc />
    public partial class FixPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_persons_FirstName_LastName",
                table: "persons",
                columns: new[] { "FirstName", "LastName" });

            migrationBuilder.CreateIndex(
                name: "IX_persons_Status",
                table: "persons",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_persons_FirstName_LastName",
                table: "persons");

            migrationBuilder.DropIndex(
                name: "IX_persons_Status",
                table: "persons");
        }
    }
}
