-- =====================================================
-- PKG_PAYMENT_PROCESSING Package
-- Handles payment creation, authorization, and settlement
-- =====================================================

CREATE OR REPLACE PACKAGE PKG_PAYMENT_PROCESSING AS

    -- Procedure to create a new payment
    PROCEDURE CREATE_PAYMENT (
        p_customer_id       IN NUMBER,
        p_payment_method_id IN NUMBER,
        p_payment_amount    IN NUMBER,
        p_currency_code     IN VARCHAR2,
        p_payment_type      IN VARCHAR2,
        p_merchant_id       IN VARCHAR2,
        p_description       IN VARCHAR2,
        p_created_by        IN VARCHAR2,
        p_payment_id        OUT NUMBER,
        p_reference_number  OUT VARCHAR2,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    );

    -- Procedure to authorize a payment
    PROCEDURE AUTHORIZE_PAYMENT (
        p_payment_id        IN NUMBER,
        p_authorization_code IN VARCHAR2,
        p_gateway_response  IN VARCHAR2,
        p_updated_by        IN VARCHAR2,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    );

    -- Procedure to settle a payment
    PROCEDURE SETTLE_PAYMENT (
        p_payment_id        IN NUMBER,
        p_settlement_amount IN NUMBER,
        p_updated_by        IN VARCHAR2,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    );

    -- Procedure to cancel a payment
    PROCEDURE CANCEL_PAYMENT (
        p_payment_id        IN NUMBER,
        p_reason            IN VARCHAR2,
        p_updated_by        IN VARCHAR2,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    );

    -- Procedure to get payment details
    PROCEDURE GET_PAYMENT_DETAILS (
        p_payment_id        IN NUMBER,
        p_result_cursor     OUT SYS_REFCURSOR,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    );

    -- Procedure to get payments by customer
    PROCEDURE GET_CUSTOMER_PAYMENTS (
        p_customer_id       IN NUMBER,
        p_from_date         IN DATE,
        p_to_date           IN DATE,
        p_result_cursor     OUT SYS_REFCURSOR,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    );

END PKG_PAYMENT_PROCESSING;
/

