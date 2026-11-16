-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[read_random_string]
    @application_id uniqueidentifier
AS
BEGIN
  SET NOCOUNT ON;
  SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

  BEGIN TRY
      SELECT (
        SELECT 
            random_string
        FROM dbo.applications
        WHERE application_id = @application_id
          AND deleted_at IS NULL
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
      ) AS data;
  END TRY
  BEGIN CATCH
     DECLARE @err NVARCHAR(2048) = (
        SELECT ERROR_NUMBER()   AS number,
               ERROR_MESSAGE()  AS message
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
     );
     THROW 51000, @err, 1;
  END CATCH
END;

GO

