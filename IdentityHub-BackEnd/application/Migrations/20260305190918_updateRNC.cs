using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityHub.Application.Migrations
{
    /// <inheritdoc />
    public partial class updateRNC : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RNC",
                table: "organizations",
                type: "nchar(13)",
                fixedLength: true,
                maxLength: 13,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldFixedLength: true,
                oldMaxLength: 13);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "RNC",
                table: "organizations",
                type: "int",
                fixedLength: true,
                maxLength: 13,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nchar(13)",
                oldFixedLength: true,
                oldMaxLength: 13);
        }
    }
}
