using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityHub.Application.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonStatusField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Person_genders_GenderId",
                table: "Person");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Person",
                table: "Person");

            migrationBuilder.RenameTable(
                name: "Person",
                newName: "persons");

            migrationBuilder.RenameIndex(
                name: "IX_Person_IdentificationNumber",
                table: "persons",
                newName: "IX_persons_IdentificationNumber");

            migrationBuilder.RenameIndex(
                name: "IX_Person_GenderId",
                table: "persons",
                newName: "IX_persons_GenderId");

            migrationBuilder.RenameIndex(
                name: "IX_Person_Email",
                table: "persons",
                newName: "IX_persons_Email");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "persons",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddPrimaryKey(
                name: "PK_persons",
                table: "persons",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_persons_genders_GenderId",
                table: "persons",
                column: "GenderId",
                principalTable: "genders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_persons_genders_GenderId",
                table: "persons");

            migrationBuilder.DropPrimaryKey(
                name: "PK_persons",
                table: "persons");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "persons");

            migrationBuilder.RenameTable(
                name: "persons",
                newName: "Person");

            migrationBuilder.RenameIndex(
                name: "IX_persons_IdentificationNumber",
                table: "Person",
                newName: "IX_Person_IdentificationNumber");

            migrationBuilder.RenameIndex(
                name: "IX_persons_GenderId",
                table: "Person",
                newName: "IX_Person_GenderId");

            migrationBuilder.RenameIndex(
                name: "IX_persons_Email",
                table: "Person",
                newName: "IX_Person_Email");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Person",
                table: "Person",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Person_genders_GenderId",
                table: "Person",
                column: "GenderId",
                principalTable: "genders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
