/*--------------------------------------------------------------
Title:        read_user_reset_token
Goal:         Validate a supplied reset token for a given user.
Parameters:
  @payload   nvarchar(max) – JSON body
Sample input:
{
  "user_id"    : "D94F7BC3-1C7E-473C-A6B0-04B0A88F8A7E",
  "reset_token": "XJ7R-58LQ-KD1Z-2W90"
}
Sample output:
{
  "data":[ { "is_valid":true } ],
  "total_tally":1,
  "current_page":1,
  "total_pages":1
}
----------------------------------------------------------------*/
CREATE   PROCEDURE [dbo].[read_user_reset_token]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;
  SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

  DECLARE
      @user_id uniqueidentifier = JSON_VALUE(@payload, '$.user_id'),
      @token   nvarchar(255)    = JSON_VALUE(@payload, '$.reset_token');

  IF @user_id IS NULL OR @token IS NULL
  BEGIN
     SELECT
        (SELECT 0 AS is_valid FOR JSON PATH)  AS data,
        0 AS total_tally, 1 AS current_page, 1 AS total_pages
     FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
     RETURN;
  END

  DECLARE @is_valid bit =
     CASE WHEN EXISTS (
            SELECT 1
            FROM   dbo.users
            WHERE  user_id = @user_id
              AND  password_reset_token = @token
              AND  is_active = 1
          ) THEN 1 ELSE 0 END;

  SELECT
     (SELECT @is_valid AS is_valid FOR JSON PATH) AS data,
     1          AS total_tally,
     1          AS current_page,
     1          AS total_pages
  FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
END;

GO

