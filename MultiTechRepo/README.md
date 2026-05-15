# Payment Processing System - Enterprise Demo

A comprehensive demonstration of multi-technology integration in a financial/payments domain, showcasing linkages between C# WinForms, Oracle PL/SQL, and Python.

## 📋 Overview

This enterprise demonstration codebase illustrates how different technologies work together in a large-scale payment processing system:

- **C# WinForms Application**: User interface for payment processing operations
- **Oracle PL/SQL Packages**: Business logic and data management layer
- **Python Scripts**: Data migration, external integration, and batch processing

### Business Domain: Payment Processing

The system handles:
- Payment creation and authorization
- Payment settlement and reconciliation
- Transaction management and refunds
- External gateway integration
- Data migration and quality management

---

## 🏗️ Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    C# WinForms UI (.NET 4.7.1)             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │Create Payment│  │Manage Payment│  │Reconciliation│     │
│  └──────────────┘  └──────────────┘  └──────────────┘     │
└─────────────────────┬───────────────────────────────────────┘
                      │ DatabaseHelper.cs
                      │ (Oracle.ManagedDataAccess)
                      ▼
┌─────────────────────────────────────────────────────────────┐
│              Oracle Database (PL/SQL Packages)              │
│  ┌───────────────────┐  ┌──────────────────┐  ┌──────────┐ │
│  │PKG_PAYMENT_       │  │PKG_TRANSACTION_  │  │PKG_      │ │
│  │PROCESSING         │  │MANAGEMENT        │  │RECON-    │ │
│  │                   │  │                  │  │CILIATION │ │
│  │• CREATE_PAYMENT   │  │• GET_PAYMENT_    │  │• PERFORM_│ │
│  │• AUTHORIZE_PAYMENT│  │  TRANSACTIONS    │  │  DAILY_  │ │
│  │• SETTLE_PAYMENT   │  │• SEARCH_         │  │  RECON   │ │
│  │• CANCEL_PAYMENT   │  │  TRANSACTIONS    │  │• GET_    │ │
│  │• GET_PAYMENT_     │  │• CREATE_REFUND_  │  │  REPORT  │ │
│  │  DETAILS          │  │  TRANSACTION     │  │          │ │
│  └───────────────────┘  └──────────────────┘  └──────────┘ │
│                                                               │
│  ┌──────────┐  ┌────────────┐  ┌────────────┐  ┌─────────┐ │
│  │CUSTOMERS │  │PAYMENTS    │  │TRANSACTIONS│  │RECON_LOG│ │
│  └──────────┘  └────────────┘  └────────────┘  └─────────┘ │
└─────────────────────┬───────────────────────────────────────┘
                      │ cx_Oracle (Python)
                      ▼
