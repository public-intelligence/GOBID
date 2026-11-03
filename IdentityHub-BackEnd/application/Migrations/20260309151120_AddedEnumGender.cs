using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityHub.Application.Migrations
{
    /// <inheritdoc />
    public partial class AddedEnumGender : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_persons_genders_GenderId",
                table: "persons");

            migrationBuilder.DropTable(
                name: "genders");

            migrationBuilder.DropIndex(
                name: "IX_persons_Email",
                table: "persons");

            migrationBuilder.DropIndex(
                name: "IX_persons_GenderId",
                table: "persons");

            migrationBuilder.DropColumn(
                name: "GenderId",
                table: "persons");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "persons",
                type: "nchar(17)",
                fixedLength: true,
                maxLength: 17,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nchar(17)",
                oldFixedLength: true,
                oldMaxLength: 17);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "persons",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "persons",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_persons_Email",
                table: "persons",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_persons_Email",
                table: "persons");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "persons");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "persons",
                type: "nchar(17)",
                fixedLength: true,
                maxLength: 17,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nchar(17)",
                oldFixedLength: true,
                oldMaxLength: 17,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "persons",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GenderId",
                table: "persons",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "genders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_genders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_persons_Email",
                table: "persons",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_persons_GenderId",
                table: "persons",
                column: "GenderId");

            migrationBuilder.AddForeignKey(
                name: "FK_persons_genders_GenderId",
                table: "persons",
                column: "GenderId",
                principalTable: "genders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
