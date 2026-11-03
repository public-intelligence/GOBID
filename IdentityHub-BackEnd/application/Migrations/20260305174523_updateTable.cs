using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityHub.Application.Migrations
{
    /// <inheritdoc />
    public partial class updateTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_persons_genders_GenderId",
                table: "persons");

            migrationBuilder.RenameColumn(
                name: "PhoneNumberConfirmed",
                table: "persons",
                newName: "Phone_Number_Confirmed");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "persons",
                newName: "Phone_Number");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "persons",
                newName: "Last_Name");

            migrationBuilder.RenameColumn(
                name: "IsExternal",
                table: "persons",
                newName: "Is_External");

            migrationBuilder.RenameColumn(
                name: "IdentificationNumber",
                table: "persons",
                newName: "Identification_Number");

            migrationBuilder.RenameColumn(
                name: "GenderId",
                table: "persons",
                newName: "Gender_Id");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "persons",
                newName: "First_Name");

            migrationBuilder.RenameColumn(
                name: "EmailConfirmed",
                table: "persons",
                newName: "Email_Confirmed");

            migrationBuilder.RenameIndex(
                name: "IX_persons_IdentificationNumber",
                table: "persons",
                newName: "IX_persons_Identification_Number");

            migrationBuilder.RenameIndex(
                name: "IX_persons_GenderId",
                table: "persons",
                newName: "IX_persons_Gender_Id");

            migrationBuilder.RenameColumn(
                name: "SocialName",
                table: "organizations",
                newName: "Social_Name");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "organizations",
                newName: "Phone_Number");

            migrationBuilder.RenameColumn(
                name: "LegalName",
                table: "organizations",
                newName: "Legal_Name");

            migrationBuilder.RenameIndex(
                name: "IX_organizations_SocialName",
                table: "organizations",
                newName: "IX_organizations_Social_Name");

            migrationBuilder.RenameIndex(
                name: "IX_organizations_LegalName",
                table: "organizations",
                newName: "IX_organizations_Legal_Name");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "genders",
                newName: "Is_Active");

            migrationBuilder.AddForeignKey(
                name: "FK_persons_genders_Gender_Id",
                table: "persons",
                column: "Gender_Id",
                principalTable: "genders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_persons_genders_Gender_Id",
                table: "persons");

            migrationBuilder.RenameColumn(
                name: "Phone_Number_Confirmed",
                table: "persons",
                newName: "PhoneNumberConfirmed");

            migrationBuilder.RenameColumn(
                name: "Phone_Number",
                table: "persons",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "Last_Name",
                table: "persons",
                newName: "LastName");

            migrationBuilder.RenameColumn(
                name: "Is_External",
                table: "persons",
                newName: "IsExternal");

            migrationBuilder.RenameColumn(
                name: "Identification_Number",
                table: "persons",
                newName: "IdentificationNumber");

            migrationBuilder.RenameColumn(
                name: "Gender_Id",
                table: "persons",
                newName: "GenderId");

            migrationBuilder.RenameColumn(
                name: "First_Name",
                table: "persons",
                newName: "FirstName");

            migrationBuilder.RenameColumn(
                name: "Email_Confirmed",
                table: "persons",
                newName: "EmailConfirmed");

            migrationBuilder.RenameIndex(
                name: "IX_persons_Identification_Number",
                table: "persons",
                newName: "IX_persons_IdentificationNumber");

            migrationBuilder.RenameIndex(
                name: "IX_persons_Gender_Id",
                table: "persons",
                newName: "IX_persons_GenderId");

            migrationBuilder.RenameColumn(
                name: "Social_Name",
                table: "organizations",
                newName: "SocialName");

            migrationBuilder.RenameColumn(
                name: "Phone_Number",
                table: "organizations",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "Legal_Name",
                table: "organizations",
                newName: "LegalName");

            migrationBuilder.RenameIndex(
                name: "IX_organizations_Social_Name",
                table: "organizations",
                newName: "IX_organizations_SocialName");

            migrationBuilder.RenameIndex(
                name: "IX_organizations_Legal_Name",
                table: "organizations",
                newName: "IX_organizations_LegalName");

            migrationBuilder.RenameColumn(
                name: "Is_Active",
                table: "genders",
                newName: "IsActive");

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
