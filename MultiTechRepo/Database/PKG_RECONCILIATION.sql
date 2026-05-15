-- =====================================================
-- PKG_RECONCILIATION Package
-- Handles payment reconciliation and matching logic
-- =====================================================

CREATE OR REPLACE PACKAGE PKG_RECONCILIATION AS

    -- Procedure to create reconciliation record
    PROCEDURE CREATE_RECONCILIATION_RECORD (
        p_payment_id        IN NUMBER,
        p_transaction_id    IN NUMBER,
        p_recon_type        IN VARCHAR2,
        p_expected_amount   IN NUMBER,
        p_actual_amount     IN NUMBER,
        p_gateway_file      IN VARCHAR2,
        p_reconciled_by     IN VARCHAR2,
        p_reconciliation_id OUT NUMBER,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    );

    -- Procedure to perform daily reconciliation
    PROCEDURE PERFORM_DAILY_RECONCILIATION (
        p_reconciliation_date IN DATE,
        p_gateway_file      IN VARCHAR2,
        p_reconciled_by     IN VARCHAR2,
        p_records_matched   OUT NUMBER,
        p_records_mismatched OUT NUMBER,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    );

    -- Procedure to get reconciliation report
    PROCEDURE GET_RECONCILIATION_REPORT (
        p_from_date         IN DATE,
        p_to_date           IN DATE,
        p_match_status      IN VARCHAR2,
        p_result_cursor     OUT SYS_REFCURSOR,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    );

    -- Procedure to update reconciliation status
    PROCEDURE UPDATE_RECONCILIATION_STATUS (
        p_reconciliation_id IN NUMBER,
        p_match_status      IN VARCHAR2,
        p_notes             IN VARCHAR2,
        p_updated_by        IN VARCHAR2,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    );

    -- Procedure to get unreconciled transactions
    PROCEDURE GET_UNRECONCILED_TRANSACTIONS (
        p_from_date         IN DATE,
        p_to_date           IN DATE,
        p_result_cursor     OUT SYS_REFCURSOR,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    );

    -- Procedure to get reconciliation summary
    PROCEDURE GET_RECONCILIATION_SUMMARY (
        p_from_date         IN DATE,
        p_to_date           IN DATE,
        p_result_cursor     OUT SYS_REFCURSOR,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    );

END PKG_RECONCILIATION;
/

