#!/usr/bin/env python3
"""
External Payment Gateway Integration
=====================================
Python script for simulating integration with external payment gateways.
Updates payment and transaction status in Oracle database based on gateway responses.

This script demonstrates:
- External API integration patterns
- Payment status synchronization
- Gateway response processing
- Webhook handling simulation

Tables Updated:
- PAYMENTS (status updates)
- TRANSACTIONS (gateway responses)

Author: Enterprise Payment System Team
Version: 1.0.0
"""

import json
import random
import time
from datetime import datetime, timedelta
from typing import Dict, List, Optional, Tuple

try:
    import cx_Oracle
except ImportError:
    print("Warning: cx_Oracle not installed. Install with: pip install cx_Oracle")
    cx_Oracle = None


class PaymentGatewaySimulator:
    """Simulates external payment gateway integration"""

    # Gateway response codes
    RESPONSE_CODES = {
        '00': 'Approved',
        '01': 'Refer to card issuer',
        '05': 'Do not honor',
        '14': 'Invalid card number',
        '41': 'Lost card',
        '43': 'Stolen card',
        '51': 'Insufficient funds',
        '54': 'Expired card',
        '61': 'Exceeds withdrawal limit',
        '91': 'Issuer unavailable'
    }

    def __init__(self, connection_string: str):
        """
        Initialize gateway simulator with database connection.

        Args:
            connection_string: Oracle connection string
        """
        self.connection_string = connection_string
        self.connection = None
        self.cursor = None
        self.stats = {
            'payments_processed': 0,
            'authorizations': 0,
            'settlements': 0,
            'failures': 0,
            'errors': []
        }

    def connect(self):
        """Establish connection to Oracle database"""
        try:
            if cx_Oracle is None:
                print("ERROR: cx_Oracle module not available")
                return False

            print("Connecting to Oracle database...")
            self.connection = cx_Oracle.connect(self.connection_string)
            self.cursor = self.connection.cursor()
            print("✓ Connected successfully")
            return True
        except Exception as e:
            print(f"✗ Connection failed: {str(e)}")
            return False

    def disconnect(self):
        """Close database connection"""
        if self.cursor:
            self.cursor.close()
        if self.connection:
            self.connection.close()
        print("✓ Disconnected from database")

    def simulate_gateway_authorization(self, payment_id: int) -> Tuple[bool, str, str]:
        """
        Simulate payment gateway authorization process.

        Args:
            payment_id: Payment ID to authorize

        Returns:
            Tuple of (success, response_code, response_message)
        """
        # Simulate network delay
        time.sleep(random.uniform(0.1, 0.5))

        # Simulate success/failure (90% success rate)
        if random.random() < 0.9:
            response_code = '00'
            auth_code = f"AUTH{datetime.now().strftime('%Y%m%d%H%M%S')}{random.randint(1000, 9999)}"
            response_message = f"{self.RESPONSE_CODES[response_code]} - Authorization: {auth_code}"
            return True, response_code, response_message
        else:
            # Random failure
            response_code = random.choice(['05', '51', '54', '61', '91'])
            response_message = self.RESPONSE_CODES[response_code]
            return False, response_code, response_message

    def simulate_gateway_settlement(self, payment_id: int) -> Tuple[bool, str, str]:
        """
        Simulate payment gateway settlement/capture process.

        Args:
            payment_id: Payment ID to settle

        Returns:
            Tuple of (success, response_code, response_message)
        """
        # Simulate network delay
        time.sleep(random.uniform(0.2, 0.8))

        # Simulate success (95% success rate for settlements)
        if random.random() < 0.95:
            response_code = '00'
            settlement_id = f"SETT{datetime.now().strftime('%Y%m%d')}{random.randint(10000, 99999)}"
            response_message = f"Settlement successful - ID: {settlement_id}"
            return True, response_code, response_message
        else:
            response_code = '91'
            response_message = "Settlement failed - Gateway timeout"
            return False, response_code, response_message

    def process_pending_authorizations(self) -> int:
        """
        Process all pending payments through gateway authorization.

        Returns:
            Number of payments processed
        """
        print("\n=== Processing Pending Authorizations ===")

        try:
            # Get pending payments
            self.cursor.execute("""
                SELECT PAYMENT_ID, REFERENCE_NUMBER, PAYMENT_AMOUNT, CUSTOMER_ID
                FROM PAYMENTS
                WHERE PAYMENT_STATUS = 'PENDING'
                AND PAYMENT_TYPE IN ('PURCHASE', 'AUTHORIZATION')
                ORDER BY CREATED_DATE
            """)

            pending_payments = self.cursor.fetchall()
            print(f"Found {len(pending_payments)} pending payments to authorize")

            processed_count = 0

            for payment in pending_payments:
                payment_id, ref_number, amount, customer_id = payment

                print(f"\n  Processing Payment #{payment_id} ({ref_number}) - Amount: ${amount:.2f}")

                # Simulate gateway authorization
                success, response_code, response_message = self.simulate_gateway_authorization(payment_id)

                if success:
                    # Authorization successful - update payment status
                    auth_code = f"AUTH{payment_id}{random.randint(1000, 9999)}"

                    try:
                        # Update payment to AUTHORIZED status
                        self.cursor.execute("""
                            UPDATE PAYMENTS
                            SET PAYMENT_STATUS = 'AUTHORIZED',
                                AUTHORIZATION_CODE = :auth_code,
                                GATEWAY_RESPONSE = :gateway_response,
                                PROCESSED_DATE = SYSDATE,
                                UPDATED_DATE = SYSDATE,
                                UPDATED_BY = 'GATEWAY_SIMULATOR'
                            WHERE PAYMENT_ID = :payment_id
                        """, {
                            'payment_id': payment_id,
                            'auth_code': auth_code,
                            'gateway_response': response_message
                        })

                        # Create transaction record
                        self.cursor.execute("""
                            INSERT INTO TRANSACTIONS (
                                TRANSACTION_ID, PAYMENT_ID, TRANSACTION_TYPE, TRANSACTION_AMOUNT,
                                TRANSACTION_STATUS, GATEWAY_TXN_ID, RESPONSE_CODE, RESPONSE_MESSAGE,
                                TRANSACTION_DATE, PROCESSED_DATE, CREATED_BY
                            ) VALUES (
                                SEQ_TRANSACTION_ID.NEXTVAL, :payment_id, 'AUTH', :amount,
                                'APPROVED', :gateway_txn_id, :response_code, :response_message,
                                SYSDATE, SYSDATE, 'GATEWAY_SIMULATOR'
                            )
                        """, {
                            'payment_id': payment_id,
                            'amount': amount,
                            'gateway_txn_id': auth_code,
                            'response_code': response_code,
                            'response_message': response_message
                        })

                        self.connection.commit()
                        processed_count += 1
                        self.stats['authorizations'] += 1

                        print(f"    ✓ AUTHORIZED - Code: {auth_code}")

                    except Exception as e:
                        self.connection.rollback()
                        error_msg = f"Database error for payment {payment_id}: {str(e)}"
                        print(f"    ✗ {error_msg}")
                        self.stats['errors'].append(error_msg)

                else:
                    # Authorization failed
                    try:
                        self.cursor.execute("""
                            UPDATE PAYMENTS
                            SET PAYMENT_STATUS = 'FAILED',
                                ERROR_MESSAGE = :error_message,
                                GATEWAY_RESPONSE = :gateway_response,
                                PROCESSED_DATE = SYSDATE,
                                UPDATED_DATE = SYSDATE,
                                UPDATED_BY = 'GATEWAY_SIMULATOR'
                            WHERE PAYMENT_ID = :payment_id
                        """, {
                            'payment_id': payment_id,
                            'error_message': f"Authorization failed: {response_code}",
                            'gateway_response': response_message
                        })

                        # Create failed transaction record
                        self.cursor.execute("""
                            INSERT INTO TRANSACTIONS (
                                TRANSACTION_ID, PAYMENT_ID, TRANSACTION_TYPE, TRANSACTION_AMOUNT,
                                TRANSACTION_STATUS, RESPONSE_CODE, RESPONSE_MESSAGE,
                                TRANSACTION_DATE, PROCESSED_DATE, CREATED_BY
                            ) VALUES (
                                SEQ_TRANSACTION_ID.NEXTVAL, :payment_id, 'AUTH', :amount,
                                'DECLINED', :response_code, :response_message,
                                SYSDATE, SYSDATE, 'GATEWAY_SIMULATOR'
                            )
                        """, {
                            'payment_id': payment_id,
                            'amount': amount,
                            'response_code': response_code,
                            'response_message': response_message
                        })

                        self.connection.commit()
                        processed_count += 1
                        self.stats['failures'] += 1

                        print(f"    ✗ DECLINED - Code: {response_code} - {response_message}")

                    except Exception as e:
                        self.connection.rollback()
                        error_msg = f"Database error for payment {payment_id}: {str(e)}"
                        print(f"    ✗ {error_msg}")
                        self.stats['errors'].append(error_msg)

            self.stats['payments_processed'] += processed_count
            print(f"\n✓ Processed {processed_count} authorization requests")
            return processed_count

        except Exception as e:
            print(f"✗ Error processing authorizations: {str(e)}")
            self.stats['errors'].append(f"Authorization processing error: {str(e)}")
            return 0

    def process_authorized_settlements(self) -> int:
        """
        Process authorized payments through gateway settlement.

        Returns:
            Number of settlements processed
        """
        print("\n=== Processing Authorized Settlements ===")

        try:
            # Get authorized payments ready for settlement
            self.cursor.execute("""
                SELECT PAYMENT_ID, REFERENCE_NUMBER, PAYMENT_AMOUNT, AUTHORIZATION_CODE
                FROM PAYMENTS
                WHERE PAYMENT_STATUS = 'AUTHORIZED'
                AND PROCESSED_DATE < SYSDATE - INTERVAL '1' MINUTE
                ORDER BY PROCESSED_DATE
            """)

            authorized_payments = self.cursor.fetchall()
            print(f"Found {len(authorized_payments)} authorized payments to settle")

            processed_count = 0

            for payment in authorized_payments:
                payment_id, ref_number, amount, auth_code = payment

                print(f"\n  Settling Payment #{payment_id} ({ref_number}) - Amount: ${amount:.2f}")

                # Simulate gateway settlement
                success, response_code, response_message = self.simulate_gateway_settlement(payment_id)

                if success:
                    try:
                        # Update payment to SETTLED status
                        self.cursor.execute("""
                            UPDATE PAYMENTS
                            SET PAYMENT_STATUS = 'SETTLED',
                                SETTLEMENT_DATE = SYSDATE,
                                UPDATED_DATE = SYSDATE,
                                UPDATED_BY = 'GATEWAY_SIMULATOR'
                            WHERE PAYMENT_ID = :payment_id
                        """, {'payment_id': payment_id})

                        # Create settlement transaction record
                        self.cursor.execute("""
                            INSERT INTO TRANSACTIONS (
                                TRANSACTION_ID, PAYMENT_ID, TRANSACTION_TYPE, TRANSACTION_AMOUNT,
                                TRANSACTION_STATUS, GATEWAY_TXN_ID, RESPONSE_CODE, RESPONSE_MESSAGE,
                                TRANSACTION_DATE, PROCESSED_DATE, CREATED_BY
                            ) VALUES (
                                SEQ_TRANSACTION_ID.NEXTVAL, :payment_id, 'CAPTURE', :amount,
                                'COMPLETED', :gateway_txn_id, :response_code, :response_message,
                                SYSDATE, SYSDATE, 'GATEWAY_SIMULATOR'
                            )
                        """, {
                            'payment_id': payment_id,
                            'amount': amount,
                            'gateway_txn_id': f"SETT{payment_id}",
                            'response_code': response_code,
                            'response_message': response_message
                        })

                        self.connection.commit()
                        processed_count += 1
                        self.stats['settlements'] += 1

                        print(f"    ✓ SETTLED successfully")

                    except Exception as e:
                        self.connection.rollback()
                        error_msg = f"Database error for payment {payment_id}: {str(e)}"
                        print(f"    ✗ {error_msg}")
                        self.stats['errors'].append(error_msg)

                else:
                    print(f"    ✗ Settlement failed: {response_message}")
                    self.stats['failures'] += 1

            print(f"\n✓ Processed {processed_count} settlements")
            return processed_count

        except Exception as e:
            print(f"✗ Error processing settlements: {str(e)}")
            self.stats['errors'].append(f"Settlement processing error: {str(e)}")
            return 0

    def sync_gateway_status(self) -> int:
        """
        Synchronize payment status with external gateway.
        Simulates polling gateway for status updates.

        Returns:
            Number of payments synchronized
        """
        print("\n=== Synchronizing Gateway Status ===")

        try:
            # Get in-flight payments
            self.cursor.execute("""
                SELECT PAYMENT_ID, REFERENCE_NUMBER, PAYMENT_STATUS
                FROM PAYMENTS
                WHERE PAYMENT_STATUS IN ('PENDING', 'AUTHORIZED')
                AND CREATED_DATE > SYSDATE - INTERVAL '7' DAY
            """)

            payments = self.cursor.fetchall()
            print(f"Checking status for {len(payments)} payments")

            synced_count = 0

            for payment in payments:
                payment_id, ref_number, current_status = payment

                # Simulate gateway status check
                print(f"  Checking Payment #{payment_id} ({ref_number}) - Current: {current_status}")

                # In a real implementation, this would call the gateway API
                # For simulation, we'll just log it
                synced_count += 1

            print(f"✓ Synchronized {synced_count} payment statuses")
            return synced_count

        except Exception as e:
            print(f"✗ Error synchronizing status: {str(e)}")
            return 0

    def generate_settlement_file(self, settlement_date: datetime) -> str:
        """
        Generate a settlement file for reconciliation.
        Simulates gateway providing settlement data.

        Args:
            settlement_date: Date for settlement report

        Returns:
            Path to generated settlement file
        """
        print(f"\n=== Generating Settlement File for {settlement_date.strftime('%Y-%m-%d')} ===")

        try:
            # Get settled transactions for the date
            self.cursor.execute("""
                SELECT
                    p.PAYMENT_ID,
                    p.REFERENCE_NUMBER,
                    p.PAYMENT_AMOUNT,
                    p.SETTLEMENT_DATE,
                    c.ACCOUNT_NUMBER
                FROM PAYMENTS p
                INNER JOIN CUSTOMERS c ON p.CUSTOMER_ID = c.CUSTOMER_ID
                WHERE TRUNC(p.SETTLEMENT_DATE) = TRUNC(:settlement_date)
                AND p.PAYMENT_STATUS = 'SETTLED'
            """, {'settlement_date': settlement_date})

            settlements = self.cursor.fetchall()

            # Generate file
            filename = f"gateway_settlement_{settlement_date.strftime('%Y%m%d')}.dat"
            filepath = f"C:\\PaymentFiles\\Gateway\\{filename}"

            settlement_data = []
            for settlement in settlements:
                payment_id, ref_num, amount, settle_date, account_num = settlement
                settlement_data.append({
                    'payment_id': payment_id,
                    'reference_number': ref_num,
                    'amount': float(amount),
                    'settlement_date': settle_date.strftime('%Y-%m-%d %H:%M:%S'),
                    'account_number': account_num,
                    'gateway_batch_id': f"BATCH{settlement_date.strftime('%Y%m%d')}"
                })

            # Write JSON file (in real scenario, could be CSV, XML, etc.)
            try:
                with open(filepath, 'w') as f:
                    json.dump(settlement_data, f, indent=2)
                print(f"✓ Settlement file generated: {filepath}")
                print(f"  Records: {len(settlement_data)}")
                return filepath
            except:
                print(f"  Note: Could not write to {filepath} (directory may not exist)")
                print(f"  Settlement data prepared with {len(settlement_data)} records")
                return filename

        except Exception as e:
            print(f"✗ Error generating settlement file: {str(e)}")
            return ""

    def print_statistics(self):
        """Print processing statistics"""
        print("\n" + "=" * 60)
        print("GATEWAY PROCESSING STATISTICS")
        print("=" * 60)
        print(f"Payments Processed:     {self.stats['payments_processed']}")
        print(f"Authorizations:         {self.stats['authorizations']}")
        print(f"Settlements:            {self.stats['settlements']}")
        print(f"Failures:               {self.stats['failures']}")
        print(f"Errors:                 {len(self.stats['errors'])}")

        if self.stats['errors']:
            print("\nErrors:")
            for error in self.stats['errors'][:5]:
                print(f"  - {error}")

        print("=" * 60)


def main():
    """Main execution function"""
    print("=" * 60)
    print("EXTERNAL PAYMENT GATEWAY SIMULATOR")
    print("Enterprise Payment Processing System")
    print("=" * 60)

    # Configuration
    connection_string = "payment_user/your_password@localhost:1521/ORCL"

    # Initialize gateway simulator
    gateway = PaymentGatewaySimulator(connection_string)

    if not gateway.connect():
        print("Failed to connect to database. Exiting.")
        return

    try:
        # Process pending authorizations
        gateway.process_pending_authorizations()

        # Process authorized settlements
        gateway.process_authorized_settlements()

        # Sync gateway status
        gateway.sync_gateway_status()

        # Generate settlement file for today
        gateway.generate_settlement_file(datetime.now())

        # Print statistics
        gateway.print_statistics()

    except Exception as e:
        print(f"\n✗ Unexpected error: {str(e)}")
    finally:
        gateway.disconnect()

    print("\n✓ Gateway processing completed!")


if __name__ == "__main__":
    main()
