using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace PaymentProcessingApp
{
    /// <summary>
    /// Database helper class for Oracle stored procedure calls
    /// Handles all database operations for Payment Processing System
    /// </summary>
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["OracleConnection"].ConnectionString;
        }

        public DatabaseHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region Payment Processing Package - PKG_PAYMENT_PROCESSING

        /// <summary>
        /// Calls PKG_PAYMENT_PROCESSING.CREATE_PAYMENT stored procedure
        /// </summary>
        public StoredProcedureResult CreatePayment(long customerId, long paymentMethodId,
            decimal paymentAmount, string currencyCode, string paymentType,
            string merchantId, string description, string createdBy,
            out long paymentId, out string referenceNumber)
        {
            var result = new StoredProcedureResult();
            paymentId = 0;
            referenceNumber = string.Empty;

            using (var connection = new OracleConnection(_connectionString))
            {
                using (var command = new OracleCommand("PKG_PAYMENT_PROCESSING.CREATE_PAYMENT", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Input parameters
                    command.Parameters.Add("p_customer_id", OracleDbType.Decimal).Value = customerId;
                    command.Parameters.Add("p_payment_method_id", OracleDbType.Decimal).Value = paymentMethodId;
                    command.Parameters.Add("p_payment_amount", OracleDbType.Decimal).Value = paymentAmount;
                    command.Parameters.Add("p_currency_code", OracleDbType.Varchar2).Value = currencyCode ?? "USD";
                    command.Parameters.Add("p_payment_type", OracleDbType.Varchar2).Value = paymentType;
                    command.Parameters.Add("p_merchant_id", OracleDbType.Varchar2).Value = merchantId ?? (object)DBNull.Value;
                    command.Parameters.Add("p_description", OracleDbType.Varchar2).Value = description ?? (object)DBNull.Value;
                    command.Parameters.Add("p_created_by", OracleDbType.Varchar2).Value = createdBy;

                    // Output parameters
                    command.Parameters.Add("p_payment_id", OracleDbType.Decimal, ParameterDirection.Output);
                    command.Parameters.Add("p_reference_number", OracleDbType.Varchar2, 100, null, ParameterDirection.Output);
                    command.Parameters.Add("p_status", OracleDbType.Varchar2, 50, null, ParameterDirection.Output);
                    command.Parameters.Add("p_error_message", OracleDbType.Varchar2, 500, null, ParameterDirection.Output);

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();

                        result.Status = command.Parameters["p_status"].Value.ToString();
                        result.ErrorMessage = command.Parameters["p_error_message"].Value.ToString();

                        if (result.IsSuccess)
                        {
                            paymentId = Convert.ToInt64(command.Parameters["p_payment_id"].Value.ToString());
                            referenceNumber = command.Parameters["p_reference_number"].Value.ToString();
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Status = "ERROR";
                        result.ErrorMessage = $"Database error: {ex.Message}";
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Calls PKG_PAYMENT_PROCESSING.AUTHORIZE_PAYMENT stored procedure
        /// </summary>
        public StoredProcedureResult AuthorizePayment(long paymentId, string authorizationCode,
            string gatewayResponse, string updatedBy)
        {
            var result = new StoredProcedureResult();

            using (var connection = new OracleConnection(_connectionString))
            {
                using (var command = new OracleCommand("PKG_PAYMENT_PROCESSING.AUTHORIZE_PAYMENT", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add("p_payment_id", OracleDbType.Decimal).Value = paymentId;
                    command.Parameters.Add("p_authorization_code", OracleDbType.Varchar2).Value = authorizationCode;
                    command.Parameters.Add("p_gateway_response", OracleDbType.Varchar2).Value = gatewayResponse;
                    command.Parameters.Add("p_updated_by", OracleDbType.Varchar2).Value = updatedBy;
                    command.Parameters.Add("p_status", OracleDbType.Varchar2, 50, null, ParameterDirection.Output);
                    command.Parameters.Add("p_error_message", OracleDbType.Varchar2, 500, null, ParameterDirection.Output);

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();

                        result.Status = command.Parameters["p_status"].Value.ToString();
                        result.ErrorMessage = command.Parameters["p_error_message"].Value.ToString();
                    }
                    catch (Exception ex)
                    {
                        result.Status = "ERROR";
                        result.ErrorMessage = $"Database error: {ex.Message}";
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Calls PKG_PAYMENT_PROCESSING.SETTLE_PAYMENT stored procedure
        /// </summary>
        public StoredProcedureResult SettlePayment(long paymentId, decimal settlementAmount, string updatedBy)
        {
            var result = new StoredProcedureResult();

            using (var connection = new OracleConnection(_connectionString))
            {
                using (var command = new OracleCommand("PKG_PAYMENT_PROCESSING.SETTLE_PAYMENT", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add("p_payment_id", OracleDbType.Decimal).Value = paymentId;
                    command.Parameters.Add("p_settlement_amount", OracleDbType.Decimal).Value = settlementAmount;
                    command.Parameters.Add("p_updated_by", OracleDbType.Varchar2).Value = updatedBy;
                    command.Parameters.Add("p_status", OracleDbType.Varchar2, 50, null, ParameterDirection.Output);
                    command.Parameters.Add("p_error_message", OracleDbType.Varchar2, 500, null, ParameterDirection.Output);

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();

                        result.Status = command.Parameters["p_status"].Value.ToString();
                        result.ErrorMessage = command.Parameters["p_error_message"].Value.ToString();
                    }
                    catch (Exception ex)
                    {
                        result.Status = "ERROR";
                        result.ErrorMessage = $"Database error: {ex.Message}";
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Calls PKG_PAYMENT_PROCESSING.CANCEL_PAYMENT stored procedure
        /// </summary>
        public StoredProcedureResult CancelPayment(long paymentId, string reason, string updatedBy)
        {
            var result = new StoredProcedureResult();

            using (var connection = new OracleConnection(_connectionString))
            {
                using (var command = new OracleCommand("PKG_PAYMENT_PROCESSING.CANCEL_PAYMENT", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add("p_payment_id", OracleDbType.Decimal).Value = paymentId;
                    command.Parameters.Add("p_reason", OracleDbType.Varchar2).Value = reason;
                    command.Parameters.Add("p_updated_by", OracleDbType.Varchar2).Value = updatedBy;
                    command.Parameters.Add("p_status", OracleDbType.Varchar2, 50, null, ParameterDirection.Output);
                    command.Parameters.Add("p_error_message", OracleDbType.Varchar2, 500, null, ParameterDirection.Output);

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();

                        result.Status = command.Parameters["p_status"].Value.ToString();
                        result.ErrorMessage = command.Parameters["p_error_message"].Value.ToString();
                    }
                    catch (Exception ex)
                    {
                        result.Status = "ERROR";
                        result.ErrorMessage = $"Database error: {ex.Message}";
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Calls PKG_PAYMENT_PROCESSING.GET_PAYMENT_DETAILS stored procedure
        /// </summary>
        public List<Payment> GetPaymentDetails(long paymentId, out StoredProcedureResult result)
        {
            result = new StoredProcedureResult();
            var payments = new List<Payment>();

            using (var connection = new OracleConnection(_connectionString))
            {
                using (var command = new OracleCommand("PKG_PAYMENT_PROCESSING.GET_PAYMENT_DETAILS", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add("p_payment_id", OracleDbType.Decimal).Value = paymentId;
                    command.Parameters.Add("p_result_cursor", OracleDbType.RefCursor, ParameterDirection.Output);
                    command.Parameters.Add("p_status", OracleDbType.Varchar2, 50, null, ParameterDirection.Output);
                    command.Parameters.Add("p_error_message", OracleDbType.Varchar2, 500, null, ParameterDirection.Output);

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();

                        result.Status = command.Parameters["p_status"].Value.ToString();
                        result.ErrorMessage = command.Parameters["p_error_message"].Value.ToString();

                        if (result.IsSuccess)
                        {
                            using (var reader = ((OracleRefCursor)command.Parameters["p_result_cursor"].Value).GetDataReader())
                            {
                                while (reader.Read())
                                {
                                    payments.Add(MapPaymentFromReader(reader));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Status = "ERROR";
                        result.ErrorMessage = $"Database error: {ex.Message}";
                    }
                }
            }

            return payments;
        }

        #endregion

        #region Transaction Management Package - PKG_TRANSACTION_MANAGEMENT

        /// <summary>
        /// Calls PKG_TRANSACTION_MANAGEMENT.GET_PAYMENT_TRANSACTIONS stored procedure
        /// </summary>
        public List<Transaction> GetPaymentTransactions(long paymentId, out StoredProcedureResult result)
        {
            result = new StoredProcedureResult();
            var transactions = new List<Transaction>();

            using (var connection = new OracleConnection(_connectionString))
            {
                using (var command = new OracleCommand("PKG_TRANSACTION_MANAGEMENT.GET_PAYMENT_TRANSACTIONS", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add("p_payment_id", OracleDbType.Decimal).Value = paymentId;
                    command.Parameters.Add("p_result_cursor", OracleDbType.RefCursor, ParameterDirection.Output);
                    command.Parameters.Add("p_status", OracleDbType.Varchar2, 50, null, ParameterDirection.Output);
                    command.Parameters.Add("p_error_message", OracleDbType.Varchar2, 500, null, ParameterDirection.Output);

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();

                        result.Status = command.Parameters["p_status"].Value.ToString();
                        result.ErrorMessage = command.Parameters["p_error_message"].Value.ToString();

                        if (result.IsSuccess)
                        {
                            using (var reader = ((OracleRefCursor)command.Parameters["p_result_cursor"].Value).GetDataReader())
                            {
                                while (reader.Read())
                                {
                                    transactions.Add(MapTransactionFromReader(reader));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Status = "ERROR";
                        result.ErrorMessage = $"Database error: {ex.Message}";
                    }
                }
            }

            return transactions;
        }

        /// <summary>
        /// Calls PKG_TRANSACTION_MANAGEMENT.SEARCH_TRANSACTIONS stored procedure
        /// </summary>
        public List<Transaction> SearchTransactions(DateTime? fromDate, DateTime? toDate,
            string transactionType, string transactionStatus, decimal? minAmount, decimal? maxAmount,
            out StoredProcedureResult result)
        {
            result = new StoredProcedureResult();
            var transactions = new List<Transaction>();

            using (var connection = new OracleConnection(_connectionString))
            {
                using (var command = new OracleCommand("PKG_TRANSACTION_MANAGEMENT.SEARCH_TRANSACTIONS", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add("p_from_date", OracleDbType.Date).Value = fromDate.HasValue ? (object)fromDate.Value : DBNull.Value;
                    command.Parameters.Add("p_to_date", OracleDbType.Date).Value = toDate.HasValue ? (object)toDate.Value : DBNull.Value;
                    command.Parameters.Add("p_transaction_type", OracleDbType.Varchar2).Value = transactionType ?? (object)DBNull.Value;
                    command.Parameters.Add("p_transaction_status", OracleDbType.Varchar2).Value = transactionStatus ?? (object)DBNull.Value;
                    command.Parameters.Add("p_min_amount", OracleDbType.Decimal).Value = minAmount.HasValue ? (object)minAmount.Value : DBNull.Value;
                    command.Parameters.Add("p_max_amount", OracleDbType.Decimal).Value = maxAmount.HasValue ? (object)maxAmount.Value : DBNull.Value;
                    command.Parameters.Add("p_result_cursor", OracleDbType.RefCursor, ParameterDirection.Output);
                    command.Parameters.Add("p_status", OracleDbType.Varchar2, 50, null, ParameterDirection.Output);
                    command.Parameters.Add("p_error_message", OracleDbType.Varchar2, 500, null, ParameterDirection.Output);

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();

                        result.Status = command.Parameters["p_status"].Value.ToString();
                        result.ErrorMessage = command.Parameters["p_error_message"].Value.ToString();

                        if (result.IsSuccess)
                        {
                            using (var reader = ((OracleRefCursor)command.Parameters["p_result_cursor"].Value).GetDataReader())
                            {
                                while (reader.Read())
                                {
                                    transactions.Add(MapSearchTransactionFromReader(reader));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Status = "ERROR";
                        result.ErrorMessage = $"Database error: {ex.Message}";
                    }
                }
            }

            return transactions;
        }

        /// <summary>
        /// Calls PKG_TRANSACTION_MANAGEMENT.CREATE_REFUND_TRANSACTION stored procedure
        /// </summary>
        public StoredProcedureResult CreateRefundTransaction(long paymentId, decimal refundAmount,
            string reason, string createdBy, out long transactionId)
        {
            var result = new StoredProcedureResult();
            transactionId = 0;

            using (var connection = new OracleConnection(_connectionString))
            {
                using (var command = new OracleCommand("PKG_TRANSACTION_MANAGEMENT.CREATE_REFUND_TRANSACTION", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add("p_payment_id", OracleDbType.Decimal).Value = paymentId;
                    command.Parameters.Add("p_refund_amount", OracleDbType.Decimal).Value = refundAmount;
                    command.Parameters.Add("p_reason", OracleDbType.Varchar2).Value = reason;
                    command.Parameters.Add("p_created_by", OracleDbType.Varchar2).Value = createdBy;
                    command.Parameters.Add("p_transaction_id", OracleDbType.Decimal, ParameterDirection.Output);
                    command.Parameters.Add("p_status", OracleDbType.Varchar2, 50, null, ParameterDirection.Output);
                    command.Parameters.Add("p_error_message", OracleDbType.Varchar2, 500, null, ParameterDirection.Output);

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();

                        result.Status = command.Parameters["p_status"].Value.ToString();
                        result.ErrorMessage = command.Parameters["p_error_message"].Value.ToString();

                        if (result.IsSuccess)
                        {
                            transactionId = Convert.ToInt64(command.Parameters["p_transaction_id"].Value.ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Status = "ERROR";
                        result.ErrorMessage = $"Database error: {ex.Message}";
                    }
                }
            }

            return result;
        }

        #endregion

        #region Reconciliation Package - PKG_RECONCILIATION

        /// <summary>
        /// Calls PKG_RECONCILIATION.CREATE_RECONCILIATION_RECORD stored procedure
        /// </summary>
        public StoredProcedureResult CreateReconciliationRecord(long paymentId, long? transactionId,
            string reconType, decimal expectedAmount, decimal actualAmount,
            string gatewayFile, string reconciledBy, out long reconciliationId)
        {
            var result = new StoredProcedureResult();
            reconciliationId = 0;

            using (var connection = new OracleConnection(_connectionString))
            {
                using (var command = new OracleCommand("PKG_RECONCILIATION.CREATE_RECONCILIATION_RECORD", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add("p_payment_id", OracleDbType.Decimal).Value = paymentId;
                    command.Parameters.Add("p_transaction_id", OracleDbType.Decimal).Value = transactionId.HasValue ? (object)transactionId.Value : DBNull.Value;
                    command.Parameters.Add("p_recon_type", OracleDbType.Varchar2).Value = reconType;
                    command.Parameters.Add("p_expected_amount", OracleDbType.Decimal).Value = expectedAmount;
                    command.Parameters.Add("p_actual_amount", OracleDbType.Decimal).Value = actualAmount;
                    command.Parameters.Add("p_gateway_file", OracleDbType.Varchar2).Value = gatewayFile ?? (object)DBNull.Value;
                    command.Parameters.Add("p_reconciled_by", OracleDbType.Varchar2).Value = reconciledBy;
                    command.Parameters.Add("p_reconciliation_id", OracleDbType.Decimal, ParameterDirection.Output);
                    command.Parameters.Add("p_status", OracleDbType.Varchar2, 50, null, ParameterDirection.Output);
                    command.Parameters.Add("p_error_message", OracleDbType.Varchar2, 500, null, ParameterDirection.Output);

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();

                        result.Status = command.Parameters["p_status"].Value.ToString();
                        result.ErrorMessage = command.Parameters["p_error_message"].Value.ToString();

                        if (result.IsSuccess)
                        {
                            reconciliationId = Convert.ToInt64(command.Parameters["p_reconciliation_id"].Value.ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Status = "ERROR";
                        result.ErrorMessage = $"Database error: {ex.Message}";
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Calls PKG_RECONCILIATION.PERFORM_DAILY_RECONCILIATION stored procedure
        /// </summary>
        public StoredProcedureResult PerformDailyReconciliation(DateTime reconciliationDate,
            string gatewayFile, string reconciledBy, out int recordsMatched, out int recordsMismatched)
        {
            var result = new StoredProcedureResult();
            recordsMatched = 0;
            recordsMismatched = 0;

            using (var connection = new OracleConnection(_connectionString))
            {
                using (var command = new OracleCommand("PKG_RECONCILIATION.PERFORM_DAILY_RECONCILIATION", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add("p_reconciliation_date", OracleDbType.Date).Value = reconciliationDate;
                    command.Parameters.Add("p_gateway_file", OracleDbType.Varchar2).Value = gatewayFile ?? (object)DBNull.Value;
                    command.Parameters.Add("p_reconciled_by", OracleDbType.Varchar2).Value = reconciledBy;
                    command.Parameters.Add("p_records_matched", OracleDbType.Decimal, ParameterDirection.Output);
                    command.Parameters.Add("p_records_mismatched", OracleDbType.Decimal, ParameterDirection.Output);
                    command.Parameters.Add("p_status", OracleDbType.Varchar2, 50, null, ParameterDirection.Output);
                    command.Parameters.Add("p_error_message", OracleDbType.Varchar2, 500, null, ParameterDirection.Output);

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();

                        result.Status = command.Parameters["p_status"].Value.ToString();
                        result.ErrorMessage = command.Parameters["p_error_message"].Value.ToString();

                        if (result.IsSuccess)
                        {
                            recordsMatched = Convert.ToInt32(command.Parameters["p_records_matched"].Value.ToString());
                            recordsMismatched = Convert.ToInt32(command.Parameters["p_records_mismatched"].Value.ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Status = "ERROR";
                        result.ErrorMessage = $"Database error: {ex.Message}";
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Calls PKG_RECONCILIATION.GET_RECONCILIATION_REPORT stored procedure
        /// </summary>
        public List<ReconciliationLog> GetReconciliationReport(DateTime? fromDate, DateTime? toDate,
            string matchStatus, out StoredProcedureResult result)
        {
            result = new StoredProcedureResult();
            var reconLogs = new List<ReconciliationLog>();

            using (var connection = new OracleConnection(_connectionString))
            {
                using (var command = new OracleCommand("PKG_RECONCILIATION.GET_RECONCILIATION_REPORT", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add("p_from_date", OracleDbType.Date).Value = fromDate.HasValue ? (object)fromDate.Value : DBNull.Value;
                    command.Parameters.Add("p_to_date", OracleDbType.Date).Value = toDate.HasValue ? (object)toDate.Value : DBNull.Value;
                    command.Parameters.Add("p_match_status", OracleDbType.Varchar2).Value = matchStatus ?? (object)DBNull.Value;
                    command.Parameters.Add("p_result_cursor", OracleDbType.RefCursor, ParameterDirection.Output);
                    command.Parameters.Add("p_status", OracleDbType.Varchar2, 50, null, ParameterDirection.Output);
                    command.Parameters.Add("p_error_message", OracleDbType.Varchar2, 500, null, ParameterDirection.Output);

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();

                        result.Status = command.Parameters["p_status"].Value.ToString();
                        result.ErrorMessage = command.Parameters["p_error_message"].Value.ToString();

                        if (result.IsSuccess)
                        {
                            using (var reader = ((OracleRefCursor)command.Parameters["p_result_cursor"].Value).GetDataReader())
                            {
                                while (reader.Read())
                                {
                                    reconLogs.Add(MapReconciliationFromReader(reader));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Status = "ERROR";
                        result.ErrorMessage = $"Database error: {ex.Message}";
                    }
                }
            }

            return reconLogs;
        }

        #endregion

        #region Helper Methods

        private Payment MapPaymentFromReader(IDataReader reader)
        {
            return new Payment
            {
                PaymentId = reader.GetInt64(reader.GetOrdinal("PAYMENT_ID")),
                CustomerId = reader.GetInt64(reader.GetOrdinal("CUSTOMER_ID")),
                CustomerName = reader.GetString(reader.GetOrdinal("CUSTOMER_NAME")),
                AccountNumber = reader.GetString(reader.GetOrdinal("ACCOUNT_NUMBER")),
                PaymentMethodId = reader.GetInt64(reader.GetOrdinal("PAYMENT_METHOD_ID")),
                MethodType = reader.GetString(reader.GetOrdinal("METHOD_TYPE")),
                PaymentAmount = reader.GetDecimal(reader.GetOrdinal("PAYMENT_AMOUNT")),
                CurrencyCode = reader.GetString(reader.GetOrdinal("CURRENCY_CODE")),
                PaymentStatus = reader.GetString(reader.GetOrdinal("PAYMENT_STATUS")),
                PaymentType = reader.GetString(reader.GetOrdinal("PAYMENT_TYPE")),
                ReferenceNumber = reader.GetString(reader.GetOrdinal("REFERENCE_NUMBER")),
                MerchantId = reader.IsDBNull(reader.GetOrdinal("MERCHANT_ID")) ? null : reader.GetString(reader.GetOrdinal("MERCHANT_ID")),
                Description = reader.IsDBNull(reader.GetOrdinal("DESCRIPTION")) ? null : reader.GetString(reader.GetOrdinal("DESCRIPTION")),
                AuthorizationCode = reader.IsDBNull(reader.GetOrdinal("AUTHORIZATION_CODE")) ? null : reader.GetString(reader.GetOrdinal("AUTHORIZATION_CODE")),
                GatewayResponse = reader.IsDBNull(reader.GetOrdinal("GATEWAY_RESPONSE")) ? null : reader.GetString(reader.GetOrdinal("GATEWAY_RESPONSE")),
                CreatedDate = reader.GetDateTime(reader.GetOrdinal("CREATED_DATE")),
                ProcessedDate = reader.IsDBNull(reader.GetOrdinal("PROCESSED_DATE")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("PROCESSED_DATE")),
                SettlementDate = reader.IsDBNull(reader.GetOrdinal("SETTLEMENT_DATE")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("SETTLEMENT_DATE")),
                CreatedBy = reader.GetString(reader.GetOrdinal("CREATED_BY"))
            };
        }

        private Transaction MapTransactionFromReader(IDataReader reader)
        {
            return new Transaction
            {
                TransactionId = reader.GetInt64(reader.GetOrdinal("TRANSACTION_ID")),
                TransactionType = reader.GetString(reader.GetOrdinal("TRANSACTION_TYPE")),
                TransactionAmount = reader.GetDecimal(reader.GetOrdinal("TRANSACTION_AMOUNT")),
                TransactionStatus = reader.GetString(reader.GetOrdinal("TRANSACTION_STATUS")),
                GatewayTxnId = reader.IsDBNull(reader.GetOrdinal("GATEWAY_TXN_ID")) ? null : reader.GetString(reader.GetOrdinal("GATEWAY_TXN_ID")),
                ResponseCode = reader.IsDBNull(reader.GetOrdinal("RESPONSE_CODE")) ? null : reader.GetString(reader.GetOrdinal("RESPONSE_CODE")),
                ResponseMessage = reader.IsDBNull(reader.GetOrdinal("RESPONSE_MESSAGE")) ? null : reader.GetString(reader.GetOrdinal("RESPONSE_MESSAGE")),
                TransactionDate = reader.GetDateTime(reader.GetOrdinal("TRANSACTION_DATE")),
                ProcessedDate = reader.IsDBNull(reader.GetOrdinal("PROCESSED_DATE")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("PROCESSED_DATE")),
                CreatedBy = reader.GetString(reader.GetOrdinal("CREATED_BY"))
            };
        }

        private Transaction MapSearchTransactionFromReader(IDataReader reader)
        {
            return new Transaction
            {
                TransactionId = reader.GetInt64(reader.GetOrdinal("TRANSACTION_ID")),
                PaymentId = reader.GetInt64(reader.GetOrdinal("PAYMENT_ID")),
                ReferenceNumber = reader.GetString(reader.GetOrdinal("REFERENCE_NUMBER")),
                TransactionType = reader.GetString(reader.GetOrdinal("TRANSACTION_TYPE")),
                TransactionAmount = reader.GetDecimal(reader.GetOrdinal("TRANSACTION_AMOUNT")),
                TransactionStatus = reader.GetString(reader.GetOrdinal("TRANSACTION_STATUS")),
                GatewayTxnId = reader.IsDBNull(reader.GetOrdinal("GATEWAY_TXN_ID")) ? null : reader.GetString(reader.GetOrdinal("GATEWAY_TXN_ID")),
                ResponseCode = reader.IsDBNull(reader.GetOrdinal("RESPONSE_CODE")) ? null : reader.GetString(reader.GetOrdinal("RESPONSE_CODE")),
                ResponseMessage = reader.IsDBNull(reader.GetOrdinal("RESPONSE_MESSAGE")) ? null : reader.GetString(reader.GetOrdinal("RESPONSE_MESSAGE")),
                TransactionDate = reader.GetDateTime(reader.GetOrdinal("TRANSACTION_DATE")),
                CustomerName = reader.GetString(reader.GetOrdinal("CUSTOMER_NAME")),
                AccountNumber = reader.GetString(reader.GetOrdinal("ACCOUNT_NUMBER"))
            };
        }

        private ReconciliationLog MapReconciliationFromReader(IDataReader reader)
        {
            return new ReconciliationLog
            {
                ReconciliationId = reader.GetInt64(reader.GetOrdinal("RECONCILIATION_ID")),
                PaymentId = reader.GetInt64(reader.GetOrdinal("PAYMENT_ID")),
                ReferenceNumber = reader.GetString(reader.GetOrdinal("REFERENCE_NUMBER")),
                TransactionId = reader.IsDBNull(reader.GetOrdinal("TRANSACTION_ID")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("TRANSACTION_ID")),
                ReconciliationDate = reader.GetDateTime(reader.GetOrdinal("RECONCILIATION_DATE")),
                ReconciliationType = reader.GetString(reader.GetOrdinal("RECONCILIATION_TYPE")),
                ExpectedAmount = reader.GetDecimal(reader.GetOrdinal("EXPECTED_AMOUNT")),
                ActualAmount = reader.GetDecimal(reader.GetOrdinal("ACTUAL_AMOUNT")),
                VarianceAmount = reader.GetDecimal(reader.GetOrdinal("VARIANCE_AMOUNT")),
                MatchStatus = reader.GetString(reader.GetOrdinal("MATCH_STATUS")),
                GatewayFileName = reader.IsDBNull(reader.GetOrdinal("GATEWAY_FILE_NAME")) ? null : reader.GetString(reader.GetOrdinal("GATEWAY_FILE_NAME")),
                ReconciledBy = reader.GetString(reader.GetOrdinal("RECONCILED_BY")),
                Notes = reader.IsDBNull(reader.GetOrdinal("NOTES")) ? null : reader.GetString(reader.GetOrdinal("NOTES")),
                CustomerName = reader.GetString(reader.GetOrdinal("CUSTOMER_NAME")),
                AccountNumber = reader.GetString(reader.GetOrdinal("ACCOUNT_NUMBER"))
            };
        }

        #endregion
    }
}
