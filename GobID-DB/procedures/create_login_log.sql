/*--------------------------------------------------------------
Title:        create_login_log
Goal:         Insert a new audit line in dbo.login_logs and return
              the generated ID.
Parameters:
  @payload  nvarchar(max)  (required) – JSON body
    {
      "login_info"                 : "User johndoe",
      "ip"                         : "203.0.113.24",
      "header_values"              : "UA=Edge/125",
      "login_successful"           : true,
      "organization_id"            : 3,
      "organization_application_id": 7,
      "created_by"                 : "69FD63B3-1C9E-4F05-B45F-F6EC5F945C0E"
    }
Sample output:
  { "login_log_id": 1024 }
----------------------------------------------------------------*/
CREATE   PROCEDURE [dbo].[create_login_log]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;

  DECLARE
    @login_info     nvarchar(500)    = JSON_VALUE(@payload, '$.login_info'),
    @ip             nvarchar(45)     = JSON_VALUE(@payload, '$.ip'),
    @header_values  nvarchar(255)    = JSON_VALUE(@payload, '$.header_values'),
    @login_success  bit              = JSON_VALUE(@payload, '$.login_successful'),
    @org_id         int              = JSON_VALUE(@payload, '$.organization_id'),
    @org_app_id     int              = JSON_VALUE(@payload, '$.organization_application_id'),
    @created_by     uniqueidentifier = JSON_VALUE(@payload, '$.created_by');

  BEGIN TRY
    BEGIN TRAN;

    INSERT dbo.login_logs
          (login_info, ip, header_values, login_successful,
           organization_id, organization_application_id, created_by)
    VALUES(@login_info, @ip, @header_values, @login_success,
           @org_id, @org_app_id, @created_by);

    DECLARE @new_id int = SCOPE_IDENTITY();

    COMMIT;

    SELECT @new_id AS login_log_id
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;

    DECLARE @err nvarchar(2048) = (
      SELECT ERROR_NUMBER()   AS error_number,
             ERROR_SEVERITY() AS error_severity,
             ERROR_STATE()    AS error_state,
             ERROR_PROCEDURE()AS error_procedure,
             ERROR_LINE()     AS error_line,
             ERROR_MESSAGE()  AS error_message
      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );
    THROW 51000, @err, 1;
  END CATCH;
END;

GO

