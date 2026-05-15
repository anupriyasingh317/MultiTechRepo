-- =====================================================
-- PKG_TRANSACTION_MANAGEMENT Package
-- Handles transaction queries, updates, and reporting
-- =====================================================

CREATE OR REPLACE PACKAGE PKG_TRANSACTION_MANAGEMENT AS

    -- Procedure to get transaction details by ID
    PROCEDURE GET_TRANSACTION_DETAILS (
        p_transaction_id    IN NUMBER,
        p_result_cursor     OUT SYS_REFCURSOR,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    );

    -- Procedure to get all transactions for a payment
    PROCEDURE GET_PAYMENT_TRANSACTIONS (
        p_payment_id        IN NUMBER,
        p_result_cursor     OUT SYS_REFCURSOR,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    );

    -- Procedure to search transactions by criteria
    PROCEDURE SEARCH_TRANSACTIONS (
        p_from_date         IN DATE,
        p_to_date           IN DATE,
        p_transaction_type  IN VARCHAR2,
        p_transaction_status IN VARCHAR2,
        p_min_amount        IN NUMBER,
        p_max_amount        IN NUMBER,
        p_result_cursor     OUT SYS_REFCURSOR,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    );

    -- Procedure to update transaction status
    PROCEDURE UPDATE_TRANSACTION_STATUS (
        p_transaction_id    IN NUMBER,
        p_new_status        IN VARCHAR2,
        p_response_message  IN VARCHAR2,
        p_updated_by        IN VARCHAR2,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    );

    -- Procedure to create refund transaction
    PROCEDURE CREATE_REFUND_TRANSACTION (
        p_payment_id        IN NUMBER,
        p_refund_amount     IN NUMBER,
        p_reason            IN VARCHAR2,
        p_created_by        IN VARCHAR2,
        p_transaction_id    OUT NUMBER,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    );

    -- Procedure to get transaction summary by date range
    PROCEDURE GET_TRANSACTION_SUMMARY (
        p_from_date         IN DATE,
        p_to_date           IN DATE,
        p_result_cursor     OUT SYS_REFCURSOR,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    );

END PKG_TRANSACTION_MANAGEMENT;
/

