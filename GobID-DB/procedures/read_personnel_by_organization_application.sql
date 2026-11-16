-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[read_personnel_by_organization_application]
    @organization_application_id INT,
    @page_number INT = 1,
    @page_size INT = 50
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    BEGIN TRY
        -- Validar que el parámetro obligatorio no sea NULL
        IF @organization_application_id IS NULL
        BEGIN
            DECLARE @err_param NVARCHAR(MAX) =
                (SELECT 'El parámetro organization_application_id es requerido.' AS message
                 FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
            THROW 51000, @err_param, 1;
        END

        -- Validar valores de paginación
        IF @page_number < 1 SET @page_number = 1;
        IF @page_size < 1 SET @page_size = 50;

        ;WITH filtered AS (
            SELECT 
                u.personnel_id,
                u.user_id,
                u.email,
                ua.user_application_id,
                ua.organization_application_id,
                ua.created_at
            FROM dbo.user_applications ua
            INNER JOIN dbo.users u 
                ON ua.user_id = u.user_id
            WHERE ua.organization_application_id = @organization_application_id
              AND ua.is_active = 1
              AND u.is_active = 1
              AND u.deleted_at IS NULL
        ),
        numbered AS (
            SELECT *,
                   ROW_NUMBER() OVER(ORDER BY created_at DESC) AS rn,
                   COUNT(*) OVER() AS total_tally
            FROM filtered
        )
        SELECT (
            SELECT
                ( SELECT total_tally,
                         @page_number AS current_page,
                         CEILING(total_tally * 1.0 / @page_size) AS total_pages
                  FROM numbered
                  WHERE rn = 1
                  FOR JSON PATH, WITHOUT_ARRAY_WRAPPER ) AS meta,
                ( SELECT personnel_id,
                         user_id,
                         email,
                         user_application_id,
                         organization_application_id,
                         created_at
                  FROM numbered
                  WHERE rn BETWEEN (@page_number - 1) * @page_size + 1
                               AND @page_number * @page_size
                  FOR JSON PATH ) AS data
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        ) AS json_result;

    END TRY
    BEGIN CATCH
        DECLARE @err NVARCHAR(2048) =
            (SELECT ERROR_NUMBER() AS number,
                    ERROR_MESSAGE() AS message
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        THROW 51000, @err, 1;
    END CATCH
END

GO

