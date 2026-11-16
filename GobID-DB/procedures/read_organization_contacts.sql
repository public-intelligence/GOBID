-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[read_organization_contacts]
	@organization_id int
AS
BEGIN
  SET NOCOUNT ON;

  BEGIN TRY
    SELECT (
      SELECT oc.*
      FROM   dbo.organization_contacts AS oc
      WHERE  oc.organization_id = @organization_id
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

