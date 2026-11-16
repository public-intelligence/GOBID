-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[read_user_organizations]
    @userId uniqueidentifier = NULL,
    @organizationId int = NULL
AS
BEGIN
  SET NOCOUNT ON;

  BEGIN TRY
    SELECT (
      SELECT organization_id AS organizationId,
             user_id AS userId,
             email,
             position
      FROM   dbo.user_organizations AS uo
      WHERE  (@userId IS NULL OR uo.user_id = @userId)
        AND (@organizationId IS NULL OR uo.organization_id = @organizationId)
      FOR JSON PATH
    ) AS json_result;
  END TRY
  BEGIN CATCH
    DECLARE @err nvarchar(2048) =
      (SELECT ERROR_NUMBER() AS number, ERROR_MESSAGE() AS message
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    THROW 51000, @err, 1;
  END CATCH
END;

GO

