using System;

namespace PaymentProcessingApp
{
    /// <summary>
    /// Payment data model representing payment information
    /// Maps to PAYMENTS table in Oracle database
    /// </summary>
    public class Payment
    {
        public long PaymentId { get; set; }
        public long CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string AccountNumber { get; set; }
        public long PaymentMethodId { get; set; }
        public string MethodType { get; set; }
        public decimal PaymentAmount { get; set; }
        public string CurrencyCode { get; set; }
        public string PaymentStatus { get; set; }
        public string PaymentType { get; set; }
        public string ReferenceNumber { get; set; }
        public string MerchantId { get; set; }
        public string Description { get; set; }
        public string AuthorizationCode { get; set; }
        public string GatewayResponse { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public DateTime? SettlementDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }

        public Payment()
        {
            CurrencyCode = "USD";
            PaymentStatus = "PENDING";
            CreatedDate = DateTime.Now;
        }

        public override string ToString()
        {
            return $"Payment #{PaymentId} - {ReferenceNumber} - {PaymentStatus} - ${PaymentAmount}";
        }
    }

    /// <summary>
    /// Transaction data model representing transaction information
    /// Maps to TRANSACTIONS table in Oracle database
    /// </summary>
    public class Transaction
    {
        public long TransactionId { get; set; }
        public long PaymentId { get; set; }
        public string ReferenceNumber { get; set; }
        public string TransactionType { get; set; }
        public decimal TransactionAmount { get; set; }
        public string TransactionStatus { get; set; }
        public string GatewayTxnId { get; set; }
        public string ProcessorCode { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string CreatedBy { get; set; }
        public string CustomerName { get; set; }
        public string AccountNumber { get; set; }

        public override string ToString()
        {
            return $"Transaction #{TransactionId} - {TransactionType} - {TransactionStatus} - ${TransactionAmount}";
        }
    }

    /// <summary>
    /// Customer data model
    /// Maps to CUSTOMERS table in Oracle database
    /// </summary>
    public class Customer
    {
        public long CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string AccountNumber { get; set; }
        public string AccountStatus { get; set; }
        public DateTime CreatedDate { get; set; }

        public override string ToString()
        {
            return $"{CustomerName} ({AccountNumber})";
        }
    }

    /// <summary>
    /// Payment Method data model
    /// Maps to PAYMENT_METHODS table in Oracle database
    /// </summary>
    public class PaymentMethod
    {
        public long MethodId { get; set; }
        public long CustomerId { get; set; }
        public string MethodType { get; set; }
        public string CardNumber { get; set; }
        public string CardType { get; set; }
        public string ExpiryDate { get; set; }
        public string BankAccountNum { get; set; }
        public string RoutingNumber { get; set; }
        public bool IsDefault { get; set; }
        public string Status { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(CardNumber))
            {
                return $"{MethodType} - {CardType} {CardNumber}";
            }
            else if (!string.IsNullOrEmpty(BankAccountNum))
            {
                return $"{MethodType} - {BankAccountNum}";
            }
            return MethodType;
        }
    }

    /// <summary>
    /// Reconciliation data model
    /// Maps to RECONCILIATION_LOG table in Oracle database
    /// </summary>
    public class ReconciliationLog
    {
        public long ReconciliationId { get; set; }
        public long PaymentId { get; set; }
        public string ReferenceNumber { get; set; }
        public long? TransactionId { get; set; }
        public DateTime ReconciliationDate { get; set; }
        public string ReconciliationType { get; set; }
        public decimal ExpectedAmount { get; set; }
        public decimal ActualAmount { get; set; }
        public decimal VarianceAmount { get; set; }
        public string MatchStatus { get; set; }
        public string GatewayFileName { get; set; }
        public string ReconciledBy { get; set; }
        public string Notes { get; set; }
        public string CustomerName { get; set; }
        public string AccountNumber { get; set; }

        public override string ToString()
        {
            return $"Recon #{ReconciliationId} - {MatchStatus} - Variance: ${VarianceAmount}";
        }
    }

    /// <summary>
    /// Result object for stored procedure execution
    /// </summary>
    public class StoredProcedureResult
    {
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public object OutputValue { get; set; }

        public bool IsSuccess => Status == "SUCCESS";

        public StoredProcedureResult()
        {
            Status = "PENDING";
        }
    }
}