CREATE OR REPLACE PACKAGE BODY PKG_PAYMENT_PROCESSING AS

    -- =====================================================
    -- CREATE_PAYMENT: Creates a new payment record
    -- =====================================================
    PROCEDURE CREATE_PAYMENT (
        p_customer_id       IN NUMBER,
        p_payment_method_id IN NUMBER,
        p_payment_amount    IN NUMBER,
        p_currency_code     IN VARCHAR2,
        p_payment_type      IN VARCHAR2,
        p_merchant_id       IN VARCHAR2,
        p_description       IN VARCHAR2,
        p_created_by        IN VARCHAR2,
        p_payment_id        OUT NUMBER,
        p_reference_number  OUT VARCHAR2,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    ) AS
        v_customer_count    NUMBER;
        v_method_count      NUMBER;
        v_timestamp         VARCHAR2(50);
    BEGIN
        -- Validate customer exists
        SELECT COUNT(*) INTO v_customer_count
        FROM CUSTOMERS
        WHERE CUSTOMER_ID = p_customer_id
        AND ACCOUNT_STATUS = 'ACTIVE';

        IF v_customer_count = 0 THEN
            p_status := 'ERROR';
            p_error_message := 'Customer not found or inactive';
            RETURN;
        END IF;

        -- Validate payment method exists
        SELECT COUNT(*) INTO v_method_count
        FROM PAYMENT_METHODS
        WHERE METHOD_ID = p_payment_method_id
        AND CUSTOMER_ID = p_customer_id
        AND STATUS = 'ACTIVE';

        IF v_method_count = 0 THEN
            p_status := 'ERROR';
            p_error_message := 'Payment method not found or inactive';
            RETURN;
        END IF;

        -- Validate amount
        IF p_payment_amount <= 0 THEN
            p_status := 'ERROR';
            p_error_message := 'Payment amount must be greater than zero';
            RETURN;
        END IF;

        -- Generate payment ID
        SELECT SEQ_PAYMENT_ID.NEXTVAL INTO p_payment_id FROM DUAL;

        -- Generate reference number
        v_timestamp := TO_CHAR(SYSDATE, 'YYYYMMDDHH24MISS');
        p_reference_number := 'PAY' || v_timestamp || LPAD(p_payment_id, 8, '0');

        -- Insert payment record
        INSERT INTO PAYMENTS (
            PAYMENT_ID,
            CUSTOMER_ID,
            PAYMENT_METHOD_ID,
            PAYMENT_AMOUNT,
            CURRENCY_CODE,
            PAYMENT_STATUS,
            PAYMENT_TYPE,
            REFERENCE_NUMBER,
            MERCHANT_ID,
            DESCRIPTION,
            CREATED_DATE,
            UPDATED_DATE,
            CREATED_BY,
            UPDATED_BY
        ) VALUES (
            p_payment_id,
            p_customer_id,
            p_payment_method_id,
            p_payment_amount,
            NVL(p_currency_code, 'USD'),
            'PENDING',
            p_payment_type,
            p_reference_number,
            p_merchant_id,
            p_description,
            SYSDATE,
            SYSDATE,
            p_created_by,
            p_created_by
        );

        COMMIT;

        p_status := 'SUCCESS';
        p_error_message := 'Payment created successfully';

    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            p_status := 'ERROR';
            p_error_message := 'Error creating payment: ' || SQLERRM;
    END CREATE_PAYMENT;

    -- =====================================================
    -- AUTHORIZE_PAYMENT: Authorizes a payment
    -- =====================================================
    PROCEDURE AUTHORIZE_PAYMENT (
        p_payment_id        IN NUMBER,
        p_authorization_code IN VARCHAR2,
        p_gateway_response  IN VARCHAR2,
        p_updated_by        IN VARCHAR2,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    ) AS
        v_payment_count     NUMBER;
        v_current_status    VARCHAR2(30);
        v_transaction_id    NUMBER;
        v_payment_amount    NUMBER;
    BEGIN
        -- Check if payment exists
        SELECT COUNT(*), MAX(PAYMENT_STATUS), MAX(PAYMENT_AMOUNT)
        INTO v_payment_count, v_current_status, v_payment_amount
        FROM PAYMENTS
        WHERE PAYMENT_ID = p_payment_id;

        IF v_payment_count = 0 THEN
            p_status := 'ERROR';
            p_error_message := 'Payment not found';
            RETURN;
        END IF;

        IF v_current_status != 'PENDING' THEN
            p_status := 'ERROR';
            p_error_message := 'Payment cannot be authorized. Current status: ' || v_current_status;
            RETURN;
        END IF;

        -- Update payment status
        UPDATE PAYMENTS
        SET PAYMENT_STATUS = 'AUTHORIZED',
            AUTHORIZATION_CODE = p_authorization_code,
            GATEWAY_RESPONSE = p_gateway_response,
            PROCESSED_DATE = SYSDATE,
            UPDATED_DATE = SYSDATE,
            UPDATED_BY = p_updated_by
        WHERE PAYMENT_ID = p_payment_id;

        -- Create transaction record
        SELECT SEQ_TRANSACTION_ID.NEXTVAL INTO v_transaction_id FROM DUAL;

        INSERT INTO TRANSACTIONS (
            TRANSACTION_ID,
            PAYMENT_ID,
            TRANSACTION_TYPE,
            TRANSACTION_AMOUNT,
            TRANSACTION_STATUS,
            GATEWAY_TXN_ID,
            RESPONSE_CODE,
            RESPONSE_MESSAGE,
            TRANSACTION_DATE,
            PROCESSED_DATE,
            CREATED_BY
        ) VALUES (
            v_transaction_id,
            p_payment_id,
            'AUTH',
            v_payment_amount,
            'APPROVED',
            p_authorization_code,
            '00',
            p_gateway_response,
            SYSDATE,
            SYSDATE,
            p_updated_by
        );

        COMMIT;

        p_status := 'SUCCESS';
        p_error_message := 'Payment authorized successfully';

    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            p_status := 'ERROR';
            p_error_message := 'Error authorizing payment: ' || SQLERRM;
    END AUTHORIZE_PAYMENT;

    -- =====================================================
    -- SETTLE_PAYMENT: Settles an authorized payment
    -- =====================================================
    PROCEDURE SETTLE_PAYMENT (
        p_payment_id        IN NUMBER,
        p_settlement_amount IN NUMBER,
        p_updated_by        IN VARCHAR2,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    ) AS
        v_payment_count     NUMBER;
        v_current_status    VARCHAR2(30);
        v_transaction_id    NUMBER;
    BEGIN
        -- Check if payment exists and is authorized
        SELECT COUNT(*), MAX(PAYMENT_STATUS)
        INTO v_payment_count, v_current_status
        FROM PAYMENTS
        WHERE PAYMENT_ID = p_payment_id;

        IF v_payment_count = 0 THEN
            p_status := 'ERROR';
            p_error_message := 'Payment not found';
            RETURN;
        END IF;

        IF v_current_status != 'AUTHORIZED' THEN
            p_status := 'ERROR';
            p_error_message := 'Payment must be authorized before settlement. Current status: ' || v_current_status;
            RETURN;
        END IF;

        -- Update payment status
        UPDATE PAYMENTS
        SET PAYMENT_STATUS = 'SETTLED',
            SETTLEMENT_DATE = SYSDATE,
            UPDATED_DATE = SYSDATE,
            UPDATED_BY = p_updated_by
        WHERE PAYMENT_ID = p_payment_id;

        -- Create settlement transaction record
        SELECT SEQ_TRANSACTION_ID.NEXTVAL INTO v_transaction_id FROM DUAL;

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
            v_transaction_id,
            p_payment_id,
            'CAPTURE',
            p_settlement_amount,
            'COMPLETED',
            '00',
            'Settlement completed',
            SYSDATE,
            SYSDATE,
            p_updated_by
        );

        COMMIT;

        p_status := 'SUCCESS';
        p_error_message := 'Payment settled successfully';

    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            p_status := 'ERROR';
            p_error_message := 'Error settling payment: ' || SQLERRM;
    END SETTLE_PAYMENT;

    -- =====================================================
    -- CANCEL_PAYMENT: Cancels a payment
    -- =====================================================
    PROCEDURE CANCEL_PAYMENT (
        p_payment_id        IN NUMBER,
        p_reason            IN VARCHAR2,
        p_updated_by        IN VARCHAR2,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    ) AS
        v_payment_count     NUMBER;
        v_current_status    VARCHAR2(30);
        v_transaction_id    NUMBER;
        v_payment_amount    NUMBER;
    BEGIN
        -- Check if payment exists
        SELECT COUNT(*), MAX(PAYMENT_STATUS), MAX(PAYMENT_AMOUNT)
        INTO v_payment_count, v_current_status, v_payment_amount
        FROM PAYMENTS
        WHERE PAYMENT_ID = p_payment_id;

        IF v_payment_count = 0 THEN
            p_status := 'ERROR';
            p_error_message := 'Payment not found';
            RETURN;
        END IF;

        IF v_current_status IN ('SETTLED', 'CANCELLED') THEN
            p_status := 'ERROR';
            p_error_message := 'Payment cannot be cancelled. Current status: ' || v_current_status;
            RETURN;
        END IF;

        -- Update payment status
        UPDATE PAYMENTS
        SET PAYMENT_STATUS = 'CANCELLED',
            ERROR_MESSAGE = p_reason,
            UPDATED_DATE = SYSDATE,
            UPDATED_BY = p_updated_by
        WHERE PAYMENT_ID = p_payment_id;

        -- Create void transaction record
        SELECT SEQ_TRANSACTION_ID.NEXTVAL INTO v_transaction_id FROM DUAL;

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
            v_transaction_id,
            p_payment_id,
            'VOID',
            v_payment_amount,
            'CANCELLED',
            '99',
            p_reason,
            SYSDATE,
            SYSDATE,
            p_updated_by
        );

        COMMIT;

        p_status := 'SUCCESS';
        p_error_message := 'Payment cancelled successfully';

    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            p_status := 'ERROR';
            p_error_message := 'Error cancelling payment: ' || SQLERRM;
    END CANCEL_PAYMENT;

    -- =====================================================
    -- GET_PAYMENT_DETAILS: Retrieves payment details
    -- =====================================================
    PROCEDURE GET_PAYMENT_DETAILS (
        p_payment_id        IN NUMBER,
        p_result_cursor     OUT SYS_REFCURSOR,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    ) AS
    BEGIN
        OPEN p_result_cursor FOR
            SELECT
                p.PAYMENT_ID,
                p.CUSTOMER_ID,
                c.CUSTOMER_NAME,
                c.ACCOUNT_NUMBER,
                p.PAYMENT_METHOD_ID,
                pm.METHOD_TYPE,
                pm.CARD_NUMBER,
                p.PAYMENT_AMOUNT,
                p.CURRENCY_CODE,
                p.PAYMENT_STATUS,
                p.PAYMENT_TYPE,
                p.REFERENCE_NUMBER,
                p.MERCHANT_ID,
                p.DESCRIPTION,
                p.AUTHORIZATION_CODE,
                p.GATEWAY_RESPONSE,
                p.ERROR_MESSAGE,
                p.CREATED_DATE,
                p.PROCESSED_DATE,
                p.SETTLEMENT_DATE,
                p.CREATED_BY
            FROM PAYMENTS p
            INNER JOIN CUSTOMERS c ON p.CUSTOMER_ID = c.CUSTOMER_ID
            INNER JOIN PAYMENT_METHODS pm ON p.PAYMENT_METHOD_ID = pm.METHOD_ID
            WHERE p.PAYMENT_ID = p_payment_id;

        p_status := 'SUCCESS';
        p_error_message := 'Payment details retrieved successfully';

    EXCEPTION
        WHEN OTHERS THEN
            p_status := 'ERROR';
            p_error_message := 'Error retrieving payment details: ' || SQLERRM;
    END GET_PAYMENT_DETAILS;

    -- =====================================================
    -- GET_CUSTOMER_PAYMENTS: Retrieves payments by customer
    -- =====================================================
    PROCEDURE GET_CUSTOMER_PAYMENTS (
        p_customer_id       IN NUMBER,
        p_from_date         IN DATE,
        p_to_date           IN DATE,
        p_result_cursor     OUT SYS_REFCURSOR,
        p_status            OUT VARCHAR2,
        p_error_message     OUT VARCHAR2
    ) AS
    BEGIN
        OPEN p_result_cursor FOR
            SELECT
                p.PAYMENT_ID,
                p.REFERENCE_NUMBER,
                p.PAYMENT_AMOUNT,
                p.CURRENCY_CODE,
                p.PAYMENT_STATUS,
                p.PAYMENT_TYPE,
                pm.METHOD_TYPE,
                p.CREATED_DATE,
                p.SETTLEMENT_DATE,
                p.DESCRIPTION
            FROM PAYMENTS p
            INNER JOIN PAYMENT_METHODS pm ON p.PAYMENT_METHOD_ID = pm.METHOD_ID
            WHERE p.CUSTOMER_ID = p_customer_id
            AND p.CREATED_DATE BETWEEN NVL(p_from_date, p.CREATED_DATE)
                                   AND NVL(p_to_date, p.CREATED_DATE)
            ORDER BY p.CREATED_DATE DESC;

        p_status := 'SUCCESS';
        p_error_message := 'Customer payments retrieved successfully';

    EXCEPTION
        WHEN OTHERS THEN
            p_status := 'ERROR';
            p_error_message := 'Error retrieving customer payments: ' || SQLERRM;
    END GET_CUSTOMER_PAYMENTS;

END PKG_PAYMENT_PROCESSING;
/

-- Grant execute permissions (adjust as needed for your environment)
-- GRANT EXECUTE ON PKG_PAYMENT_PROCESSING TO your_app_user;
