#!/usr/bin/env python3
"""
Payment Data Loader
====================
Python script for loading payment data from CSV/JSON files into Oracle database tables.
This script demonstrates data migration capabilities in the enterprise payment processing system.

Tables Updated:
- CUSTOMERS
- PAYMENT_METHODS
- PAYMENTS
- TRANSACTIONS

Author: Enterprise Payment System Team
Version: 1.0.0
"""

import csv
import json
import os
import sys
from datetime import datetime
from typing import List, Dict, Any

try:
    import cx_Oracle
except ImportError:
    print("Warning: cx_Oracle not installed. Install with: pip install cx_Oracle")
    cx_Oracle = None


class PaymentDataLoader:
    """Handles loading payment data from external sources into Oracle database"""

    def __init__(self, connection_string: str):
        """
        Initialize the data loader with database connection.

        Args:
            connection_string: Oracle connection string (user/password@host:port/service)
        """
        self.connection_string = connection_string
        self.connection = None
        self.cursor = None
        self.stats = {
            'customers_loaded': 0,
            'payment_methods_loaded': 0,
            'payments_loaded': 0,
            'transactions_loaded': 0,
            'errors': []
        }

    def connect(self):
        """Establish connection to Oracle database"""
        try:
            if cx_Oracle is None:
                print("ERROR: cx_Oracle module not available")
                return False

            print(f"Connecting to Oracle database...")
            self.connection = cx_Oracle.connect(self.connection_string)
            self.cursor = self.connection.cursor()
            print("✓ Connected successfully")
            return True
        except Exception as e:
            print(f"✗ Connection failed: {str(e)}")
            self.stats['errors'].append(f"Connection error: {str(e)}")
            return False

    def disconnect(self):
        """Close database connection"""
        if self.cursor:
            self.cursor.close()
        if self.connection:
            self.connection.close()
        print("✓ Disconnected from database")

    def load_customers_from_csv(self, csv_file_path: str) -> int:
        """
        Load customer data from CSV file into CUSTOMERS table.

        CSV Format: customer_id,customer_name,email,phone,account_number,account_status

        Args:
            csv_file_path: Path to CSV file containing customer data

        Returns:
            Number of records loaded
        """
        print(f"\n=== Loading Customers from {csv_file_path} ===")

        if not os.path.exists(csv_file_path):
            print(f"✗ File not found: {csv_file_path}")
            return 0

        try:
            records_loaded = 0

            with open(csv_file_path, 'r', encoding='utf-8') as file:
                csv_reader = csv.DictReader(file)

                for row in csv_reader:
                    try:
                        # Insert or update customer record
                        self.cursor.execute("""
                            MERGE INTO CUSTOMERS c
                            USING (SELECT :customer_id AS customer_id FROM DUAL) src
                            ON (c.CUSTOMER_ID = src.customer_id)
                            WHEN MATCHED THEN
                                UPDATE SET
                                    CUSTOMER_NAME = :customer_name,
                                    EMAIL = :email,
                                    PHONE = :phone,
                                    ACCOUNT_STATUS = :account_status,
                                    UPDATED_DATE = SYSDATE,
                                    UPDATED_BY = 'PYTHON_LOADER'
                            WHEN NOT MATCHED THEN
                                INSERT (CUSTOMER_ID, CUSTOMER_NAME, EMAIL, PHONE, ACCOUNT_NUMBER,
                                       ACCOUNT_STATUS, CREATED_DATE, CREATED_BY)
                                VALUES (:customer_id, :customer_name, :email, :phone, :account_number,
                                       :account_status, SYSDATE, 'PYTHON_LOADER')
                        """, {
                            'customer_id': int(row['customer_id']),
                            'customer_name': row['customer_name'],
                            'email': row['email'],
                            'phone': row['phone'],
                            'account_number': row['account_number'],
                            'account_status': row.get('account_status', 'ACTIVE')
                        })

                        records_loaded += 1
                        print(f"  ✓ Loaded customer: {row['customer_name']} (ID: {row['customer_id']})")

                    except Exception as e:
                        error_msg = f"Error loading customer {row.get('customer_id', 'unknown')}: {str(e)}"
                        print(f"  ✗ {error_msg}")
                        self.stats['errors'].append(error_msg)

            self.connection.commit()
            self.stats['customers_loaded'] = records_loaded
            print(f"✓ Successfully loaded {records_loaded} customers")
            return records_loaded

        except Exception as e:
            print(f"✗ Error reading CSV file: {str(e)}")
            self.stats['errors'].append(f"CSV read error: {str(e)}")
            return 0

    def load_payment_methods_from_json(self, json_file_path: str) -> int:
        """
        Load payment methods from JSON file into PAYMENT_METHODS table.

        JSON Format:
        [
            {
                "method_id": 1,
                "customer_id": 1,
                "method_type": "CREDIT_CARD",
                "card_number": "****1234",
                "card_type": "VISA",
                "expiry_date": "12/2026",
                "is_default": "Y"
            },
            ...
        ]

        Args:
            json_file_path: Path to JSON file containing payment method data

        Returns:
            Number of records loaded
        """
        print(f"\n=== Loading Payment Methods from {json_file_path} ===")

        if not os.path.exists(json_file_path):
            print(f"✗ File not found: {json_file_path}")
            return 0

        try:
            with open(json_file_path, 'r', encoding='utf-8') as file:
                payment_methods = json.load(file)

            records_loaded = 0

            for method in payment_methods:
                try:
                    self.cursor.execute("""
                        MERGE INTO PAYMENT_METHODS pm
                        USING (SELECT :method_id AS method_id FROM DUAL) src
                        ON (pm.METHOD_ID = src.method_id)
                        WHEN MATCHED THEN
                            UPDATE SET
                                METHOD_TYPE = :method_type,
                                CARD_NUMBER = :card_number,
                                CARD_TYPE = :card_type,
                                EXPIRY_DATE = :expiry_date,
                                BANK_ACCOUNT_NUM = :bank_account_num,
                                ROUTING_NUMBER = :routing_number,
                                IS_DEFAULT = :is_default,
                                STATUS = :status,
                                UPDATED_DATE = SYSDATE
                        WHEN NOT MATCHED THEN
                            INSERT (METHOD_ID, CUSTOMER_ID, METHOD_TYPE, CARD_NUMBER, CARD_TYPE,
                                   EXPIRY_DATE, BANK_ACCOUNT_NUM, ROUTING_NUMBER, IS_DEFAULT,
                                   STATUS, CREATED_DATE)
                            VALUES (:method_id, :customer_id, :method_type, :card_number, :card_type,
                                   :expiry_date, :bank_account_num, :routing_number, :is_default,
                                   :status, SYSDATE)
                    """, {
                        'method_id': method['method_id'],
                        'customer_id': method['customer_id'],
                        'method_type': method['method_type'],
                        'card_number': method.get('card_number'),
                        'card_type': method.get('card_type'),
                        'expiry_date': method.get('expiry_date'),
                        'bank_account_num': method.get('bank_account_num'),
                        'routing_number': method.get('routing_number'),
                        'is_default': method.get('is_default', 'N'),
                        'status': method.get('status', 'ACTIVE')
                    })

                    records_loaded += 1
                    print(f"  ✓ Loaded payment method: {method['method_type']} (ID: {method['method_id']})")

                except Exception as e:
                    error_msg = f"Error loading payment method {method.get('method_id', 'unknown')}: {str(e)}"
                    print(f"  ✗ {error_msg}")
                    self.stats['errors'].append(error_msg)

            self.connection.commit()
            self.stats['payment_methods_loaded'] = records_loaded
            print(f"✓ Successfully loaded {records_loaded} payment methods")
            return records_loaded

        except Exception as e:
            print(f"✗ Error reading JSON file: {str(e)}")
            self.stats['errors'].append(f"JSON read error: {str(e)}")
            return 0

    def bulk_load_payments(self, payment_data: List[Dict[str, Any]]) -> int:
        """
        Bulk load payment records into PAYMENTS table.

        Args:
            payment_data: List of payment dictionaries

        Returns:
            Number of records loaded
        """
        print(f"\n=== Bulk Loading {len(payment_data)} Payments ===")

        records_loaded = 0

        try:
            for payment in payment_data:
                try:
                    # Generate payment ID if not provided
                    if 'payment_id' not in payment:
                        self.cursor.execute("SELECT SEQ_PAYMENT_ID.NEXTVAL FROM DUAL")
                        payment['payment_id'] = self.cursor.fetchone()[0]

                    # Generate reference number
                    timestamp = datetime.now().strftime('%Y%m%d%H%M%S')
                    reference_number = f"PAY{timestamp}{payment['payment_id']:08d}"

                    self.cursor.execute("""
                        INSERT INTO PAYMENTS (
                            PAYMENT_ID, CUSTOMER_ID, PAYMENT_METHOD_ID, PAYMENT_AMOUNT,
                            CURRENCY_CODE, PAYMENT_STATUS, PAYMENT_TYPE, REFERENCE_NUMBER,
                            MERCHANT_ID, DESCRIPTION, CREATED_DATE, CREATED_BY, UPDATED_BY
                        ) VALUES (
                            :payment_id, :customer_id, :payment_method_id, :payment_amount,
                            :currency_code, :payment_status, :payment_type, :reference_number,
                            :merchant_id, :description, SYSDATE, 'PYTHON_LOADER', 'PYTHON_LOADER'
                        )
                    """, {
                        'payment_id': payment['payment_id'],
                        'customer_id': payment['customer_id'],
                        'payment_method_id': payment['payment_method_id'],
                        'payment_amount': payment['payment_amount'],
                        'currency_code': payment.get('currency_code', 'USD'),
                        'payment_status': payment.get('payment_status', 'PENDING'),
                        'payment_type': payment.get('payment_type', 'PURCHASE'),
                        'reference_number': reference_number,
                        'merchant_id': payment.get('merchant_id'),
                        'description': payment.get('description')
                    })

                    records_loaded += 1
                    if records_loaded % 100 == 0:
                        print(f"  Progress: {records_loaded} payments loaded...")

                except Exception as e:
                    error_msg = f"Error loading payment: {str(e)}"
                    print(f"  ✗ {error_msg}")
                    self.stats['errors'].append(error_msg)

            self.connection.commit()
            self.stats['payments_loaded'] = records_loaded
            print(f"✓ Successfully loaded {records_loaded} payments")
            return records_loaded

        except Exception as e:
            self.connection.rollback()
            print(f"✗ Bulk load failed: {str(e)}")
            self.stats['errors'].append(f"Bulk load error: {str(e)}")
            return records_loaded

    def generate_sample_data(self) -> Dict[str, int]:
        """
        Generate and load sample payment data for testing.

        Returns:
            Dictionary with counts of generated records
        """
        print("\n=== Generating Sample Payment Data ===")

        # Sample customers
        sample_customers = [
            {'customer_id': 10, 'customer_name': 'Alice Johnson', 'email': 'alice.j@example.com',
             'phone': '555-1001', 'account_number': 'ACC010', 'account_status': 'ACTIVE'},
            {'customer_id': 11, 'customer_name': 'Bob Williams', 'email': 'bob.w@example.com',
             'phone': '555-1002', 'account_number': 'ACC011', 'account_status': 'ACTIVE'},
            {'customer_id': 12, 'customer_name': 'Carol Brown', 'email': 'carol.b@example.com',
             'phone': '555-1003', 'account_number': 'ACC012', 'account_status': 'ACTIVE'}
        ]

        # Sample payment methods
        sample_methods = [
            {'method_id': 10, 'customer_id': 10, 'method_type': 'CREDIT_CARD',
             'card_number': '****4321', 'card_type': 'VISA', 'expiry_date': '06/2027', 'is_default': 'Y'},
            {'method_id': 11, 'customer_id': 11, 'method_type': 'DEBIT_CARD',
             'card_number': '****8765', 'card_type': 'MASTERCARD', 'expiry_date': '09/2026', 'is_default': 'Y'},
            {'method_id': 12, 'customer_id': 12, 'method_type': 'BANK_ACCOUNT',
             'bank_account_num': '****3456', 'routing_number': '021000021', 'is_default': 'Y'}
        ]

        # Sample payments
        sample_payments = [
            {'customer_id': 10, 'payment_method_id': 10, 'payment_amount': 150.00,
             'payment_type': 'PURCHASE', 'merchant_id': 'MERCH001', 'description': 'Online purchase'},
            {'customer_id': 11, 'payment_method_id': 11, 'payment_amount': 275.50,
             'payment_type': 'PURCHASE', 'merchant_id': 'MERCH002', 'description': 'Store purchase'},
            {'customer_id': 12, 'payment_method_id': 12, 'payment_amount': 500.00,
             'payment_type': 'PURCHASE', 'merchant_id': 'MERCH001', 'description': 'Subscription payment'}
        ]

        results = {
            'customers': 0,
            'payment_methods': 0,
            'payments': 0
        }

        try:
            # Load sample customers
            for customer in sample_customers:
                try:
                    self.cursor.execute("""
                        MERGE INTO CUSTOMERS c
                        USING (SELECT :customer_id AS customer_id FROM DUAL) src
                        ON (c.CUSTOMER_ID = src.customer_id)
                        WHEN NOT MATCHED THEN
                            INSERT (CUSTOMER_ID, CUSTOMER_NAME, EMAIL, PHONE, ACCOUNT_NUMBER,
                                   ACCOUNT_STATUS, CREATED_DATE, CREATED_BY)
                            VALUES (:customer_id, :customer_name, :email, :phone, :account_number,
                                   :account_status, SYSDATE, 'PYTHON_LOADER')
                    """, customer)
                    results['customers'] += 1
                except:
                    pass

            # Load sample payment methods
            for method in sample_methods:
                try:
                    self.cursor.execute("""
                        MERGE INTO PAYMENT_METHODS pm
                        USING (SELECT :method_id AS method_id FROM DUAL) src
                        ON (pm.METHOD_ID = src.method_id)
                        WHEN NOT MATCHED THEN
                            INSERT (METHOD_ID, CUSTOMER_ID, METHOD_TYPE, CARD_NUMBER, CARD_TYPE,
                                   EXPIRY_DATE, BANK_ACCOUNT_NUM, ROUTING_NUMBER, IS_DEFAULT,
                                   STATUS, CREATED_DATE)
                            VALUES (:method_id, :customer_id, :method_type, :card_number, :card_type,
                                   :expiry_date, :bank_account_num, :routing_number, :is_default,
                                   'ACTIVE', SYSDATE)
                    """, method)
                    results['payment_methods'] += 1
                except:
                    pass

            # Load sample payments
            results['payments'] = self.bulk_load_payments(sample_payments)

            self.connection.commit()
            print(f"✓ Sample data generated: {results['customers']} customers, "
                  f"{results['payment_methods']} payment methods, {results['payments']} payments")

            return results

        except Exception as e:
            self.connection.rollback()
            print(f"✗ Error generating sample data: {str(e)}")
            return results

    def print_statistics(self):
        """Print loading statistics"""
        print("\n" + "=" * 60)
        print("DATA LOADING STATISTICS")
        print("=" * 60)
        print(f"Customers Loaded:       {self.stats['customers_loaded']}")
        print(f"Payment Methods Loaded: {self.stats['payment_methods_loaded']}")
        print(f"Payments Loaded:        {self.stats['payments_loaded']}")
        print(f"Transactions Loaded:    {self.stats['transactions_loaded']}")
        print(f"Errors Encountered:     {len(self.stats['errors'])}")

        if self.stats['errors']:
            print("\nErrors:")
            for error in self.stats['errors'][:10]:  # Show first 10 errors
                print(f"  - {error}")
            if len(self.stats['errors']) > 10:
                print(f"  ... and {len(self.stats['errors']) - 10} more errors")

        print("=" * 60)


def main():
    """Main execution function"""
    print("=" * 60)
    print("PAYMENT DATA LOADER - Enterprise Payment Processing System")
    print("=" * 60)

    # Configuration
    # Update this connection string with your Oracle credentials
    connection_string = "payment_user/your_password@localhost:1521/ORCL"

    # Initialize loader
    loader = PaymentDataLoader(connection_string)

    # Connect to database
    if not loader.connect():
        print("Failed to connect to database. Exiting.")
        sys.exit(1)

    try:
        # Generate and load sample data
        loader.generate_sample_data()

        # Example: Load from CSV file (if it exists)
        csv_file = "customers_import.csv"
        if os.path.exists(csv_file):
            loader.load_customers_from_csv(csv_file)

        # Example: Load from JSON file (if it exists)
        json_file = "payment_methods_import.json"
        if os.path.exists(json_file):
            loader.load_payment_methods_from_json(json_file)

        # Print statistics
        loader.print_statistics()

    except Exception as e:
        print(f"\n✗ Unexpected error: {str(e)}")
    finally:
        loader.disconnect()

    print("\n✓ Data loading completed!")


if __name__ == "__main__":
    main()