┌─────────────────────────────────────────────────────────────┐
│                    Python Scripts                           │
│  ┌──────────────────┐  ┌───────────────────┐  ┌──────────┐ │
│  │payment_data_     │  │external_payment_  │  │trans-    │ │
│  │loader.py         │  │gateway.py         │  │action_   │ │
│  │                  │  │                   │  │migration │ │
│  │• Load customers  │  │• Authorize        │  │.py       │ │
│  │• Load payment    │  │  payments         │  │          │ │
│  │  methods         │  │• Settle payments  │  │• Migrate │ │
│  │• Bulk load       │  │• Generate         │  │  legacy  │ │
│  │  payments        │  │  settlement files │  │• Fix data│ │
│  └──────────────────┘  └───────────────────┘  └──────────┘ │
└─────────────────────────────────────────────────────────────┘
```

---

## 💻 Technology Stack

| Component | Technology | Version | Purpose |
|-----------|-----------|---------|---------|
| **Frontend** | C# WinForms | .NET Framework 4.7.1 | User interface |
| **Data Access** | Oracle.ManagedDataAccess | 4.122.19.1+ | Oracle connectivity |
| **Database** | Oracle Database | 11g+ | Data storage & business logic |
| **PL/SQL** | Oracle PL/SQL | - | Stored procedures & packages |
| **Scripting** | Python | 3.x | Data migration & integration |
| **Python Oracle** | cx_Oracle | Latest | Python-Oracle connectivity |

---

## 📁 Directory Structure

```
MultiTechRepo/
│
├── Database/
│   ├── DDL_Tables.sql                    # Database schema & tables
│   ├── PKG_PAYMENT_PROCESSING.sql        # Payment processing package
│   ├── PKG_TRANSACTION_MANAGEMENT.sql    # Transaction management package
│   └── PKG_RECONCILIATION.sql            # Reconciliation package
│
├── CSharp/
│   ├── Program.cs                        # Application entry point
│   ├── MainForm.cs                       # WinForms UI
│   ├── Payment.cs                        # Data models
│   ├── DatabaseHelper.cs                 # Oracle database access layer
│   └── App.config                        # Configuration & connection strings
│
├── Python/
│   ├── payment_data_loader.py           # Data migration script
│   ├── external_payment_gateway.py      # Gateway integration simulator
│   └── transaction_migration.py         # Transaction migration utility
│
└── README.md                             # This file
```

---

## 🗄️ Database Components

### Tables

| Table | Purpose | Key Fields |
|-------|---------|------------|
| **CUSTOMERS** | Customer account information | CUSTOMER_ID, CUSTOMER_NAME, ACCOUNT_NUMBER |
| **PAYMENT_METHODS** | Payment methods (cards, bank accounts) | METHOD_ID, CUSTOMER_ID, METHOD_TYPE |
| **PAYMENTS** | Main payment records | PAYMENT_ID, PAYMENT_AMOUNT, PAYMENT_STATUS, REFERENCE_NUMBER |
| **TRANSACTIONS** | Individual transaction records | TRANSACTION_ID, PAYMENT_ID, TRANSACTION_TYPE, GATEWAY_TXN_ID |
| **RECONCILIATION_LOG** | Reconciliation tracking | RECONCILIATION_ID, EXPECTED_AMOUNT, ACTUAL_AMOUNT, MATCH_STATUS |

### PL/SQL Packages

#### PKG_PAYMENT_PROCESSING
Core payment processing operations
- `CREATE_PAYMENT` - Creates new payment record
- `AUTHORIZE_PAYMENT` - Authorizes a payment
- `SETTLE_PAYMENT` - Settles an authorized payment
- `CANCEL_PAYMENT` - Cancels a payment
- `GET_PAYMENT_DETAILS` - Retrieves payment information
- `GET_CUSTOMER_PAYMENTS` - Lists customer payment history

#### PKG_TRANSACTION_MANAGEMENT
Transaction queries and management
- `GET_TRANSACTION_DETAILS` - Gets transaction by ID
- `GET_PAYMENT_TRANSACTIONS` - Lists all transactions for a payment
- `SEARCH_TRANSACTIONS` - Advanced transaction search
- `UPDATE_TRANSACTION_STATUS` - Updates transaction status
- `CREATE_REFUND_TRANSACTION` - Processes refunds
- `GET_TRANSACTION_SUMMARY` - Statistical summaries

#### PKG_RECONCILIATION
Payment reconciliation and matching
- `CREATE_RECONCILIATION_RECORD` - Creates reconciliation entry
- `PERFORM_DAILY_RECONCILIATION` - Batch daily reconciliation
- `GET_RECONCILIATION_REPORT` - Retrieves reconciliation reports
- `UPDATE_RECONCILIATION_STATUS` - Updates reconciliation status
- `GET_UNRECONCILED_TRANSACTIONS` - Finds unreconciled items
- `GET_RECONCILIATION_SUMMARY` - Summary statistics

---

## 🖥️ C# Application

### Files (5 total)

#### 1. Program.cs
Entry point for the WinForms application

#### 2. Payment.cs
Data models: Payment, Transaction, Customer, PaymentMethod, ReconciliationLog, StoredProcedureResult

#### 3. DatabaseHelper.cs
Oracle database access layer with methods that call PL/SQL procedures

#### 4. MainForm.cs
WinForms UI with three tabs:
- **Create Payment Tab**: Form to create new payments
- **Manage Payment Tab**: View/manage payments, authorize, settle, cancel, refund
- **Reconciliation Tab**: Daily reconciliation and reporting

#### 5. App.config
Configuration file with Oracle connection strings and application settings

### C# to PL/SQL Linkages

| C# Method | PL/SQL Procedure | Package |
|-----------|------------------|---------|
| `CreatePayment()` | `CREATE_PAYMENT` | PKG_PAYMENT_PROCESSING |
| `AuthorizePayment()` | `AUTHORIZE_PAYMENT` | PKG_PAYMENT_PROCESSING |
| `SettlePayment()` | `SETTLE_PAYMENT` | PKG_PAYMENT_PROCESSING |
| `CancelPayment()` | `CANCEL_PAYMENT` | PKG_PAYMENT_PROCESSING |
| `GetPaymentTransactions()` | `GET_PAYMENT_TRANSACTIONS` | PKG_TRANSACTION_MANAGEMENT |
| `CreateRefundTransaction()` | `CREATE_REFUND_TRANSACTION` | PKG_TRANSACTION_MANAGEMENT |
| `PerformDailyReconciliation()` | `PERFORM_DAILY_RECONCILIATION` | PKG_RECONCILIATION |
| `GetReconciliationReport()` | `GET_RECONCILIATION_REPORT` | PKG_RECONCILIATION |

---

## 🐍 Python Scripts

### Files (3 total)

#### 1. payment_data_loader.py
**Purpose**: Data migration and bulk loading

**Key Functions**:
- `load_customers_from_csv()` - Load customers from CSV files
- `load_payment_methods_from_json()` - Load payment methods from JSON
- `bulk_load_payments()` - Bulk payment insertion
- `generate_sample_data()` - Create test data

**Tables Updated**: CUSTOMERS, PAYMENT_METHODS, PAYMENTS

#### 2. external_payment_gateway.py
**Purpose**: Simulates external payment gateway integration

**Key Functions**:
- `simulate_gateway_authorization()` - Simulate payment authorization
- `simulate_gateway_settlement()` - Simulate payment settlement
- `process_pending_authorizations()` - Batch authorize pending payments
- `process_authorized_settlements()` - Batch settle authorized payments
- `generate_settlement_file()` - Create reconciliation files

**Tables Updated**: PAYMENTS, TRANSACTIONS

#### 3. transaction_migration.py
**Purpose**: Transaction data migration and maintenance

**Key Functions**:
- `migrate_legacy_transactions()` - Import legacy transaction data
- `sync_payment_statuses()` - Ensure data consistency
- `fix_data_quality_issues()` - Automated data cleanup
- `archive_old_transactions()` - Historical data archival

**Tables Updated**: TRANSACTIONS, PAYMENTS, RECONCILIATION_LOG

---

## 🔗 Technology Linkages Summary

### C# → PL/SQL
C# WinForms application calls PL/SQL stored procedures via Oracle.ManagedDataAccess.Client for all payment operations.

### Python → Oracle Tables
Python scripts directly insert/update Oracle tables using cx_Oracle for data migration and external integration tasks.

### PL/SQL → Tables
PL/SQL packages contain the business logic and perform all CRUD operations on tables, ensuring data integrity.

---

## 🚀 Setup Instructions

### 1. Database Setup

```sql
-- Connect to Oracle as privileged user
sqlplus system/password@localhost:1521/ORCL

