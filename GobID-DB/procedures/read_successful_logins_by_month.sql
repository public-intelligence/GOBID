-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[read_successful_logins_by_month]
    @organization_id INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
    
    BEGIN TRY
        DECLARE @currentYear INT = YEAR(GETDATE());
        
        ;WITH Months AS (
            SELECT 1 AS MonthNumber UNION ALL
            SELECT 2 UNION ALL
            SELECT 3 UNION ALL
            SELECT 4 UNION ALL
            SELECT 5 UNION ALL
            SELECT 6 UNION ALL
            SELECT 7 UNION ALL
            SELECT 8 UNION ALL
            SELECT 9 UNION ALL
            SELECT 10 UNION ALL
            SELECT 11 UNION ALL
            SELECT 12
        ),
        LoginStats AS (
            SELECT 
                m.MonthNumber,
                UPPER(LEFT(FORMAT(DATEFROMPARTS(@currentYear, m.MonthNumber, 1), 'MMMM', 'es-ES'), 1)) +
                LOWER(SUBSTRING(FORMAT(DATEFROMPARTS(@currentYear, m.MonthNumber, 1), 'MMMM', 'es-ES'), 2, 20)) AS MonthName,
                ISNULL(COUNT(l.login_log_id), 0) AS SuccessfulLogins
            FROM Months m
            LEFT JOIN login_logs l
                ON MONTH(l.created_at) = m.MonthNumber
               AND YEAR(l.created_at) = @currentYear
               AND l.login_successful = 1
               AND l.is_active = 1
               AND (@organization_id IS NULL OR l.organization_id = @organization_id)
            GROUP BY m.MonthNumber
        )
        SELECT (
            SELECT 
                MonthNumber AS monthNumber,
                MonthName AS monthName,
                SuccessfulLogins AS successfulLogins
            FROM LoginStats
            ORDER BY MonthNumber
            FOR JSON PATH
        ) AS json_result;
        
    END TRY
    BEGIN CATCH
        DECLARE @err NVARCHAR(2048) =
            (SELECT ERROR_NUMBER() AS error_number,
                    ERROR_MESSAGE() AS error_message
             FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
        THROW 51000, @err, 1;
    END CATCH
END;

GO

