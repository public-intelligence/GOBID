CREATE PROCEDURE [dbo].[upsert_user_account]
  @payload nvarchar(max)
AS
BEGIN
  SET NOCOUNT ON;

  DECLARE @now datetime2(3) = SYSUTCDATETIME();

  DECLARE
    @user_id                 uniqueidentifier,
    @personnel_id            uniqueidentifier,
    @email                   nvarchar(320),
    @user_password           nvarchar(128),
    @must_change_password    bit,
    @admission_date          date,
    @discharge_date          date,
    @updated_by              uniqueidentifier,
    @is_active               bit,
    @is_external             bit,
    @person_position_id      uniqueidentifier,
    @phone_number            nvarchar(13);


  SELECT
    @user_id              = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@payload,'$.user_id')),
    @personnel_id         = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@payload,'$.personnel_id')),
    @email                = JSON_VALUE(@payload,'$.email'),
    @user_password        = JSON_VALUE(@payload,'$.user_password'),
    @must_change_password = CASE
                              WHEN JSON_VALUE(@payload,'$.must_change_password') IN ('true','1')  THEN 1
                              WHEN JSON_VALUE(@payload,'$.must_change_password') IN ('false','0') THEN 0
                              ELSE 0
                            END,
    @admission_date       = TRY_CONVERT(date, JSON_VALUE(@payload,'$.admission_date')),
    @discharge_date       = TRY_CONVERT(date, JSON_VALUE(@payload,'$.discharge_date')),
    @updated_by           = TRY_CONVERT(uniqueidentifier, JSON_VALUE(@payload,'$.updated_by')),
    @is_active            = CASE
                              WHEN JSON_VALUE(@payload,'$.is_active') IN ('true','1')  THEN 1
                              WHEN JSON_VALUE(@payload,'$.is_active') IN ('false','0') THEN 0
                              ELSE 1
                            END,
    @is_external          = CASE
                              WHEN JSON_VALUE(@payload,'$.is_external') IN ('true','1')  THEN 1
                              WHEN JSON_VALUE(@payload,'$.is_external') IN ('false','0') THEN 0
                              ELSE NULL  -- NULL = no vino en el payload, se preserva en UPDATE
                            END,
    @person_position_id    =TRY_CONVERT(uniqueidentifier, JSON_VALUE(@payload,'$.person_position_id')),
    @phone_number          = JSON_VALUE(@payload,'$.phone_number');

  IF @user_id IS NULL
    SET @user_id = NEWID();

  BEGIN TRY
    BEGIN TRAN;

    DECLARE @before nvarchar(max);

    IF @user_id IS NOT NULL
    BEGIN
      SELECT @before =
        (SELECT * FROM dbo.users WHERE user_id = @user_id
         FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END

    DECLARE @out TABLE (
      user_id       uniqueidentifier,
      row_version   varbinary(8),
      action        nvarchar(10)
    );

    -- después del SELECT de variables
IF @person_position_id = '00000000-0000-0000-0000-000000000000'
    SET @person_position_id = NULL;

IF @person_position_id IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM dbo.personnel p
       WHERE p.personnel_id = @person_position_id
         AND p.is_active = 1
   )
BEGIN
    THROW 51000, '{"message":"person_position_id no existe en personnel o está inactivo"}', 1;
END; 
    MERGE dbo.users WITH (HOLDLOCK) AS tgt
    USING (SELECT @user_id AS user_id) AS src
       ON tgt.user_id = src.user_id

    WHEN MATCHED THEN
      UPDATE SET
        personnel_id         = @personnel_id,
        email                = @email,
        must_change_password = @must_change_password,
        admission_date       = @admission_date,
        discharge_date       = @discharge_date,
        updated_at           = @now,
        updated_by           = @updated_by,
        is_active            = @is_active,
        -- Preserva el valor existente si no vino en el payload
        is_external          = ISNULL(@is_external, tgt.is_external),
        person_position_id = ISNULL(@person_position_id, tgt.person_position_id),
        phone_number  = ISNULL(@phone_number , tgt.phone_number)

    WHEN NOT MATCHED THEN
      INSERT (user_id, personnel_id, email, user_password, must_change_password,
              admission_date, discharge_date,
              created_at, created_by, is_active, is_external, person_position_id, phone_number)
      VALUES (@user_id, @personnel_id, @email, @user_password, @must_change_password,
              @admission_date, @discharge_date,
              @now, @updated_by, @is_active,
              -- Si no vino en el payload, por defecto es interno (false)
              ISNULL(@is_external, 0), @person_position_id, @phone_number) 

    OUTPUT
      inserted.user_id,
      inserted.row_version,
      $action
    INTO @out;

    IF NOT EXISTS (SELECT 1 FROM @out)
      THROW 51000, '{"message":"No se realizó ninguna acción"}', 1;

    DECLARE @after nvarchar(max);

    SELECT @after =
      (SELECT * FROM dbo.users
       WHERE user_id = (SELECT TOP 1 user_id FROM @out)
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    IF EXISTS (SELECT 1 FROM @out WHERE action = 'UPDATE')
    BEGIN
      INSERT dbo.change_logs (
          log_name, entity_id, is_deleting,
          before_update_entity_json, after_update_entity_json,
          observation, did_rollback, is_active,
          created_at, created_by
      )
      VALUES (
          'update_user_account',
          @user_id, 0,
          @before, @after,
          NULL, 0, 1,
          @now, @updated_by
      );
    END

    SELECT
      user_id     = o.user_id,
      row_version = o.row_version,
      action      = o.action
    FROM @out AS o
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;

    COMMIT;

  END TRY
  BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;

    DECLARE @err nvarchar(2048) =
      (SELECT
         error_number  = ERROR_NUMBER(),
         error_message = ERROR_MESSAGE()
       FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

    THROW 51000, @err, 1;
  END CATCH
END;

GO

