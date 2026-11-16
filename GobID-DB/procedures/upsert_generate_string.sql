CREATE PROCEDURE [dbo].[upsert_generate_string]
    @application_id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        BEGIN TRAN;

        /* 🔥 VALIDACIÓN */
        IF @application_id IS NULL
            THROW 51000, '{"message":"application_id is required"}', 1;

        /* 🔥 BEFORE */
        DECLARE @before NVARCHAR(MAX);

        SELECT @before =
          (SELECT *
           FROM dbo.applications
           WHERE application_id = @application_id
           FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

        /* 🔥 UPDATE */
        DECLARE @out TABLE (
            application_id UNIQUEIDENTIFIER,
            random_string NVARCHAR(40)
        );

        UPDATE dbo.applications
        SET random_string = SUBSTRING(CONVERT(nvarchar(40), NEWID()), 1, 12),
            updated_at    = SYSUTCDATETIME()
        OUTPUT INSERTED.application_id, INSERTED.random_string
        INTO @out
        WHERE application_id = @application_id;

        IF @@ROWCOUNT = 0
            THROW 51000, '{"message":"application not found"}', 1;

        /* 🔥 AFTER */
        DECLARE @after NVARCHAR(MAX);

        SELECT @after =
          (SELECT *
           FROM dbo.applications
           WHERE application_id = @application_id
           FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);

        /* 🔥 LOG */
        INSERT dbo.change_logs (
            log_name, entity_id, is_deleting,
            before_update_entity_json, after_update_entity_json,
            observation, did_rollback, is_active,
            created_at, created_by
        )
        VALUES (
            'update_generate_string',
            @application_id,
            0,
            @before,
            @after,
            NULL,
            0,
            1,
            SYSUTCDATETIME(),
            NULL
        );

        COMMIT;

        /* 🔥 RESULT */
        SELECT application_id, random_string
        FROM @out
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;

    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK;

        DECLARE @err NVARCHAR(2048) = (
            SELECT ERROR_NUMBER()   AS error_number,
                   ERROR_SEVERITY() AS error_severity,
                   ERROR_STATE()    AS error_state,
                   ERROR_PROCEDURE()AS error_proc,
                   ERROR_LINE()     AS error_line,
                   ERROR_MESSAGE()  AS error_message
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        );

        THROW 51000, @err, 1;
    END CATCH
END;

GO