-- Create user
CREATE USER payment_user IDENTIFIED BY your_password;
GRANT CONNECT, RESOURCE, CREATE VIEW, CREATE SEQUENCE TO payment_user;
GRANT UNLIMITED TABLESPACE TO payment_user;

-- Connect as payment_user and run scripts
sqlplus payment_user/your_password@localhost:1521/ORCL

-- Run DDL
@Database/DDL_Tables.sql

-- Create packages
@Database/PKG_PAYMENT_PROCESSING.sql
@Database/PKG_TRANSACTION_MANAGEMENT.sql
@Database/PKG_RECONCILIATION.sql
```

### 2. C# Application Setup

1. Open project in Visual Studio
2. Install NuGet package:
   ```powershell
   Install-Package Oracle.ManagedDataAccess
   ```
3. Update `App.config` connection string:
   ```xml
   <add name="OracleConnection"
        connectionString="Data Source=localhost:1521/ORCL;User Id=payment_user;Password=your_password;"
        providerName="Oracle.ManagedDataAccess.Client" />
   ```
4. Build and run

### 3. Python Scripts Setup

1. Install dependencies:
   ```bash
   pip install cx_Oracle
   ```

2. Install Oracle Instant Client (required by cx_Oracle)

3. Update connection strings in each Python script:
   ```python
   connection_string = "payment_user/your_password@localhost:1521/ORCL"
   ```

4. Run scripts:
   ```bash
   python Python/payment_data_loader.py
   python Python/external_payment_gateway.py
   python Python/transaction_migration.py
   ```

---

## 📝 Usage Examples

### Example 1: Create Payment (C# → PL/SQL)

1. Launch C# WinForms application
2. Enter payment details in "Create Payment" tab
3. Click "Create Payment" button

**Flow**:
```
C# UI → DatabaseHelper.CreatePayment() → PKG_PAYMENT_PROCESSING.CREATE_PAYMENT → PAYMENTS table
```

### Example 2: Load Sample Data (Python → Oracle)

```bash
python Python/payment_data_loader.py
```

**Flow**:
```
Python script → cx_Oracle → INSERT into CUSTOMERS, PAYMENT_METHODS, PAYMENTS tables
```

### Example 3: Process Payments (Python → Oracle)

```bash
python Python/external_payment_gateway.py
```

**Flow**:
```
Python script → cx_Oracle → UPDATE PAYMENTS, INSERT TRANSACTIONS tables
```

### Example 4: Daily Reconciliation (C# → PL/SQL → Oracle)

1. Click "Perform Daily Reconciliation" in C# UI

**Flow**:
```
C# UI → DatabaseHelper.PerformDailyReconciliation()
      → PKG_RECONCILIATION.PERFORM_DAILY_RECONCILIATION
      → SELECT from PAYMENTS/TRANSACTIONS, INSERT into RECONCILIATION_LOG