CREATE OR REPLACE PACKAGE BODY PKG_RECONCILIATION AS

    -- =====================================================
    -- CREATE_RECONCILIATION_RECORD: Creates a reconciliation record
    -- =====================================================
    PROCEDURE CREATE_RECONCILIATION_RECORD (
        p_payment_id        IN NUMBER,
        p_transaction_id    IN NUMBER,
        p_recon_type        IN VARCHAR2,
        p_expected_amount   IN NUMBER,
        p_actual_amount     IN NUMBER,
        p_gateway_file      IN VARCHAR2,
        p_reconciled_by     IN VARCHAR2,
        p_reconciliation_id OUT NUMBER,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    ) AS
        v_variance_amount   NUMBER;
        v_match_status      VARCHAR2(30);
    BEGIN
        -- Calculate variance
        v_variance_amount := p_actual_amount - p_expected_amount;

        -- Determine match status
        IF v_variance_amount = 0 THEN
            v_match_status := 'MATCHED';
        ELSIF ABS(v_variance_amount) <= 0.01 THEN -- Allow 1 cent tolerance
            v_match_status := 'MATCHED';
        ELSE
            v_match_status := 'MISMATCHED';
        END IF;

        -- Generate reconciliation ID
        SELECT SEQ_RECONCILIATION_ID.NEXTVAL INTO p_reconciliation_id FROM DUAL;

        -- Insert reconciliation record
        INSERT INTO RECONCILIATION_LOG (
            RECONCILIATION_ID,
            PAYMENT_ID,
            TRANSACTION_ID,
            RECONCILIATION_DATE,
            RECONCILIATION_TYPE,
            EXPECTED_AMOUNT,
            ACTUAL_AMOUNT,
            VARIANCE_AMOUNT,
            MATCH_STATUS,
            GATEWAY_FILE_NAME,
            RECONCILED_BY,
            CREATED_DATE
        ) VALUES (
            p_reconciliation_id,
            p_payment_id,
            p_transaction_id,
            SYSDATE,
            p_recon_type,
            p_expected_amount,
            p_actual_amount,
            v_variance_amount,
            v_match_status,
            p_gateway_file,
            p_reconciled_by,
            SYSDATE
        );

        COMMIT;

        p_status := 'SUCCESS';
        p_error_message := 'Reconciliation record created with status: ' || v_match_status;

    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            p_status := 'ERROR';
            p_error_message := 'Error creating reconciliation record: ' || SQLERRM;
    END CREATE_RECONCILIATION_RECORD;

    -- =====================================================
    -- PERFORM_DAILY_RECONCILIATION: Performs daily batch reconciliation
    -- =====================================================
    PROCEDURE PERFORM_DAILY_RECONCILIATION (
        p_reconciliation_date IN DATE,
        p_gateway_file      IN VARCHAR2,
        p_reconciled_by     IN VARCHAR2,
        p_records_matched   OUT NUMBER,
        p_records_mismatched OUT NUMBER,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    ) AS
        v_recon_date        DATE;
        v_reconciliation_id NUMBER;
        v_variance_amount   NUMBER;
        v_match_status      VARCHAR2(30);

        -- Cursor for settled transactions on the reconciliation date
        CURSOR c_transactions IS
            SELECT
                t.TRANSACTION_ID,
                t.PAYMENT_ID,
                t.TRANSACTION_AMOUNT,
                p.SETTLEMENT_DATE
            FROM TRANSACTIONS t
            INNER JOIN PAYMENTS p ON t.PAYMENT_ID = p.PAYMENT_ID
            WHERE t.TRANSACTION_TYPE = 'CAPTURE'
            AND t.TRANSACTION_STATUS = 'COMPLETED'
            AND TRUNC(p.SETTLEMENT_DATE) = TRUNC(v_recon_date)
            AND NOT EXISTS (
                SELECT 1
                FROM RECONCILIATION_LOG rl
                WHERE rl.TRANSACTION_ID = t.TRANSACTION_ID
                AND rl.RECONCILIATION_TYPE = 'DAILY'
                AND TRUNC(rl.RECONCILIATION_DATE) = TRUNC(v_recon_date)
            );

    BEGIN
        v_recon_date := NVL(p_reconciliation_date, SYSDATE);
        p_records_matched := 0;
        p_records_mismatched := 0;

        -- Process each transaction
        FOR rec IN c_transactions LOOP
            -- Simulate gateway amount (in real scenario, this would come from gateway file)
            -- For demo purposes, 90% will match, 10% will have small variance
            IF MOD(rec.TRANSACTION_ID, 10) = 0 THEN
                v_variance_amount := 0.50; -- Small mismatch
            ELSE
                v_variance_amount := 0.00; -- Perfect match
            END IF;

            -- Determine match status
            IF v_variance_amount = 0 THEN
                v_match_status := 'MATCHED';
                p_records_matched := p_records_matched + 1;
            ELSE
                v_match_status := 'MISMATCHED';
                p_records_mismatched := p_records_mismatched + 1;
            END IF;

            -- Generate reconciliation ID
            SELECT SEQ_RECONCILIATION_ID.NEXTVAL INTO v_reconciliation_id FROM DUAL;

            -- Insert reconciliation record
            INSERT INTO RECONCILIATION_LOG (
                RECONCILIATION_ID,
                PAYMENT_ID,
                TRANSACTION_ID,
                RECONCILIATION_DATE,
                RECONCILIATION_TYPE,
                EXPECTED_AMOUNT,
                ACTUAL_AMOUNT,
                VARIANCE_AMOUNT,
                MATCH_STATUS,
                GATEWAY_FILE_NAME,
                RECONCILED_BY,
                CREATED_DATE
            ) VALUES (
                v_reconciliation_id,
                rec.PAYMENT_ID,
                rec.TRANSACTION_ID,
                v_recon_date,
                'DAILY',
                rec.TRANSACTION_AMOUNT,
                rec.TRANSACTION_AMOUNT - v_variance_amount,
                v_variance_amount,
                v_match_status,
                p_gateway_file,
                p_reconciled_by,
                SYSDATE
            );
        END LOOP;

        COMMIT;

        p_status := 'SUCCESS';
        p_error_message := 'Daily reconciliation completed. Matched: ' || p_records_matched ||
                          ', Mismatched: ' || p_records_mismatched;

    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            p_status := 'ERROR';
            p_error_message := 'Error performing daily reconciliation: ' || SQLERRM;
    END PERFORM_DAILY_RECONCILIATION;

    -- =====================================================
    -- GET_RECONCILIATION_REPORT: Gets reconciliation report
    -- =====================================================
    PROCEDURE GET_RECONCILIATION_REPORT (
        p_from_date         IN DATE,
        p_to_date           IN DATE,
        p_match_status      IN VARCHAR2,
        p_result_cursor     OUT SYS_REFCURSOR,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    ) AS
    BEGIN
        OPEN p_result_cursor FOR
            SELECT
                rl.RECONCILIATION_ID,
                rl.PAYMENT_ID,
                p.REFERENCE_NUMBER,
                rl.TRANSACTION_ID,
                rl.RECONCILIATION_DATE,
                rl.RECONCILIATION_TYPE,
                rl.EXPECTED_AMOUNT,
                rl.ACTUAL_AMOUNT,
                rl.VARIANCE_AMOUNT,
                rl.MATCH_STATUS,
                rl.GATEWAY_FILE_NAME,
                rl.RECONCILED_BY,
                rl.NOTES,
                c.CUSTOMER_NAME,
                c.ACCOUNT_NUMBER
            FROM RECONCILIATION_LOG rl
            INNER JOIN PAYMENTS p ON rl.PAYMENT_ID = p.PAYMENT_ID
            INNER JOIN CUSTOMERS c ON p.CUSTOMER_ID = c.CUSTOMER_ID
            WHERE rl.RECONCILIATION_DATE BETWEEN NVL(p_from_date, rl.RECONCILIATION_DATE)
                                            AND NVL(p_to_date, rl.RECONCILIATION_DATE)
            AND (p_match_status IS NULL OR rl.MATCH_STATUS = p_match_status)
            ORDER BY rl.RECONCILIATION_DATE DESC, rl.RECONCILIATION_ID DESC;

        p_status := 'SUCCESS';
        p_error_message := 'Reconciliation report retrieved successfully';

    EXCEPTION
        WHEN OTHERS THEN
            p_status := 'ERROR';
            p_error_message := 'Error retrieving reconciliation report: ' || SQLERRM;
    END GET_RECONCILIATION_REPORT;

    -- =====================================================
    -- UPDATE_RECONCILIATION_STATUS: Updates reconciliation status
    -- =====================================================
    PROCEDURE UPDATE_RECONCILIATION_STATUS (
        p_reconciliation_id IN NUMBER,
        p_match_status      IN VARCHAR2,
        p_notes             IN VARCHAR2,
        p_updated_by        IN VARCHAR2,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    ) AS
        v_recon_count       NUMBER;
    BEGIN
        -- Check if reconciliation record exists
        SELECT COUNT(*) INTO v_recon_count
        FROM RECONCILIATION_LOG
        WHERE RECONCILIATION_ID = p_reconciliation_id;

        IF v_recon_count = 0 THEN
            p_status := 'ERROR';
            p_error_message := 'Reconciliation record not found';
            RETURN;
        END IF;

        -- Update reconciliation status
        UPDATE RECONCILIATION_LOG
        SET MATCH_STATUS = p_match_status,
            NOTES = NVL(p_notes, NOTES),
            RECONCILED_BY = p_updated_by
        WHERE RECONCILIATION_ID = p_reconciliation_id;

        COMMIT;

        p_status := 'SUCCESS';
        p_error_message := 'Reconciliation status updated successfully';

    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            p_status := 'ERROR';
            p_error_message := 'Error updating reconciliation status: ' || SQLERRM;
    END UPDATE_RECONCILIATION_STATUS;

    -- =====================================================
    -- GET_UNRECONCILED_TRANSACTIONS: Gets unreconciled transactions
    -- =====================================================
    PROCEDURE GET_UNRECONCILED_TRANSACTIONS (
        p_from_date         IN DATE,
        p_to_date           IN DATE,
        p_result_cursor     OUT SYS_REFCURSOR,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    ) AS
    BEGIN
        OPEN p_result_cursor FOR
            SELECT
                t.TRANSACTION_ID,
                t.PAYMENT_ID,
                p.REFERENCE_NUMBER,
                t.TRANSACTION_TYPE,
                t.TRANSACTION_AMOUNT,
                t.TRANSACTION_STATUS,
                t.TRANSACTION_DATE,
                p.SETTLEMENT_DATE,
                c.CUSTOMER_NAME,
                c.ACCOUNT_NUMBER
            FROM TRANSACTIONS t
            INNER JOIN PAYMENTS p ON t.PAYMENT_ID = p.PAYMENT_ID
            INNER JOIN CUSTOMERS c ON p.CUSTOMER_ID = c.CUSTOMER_ID
            WHERE t.TRANSACTION_TYPE IN ('CAPTURE', 'REFUND')
            AND t.TRANSACTION_STATUS = 'COMPLETED'
            AND t.TRANSACTION_DATE BETWEEN NVL(p_from_date, t.TRANSACTION_DATE)
                                      AND NVL(p_to_date, t.TRANSACTION_DATE)
            AND NOT EXISTS (
                SELECT 1
                FROM RECONCILIATION_LOG rl
                WHERE rl.TRANSACTION_ID = t.TRANSACTION_ID
            )
            ORDER BY t.TRANSACTION_DATE DESC;

        p_status := 'SUCCESS';
        p_error_message := 'Unreconciled transactions retrieved successfully';

    EXCEPTION
        WHEN OTHERS THEN
            p_status := 'ERROR';
            p_error_message := 'Error retrieving unreconciled transactions: ' || SQLERRM;
    END GET_UNRECONCILED_TRANSACTIONS;

    -- =====================================================
    -- GET_RECONCILIATION_SUMMARY: Gets reconciliation summary
    -- =====================================================
    PROCEDURE GET_RECONCILIATION_SUMMARY (
        p_from_date         IN DATE,
        p_to_date           IN DATE,
        p_result_cursor     OUT SYS_REFCURSOR,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    ) AS
    BEGIN
        OPEN p_result_cursor FOR
            SELECT
                TRUNC(RECONCILIATION_DATE) AS RECON_DATE,
                RECONCILIATION_TYPE,
                MATCH_STATUS,
                COUNT(*) AS RECORD_COUNT,
                SUM(EXPECTED_AMOUNT) AS TOTAL_EXPECTED,
                SUM(ACTUAL_AMOUNT) AS TOTAL_ACTUAL,
                SUM(ABS(VARIANCE_AMOUNT)) AS TOTAL_VARIANCE
            FROM RECONCILIATION_LOG
            WHERE RECONCILIATION_DATE BETWEEN NVL(p_from_date, RECONCILIATION_DATE)
                                         AND NVL(p_to_date, RECONCILIATION_DATE)
            GROUP BY TRUNC(RECONCILIATION_DATE), RECONCILIATION_TYPE, MATCH_STATUS
            ORDER BY TRUNC(RECONCILIATION_DATE) DESC, RECONCILIATION_TYPE, MATCH_STATUS;

        p_status := 'SUCCESS';
        p_error_message := 'Reconciliation summary retrieved successfully';

    EXCEPTION
        WHEN OTHERS THEN
            p_status := 'ERROR';
            p_error_message := 'Error retrieving reconciliation summary: ' || SQLERRM;
    END GET_RECONCILIATION_SUMMARY;

END PKG_RECONCILIATION;
/

-- Grant execute permissions (adjust as needed for your environment)
-- GRANT EXECUTE ON PKG_RECONCILIATION TO your_app_user;
