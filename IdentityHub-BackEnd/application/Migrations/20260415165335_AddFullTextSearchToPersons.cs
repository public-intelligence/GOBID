using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityHub.Application.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearchToPersons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Crear catálogo Full-Text si no existe
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'ft_persons_catalog')
                BEGIN
                    CREATE FULLTEXT CATALOG ft_persons_catalog AS DEFAULT;
                END
            ", suppressTransaction: true);

            // Crear índice Full-Text en la tabla persons
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('persons'))
                BEGIN
                    CREATE FULLTEXT INDEX ON persons(FirstName, LastName, IdentificationNumber)
                    KEY INDEX PK_persons
                    ON ft_persons_catalog
                    WITH CHANGE_TRACKING AUTO;
                END
            ", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Eliminar índice Full-Text
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('persons'))
                BEGIN
                    DROP FULLTEXT INDEX ON persons;
                END
            ", suppressTransaction: true);

            // Eliminar catálogo Full-Text
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'ft_persons_catalog')
                BEGIN
                    DROP FULLTEXT CATALOG ft_persons_catalog;
                END
            ", suppressTransaction: true);
        }
    }
}