```

---

## 📊 Complete Data Flow Example

**End-to-End Payment Processing**:

1. **Python**: Load customer and payment method data
   ```
   payment_data_loader.py → CUSTOMERS, PAYMENT_METHODS tables
   ```

2. **C# UI**: Create payment
   ```
   MainForm → DatabaseHelper → PKG_PAYMENT_PROCESSING.CREATE_PAYMENT → PAYMENTS table
   ```

3. **Python**: Authorize payment via gateway
   ```
   external_payment_gateway.py → UPDATE PAYMENTS (status=AUTHORIZED), INSERT TRANSACTIONS
   ```

4. **Python**: Settle payment
   ```
   external_payment_gateway.py → UPDATE PAYMENTS (status=SETTLED), INSERT TRANSACTIONS
   ```

5. **C# UI**: Perform reconciliation
   ```
   MainForm → DatabaseHelper → PKG_RECONCILIATION.PERFORM_DAILY_RECONCILIATION → RECONCILIATION_LOG
   ```

6. **Python**: Archive old data
   ```
   transaction_migration.py → UPDATE TRANSACTIONS (mark for archival)
   ```

---

## 🎯 Key Demonstration Points

### Multi-Technology Integration
✅ C# calls PL/SQL procedures
✅ Python updates Oracle tables
✅ PL/SQL packages enforce business logic
✅ All three technologies work on same database

### Financial Domain
✅ Payment lifecycle (Pending → Authorized → Settled)
✅ Transaction management
✅ Reconciliation processes
✅ Refund handling

### Enterprise Patterns
✅ Stored procedures for business logic
✅ Data access layer (DatabaseHelper.cs)
✅ Batch processing (Python scripts)
✅ Data migration patterns
✅ External system integration

---

## 📈 Procedure Name Consistency

**All procedure names match across C# and PL/SQL**:

| Procedure Name | C# Method | PL/SQL Package |
|----------------|-----------|----------------|
| CREATE_PAYMENT | ✅ | ✅ PKG_PAYMENT_PROCESSING |
| AUTHORIZE_PAYMENT | ✅ | ✅ PKG_PAYMENT_PROCESSING |
| SETTLE_PAYMENT | ✅ | ✅ PKG_PAYMENT_PROCESSING |
| CANCEL_PAYMENT | ✅ | ✅ PKG_PAYMENT_PROCESSING |
| GET_PAYMENT_DETAILS | ✅ | ✅ PKG_PAYMENT_PROCESSING |
| GET_PAYMENT_TRANSACTIONS | ✅ | ✅ PKG_TRANSACTION_MANAGEMENT |
| SEARCH_TRANSACTIONS | ✅ | ✅ PKG_TRANSACTION_MANAGEMENT |
| CREATE_REFUND_TRANSACTION | ✅ | ✅ PKG_TRANSACTION_MANAGEMENT |
| CREATE_RECONCILIATION_RECORD | ✅ | ✅ PKG_RECONCILIATION |
| PERFORM_DAILY_RECONCILIATION | ✅ | ✅ PKG_RECONCILIATION |
| GET_RECONCILIATION_REPORT | ✅ | ✅ PKG_RECONCILIATION |

---

## 📂 File Summary

### Database Files (4 files)
- `DDL_Tables.sql` - Creates 5 tables and 3 sequences
- `PKG_PAYMENT_PROCESSING.sql` - 6 procedures
- `PKG_TRANSACTION_MANAGEMENT.sql` - 6 procedures
- `PKG_RECONCILIATION.sql` - 6 procedures

### C# Files (5 files - as requested)
- `Program.cs` - Application entry point
- `Payment.cs` - Data models (6 classes)
- `DatabaseHelper.cs` - Database layer (11 methods calling PL/SQL)
- `MainForm.cs` - WinForms UI (3 tabs)
- `App.config` - Configuration

### Python Files (3 files - as requested)
- `payment_data_loader.py` - Data migration
- `external_payment_gateway.py` - External integration
- `transaction_migration.py` - Data updates

**Total: 12 code files demonstrating C#, PL/SQL, and Python integration**

---

## 🎓 What This Demo Shows

1. **C# WinForms** calling **Oracle PL/SQL packages** for payment operations
2. **Python scripts** directly updating **Oracle tables** for data migration
3. **PL/SQL packages** containing business logic called by **C# code**
4. **Consistent procedure names** between C# methods and PL/SQL procedures
5. **Financial/payments domain** with realistic workflows
6. **Enterprise patterns**: separation of concerns, stored procedures, batch processing
7. **Multi-technology ecosystem** working together on shared data

---

## 📧 Notes

- This is a **demonstration codebase** for showing technology linkages
- **Procedure names are identical** in C# and PL/SQL for clarity
- All **3 PL/SQL packages** are called from the **5 C# files**
- All **3 Python files** update the Oracle tables created by DDL script
- System demonstrates **end-to-end payment processing** across technologies

---

**Enterprise Payment Processing System - Multi-Technology Integration Demo**