CREATE OR REPLACE PACKAGE BODY PKG_TRANSACTION_MANAGEMENT AS

    -- =====================================================
    -- GET_TRANSACTION_DETAILS: Retrieves transaction details by ID
    -- =====================================================
    PROCEDURE GET_TRANSACTION_DETAILS (
        p_transaction_id    IN NUMBER,
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
                t.GATEWAY_TXN_ID,
                t.PROCESSOR_CODE,
                t.RESPONSE_CODE,
                t.RESPONSE_MESSAGE,
                t.TRANSACTION_DATE,
                t.PROCESSED_DATE,
                t.CREATED_BY,
                c.CUSTOMER_NAME,
                c.ACCOUNT_NUMBER
            FROM TRANSACTIONS t
            INNER JOIN PAYMENTS p ON t.PAYMENT_ID = p.PAYMENT_ID
            INNER JOIN CUSTOMERS c ON p.CUSTOMER_ID = c.CUSTOMER_ID
            WHERE t.TRANSACTION_ID = p_transaction_id;

        p_status := 'SUCCESS';
        p_error_message := 'Transaction details retrieved successfully';

    EXCEPTION
        WHEN OTHERS THEN
            p_status := 'ERROR';
            p_error_message := 'Error retrieving transaction details: ' || SQLERRM;
    END GET_TRANSACTION_DETAILS;

    -- =====================================================
    -- GET_PAYMENT_TRANSACTIONS: Gets all transactions for a payment
    -- =====================================================
    PROCEDURE GET_PAYMENT_TRANSACTIONS (
        p_payment_id        IN NUMBER,
        p_result_cursor     OUT SYS_REFCURSOR,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    ) AS
    BEGIN
        OPEN p_result_cursor FOR
            SELECT
                t.TRANSACTION_ID,
                t.TRANSACTION_TYPE,
                t.TRANSACTION_AMOUNT,
                t.TRANSACTION_STATUS,
                t.GATEWAY_TXN_ID,
                t.RESPONSE_CODE,
                t.RESPONSE_MESSAGE,
                t.TRANSACTION_DATE,
                t.PROCESSED_DATE,
                t.CREATED_BY
            FROM TRANSACTIONS t
            WHERE t.PAYMENT_ID = p_payment_id
            ORDER BY t.TRANSACTION_DATE DESC;

        p_status := 'SUCCESS';
        p_error_message := 'Payment transactions retrieved successfully';

    EXCEPTION
        WHEN OTHERS THEN
            p_status := 'ERROR';
            p_error_message := 'Error retrieving payment transactions: ' || SQLERRM;
    END GET_PAYMENT_TRANSACTIONS;

    -- =====================================================
    -- SEARCH_TRANSACTIONS: Search transactions by multiple criteria
    -- =====================================================
    PROCEDURE SEARCH_TRANSACTIONS (
        p_from_date         IN DATE,
        p_to_date           IN DATE,
        p_transaction_type  IN VARCHAR2,
        p_transaction_status IN VARCHAR2,
        p_min_amount        IN NUMBER,
        p_max_amount        IN NUMBER,
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
                t.GATEWAY_TXN_ID,
                t.RESPONSE_CODE,
                t.RESPONSE_MESSAGE,
                t.TRANSACTION_DATE,
                c.CUSTOMER_NAME,
                c.ACCOUNT_NUMBER
            FROM TRANSACTIONS t
            INNER JOIN PAYMENTS p ON t.PAYMENT_ID = p.PAYMENT_ID
            INNER JOIN CUSTOMERS c ON p.CUSTOMER_ID = c.CUSTOMER_ID
            WHERE t.TRANSACTION_DATE BETWEEN NVL(p_from_date, t.TRANSACTION_DATE)
                                        AND NVL(p_to_date, t.TRANSACTION_DATE)
            AND (p_transaction_type IS NULL OR t.TRANSACTION_TYPE = p_transaction_type)
            AND (p_transaction_status IS NULL OR t.TRANSACTION_STATUS = p_transaction_status)
            AND (p_min_amount IS NULL OR t.TRANSACTION_AMOUNT >= p_min_amount)
            AND (p_max_amount IS NULL OR t.TRANSACTION_AMOUNT <= p_max_amount)
            ORDER BY t.TRANSACTION_DATE DESC;

        p_status := 'SUCCESS';
        p_error_message := 'Transactions retrieved successfully';

    EXCEPTION
        WHEN OTHERS THEN
            p_status := 'ERROR';
            p_error_message := 'Error searching transactions: ' || SQLERRM;
    END SEARCH_TRANSACTIONS;

    -- =====================================================
    -- UPDATE_TRANSACTION_STATUS: Updates transaction status
    -- =====================================================
    PROCEDURE UPDATE_TRANSACTION_STATUS (
        p_transaction_id    IN NUMBER,
        p_new_status        IN VARCHAR2,
        p_response_message  IN VARCHAR2,
        p_updated_by        IN VARCHAR2,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    ) AS
        v_transaction_count NUMBER;
        v_current_status    VARCHAR2(30);
    BEGIN
        -- Check if transaction exists
        SELECT COUNT(*), MAX(TRANSACTION_STATUS)
        INTO v_transaction_count, v_current_status
        FROM TRANSACTIONS
        WHERE TRANSACTION_ID = p_transaction_id;

        IF v_transaction_count = 0 THEN
            p_status := 'ERROR';
            p_error_message := 'Transaction not found';
            RETURN;
        END IF;

        -- Update transaction status
        UPDATE TRANSACTIONS
        SET TRANSACTION_STATUS = p_new_status,
            RESPONSE_MESSAGE = NVL(p_response_message, RESPONSE_MESSAGE),
            PROCESSED_DATE = SYSDATE
        WHERE TRANSACTION_ID = p_transaction_id;

        COMMIT;

        p_status := 'SUCCESS';
        p_error_message := 'Transaction status updated from ' || v_current_status || ' to ' || p_new_status;

    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            p_status := 'ERROR';
            p_error_message := 'Error updating transaction status: ' || SQLERRM;
    END UPDATE_TRANSACTION_STATUS;

    -- =====================================================
    -- CREATE_REFUND_TRANSACTION: Creates a refund transaction
    -- =====================================================
    PROCEDURE CREATE_REFUND_TRANSACTION (
        p_payment_id        IN NUMBER,
        p_refund_amount     IN NUMBER,
        p_reason            IN VARCHAR2,
        p_created_by        IN VARCHAR2,
        p_transaction_id    OUT NUMBER,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    ) AS
        v_payment_count     NUMBER;
        v_payment_status    VARCHAR2(30);
        v_payment_amount    NUMBER;
        v_total_refunded    NUMBER := 0;
    BEGIN
        -- Validate payment exists and is settled
        SELECT COUNT(*), MAX(PAYMENT_STATUS), MAX(PAYMENT_AMOUNT)
        INTO v_payment_count, v_payment_status, v_payment_amount
        FROM PAYMENTS
        WHERE PAYMENT_ID = p_payment_id;

        IF v_payment_count = 0 THEN
            p_status := 'ERROR';
            p_error_message := 'Payment not found';
            RETURN;
        END IF;

        IF v_payment_status != 'SETTLED' THEN
            p_status := 'ERROR';
            p_error_message := 'Only settled payments can be refunded. Current status: ' || v_payment_status;
            RETURN;
        END IF;

        -- Calculate total already refunded
        SELECT NVL(SUM(TRANSACTION_AMOUNT), 0)
        INTO v_total_refunded
        FROM TRANSACTIONS
        WHERE PAYMENT_ID = p_payment_id
        AND TRANSACTION_TYPE = 'REFUND'
        AND TRANSACTION_STATUS = 'COMPLETED';

        -- Check if refund amount is valid
        IF (v_total_refunded + p_refund_amount) > v_payment_amount THEN
            p_status := 'ERROR';
            p_error_message := 'Refund amount exceeds available balance. Already refunded: ' || v_total_refunded;
            RETURN;
        END IF;

        -- Generate transaction ID
        SELECT SEQ_TRANSACTION_ID.NEXTVAL INTO p_transaction_id FROM DUAL;

        -- Insert refund transaction
        INSERT INTO TRANSACTIONS (
            TRANSACTION_ID,
            PAYMENT_ID,
            TRANSACTION_TYPE,
            TRANSACTION_AMOUNT,
            TRANSACTION_STATUS,
            RESPONSE_CODE,
            RESPONSE_MESSAGE,
            TRANSACTION_DATE,
            PROCESSED_DATE,
            CREATED_BY
        ) VALUES (
            p_transaction_id,
            p_payment_id,
            'REFUND',
            p_refund_amount,
            'COMPLETED',
            '00',
            p_reason,
            SYSDATE,
            SYSDATE,
            p_created_by
        );

        -- Update payment status if fully refunded
        IF (v_total_refunded + p_refund_amount) = v_payment_amount THEN
            UPDATE PAYMENTS
            SET PAYMENT_STATUS = 'REFUNDED',
                UPDATED_DATE = SYSDATE,
                UPDATED_BY = p_created_by
            WHERE PAYMENT_ID = p_payment_id;
        END IF;

        COMMIT;

        p_status := 'SUCCESS';
        p_error_message := 'Refund transaction created successfully';

    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            p_status := 'ERROR';
            p_error_message := 'Error creating refund transaction: ' || SQLERRM;
    END CREATE_REFUND_TRANSACTION;

    -- =====================================================
    -- GET_TRANSACTION_SUMMARY: Gets transaction summary by date range
    -- =====================================================
    PROCEDURE GET_TRANSACTION_SUMMARY (
        p_from_date         IN DATE,
        p_to_date           IN DATE,
        p_result_cursor     OUT SYS_REFCURSOR,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    ) AS
    BEGIN
        OPEN p_result_cursor FOR
            SELECT
                TRANSACTION_TYPE,
                TRANSACTION_STATUS,
                COUNT(*) AS TRANSACTION_COUNT,
                SUM(TRANSACTION_AMOUNT) AS TOTAL_AMOUNT,
                AVG(TRANSACTION_AMOUNT) AS AVERAGE_AMOUNT,
                MIN(TRANSACTION_AMOUNT) AS MIN_AMOUNT,
                MAX(TRANSACTION_AMOUNT) AS MAX_AMOUNT
            FROM TRANSACTIONS
            WHERE TRANSACTION_DATE BETWEEN NVL(p_from_date, TRANSACTION_DATE)
                                      AND NVL(p_to_date, TRANSACTION_DATE)
            GROUP BY TRANSACTION_TYPE, TRANSACTION_STATUS
            ORDER BY TRANSACTION_TYPE, TRANSACTION_STATUS;

        p_status := 'SUCCESS';
        p_error_message := 'Transaction summary retrieved successfully';

    EXCEPTION
        WHEN OTHERS THEN
            p_status := 'ERROR';
            p_error_message := 'Error retrieving transaction summary: ' || SQLERRM;
    END GET_TRANSACTION_SUMMARY;

END PKG_TRANSACTION_MANAGEMENT;
/

-- Grant execute permissions (adjust as needed for your environment)
-- GRANT EXECUTE ON PKG_TRANSACTION_MANAGEMENT TO your_app_user;
