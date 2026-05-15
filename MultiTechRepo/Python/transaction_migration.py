#!/usr/bin/env python3
"""
Transaction Migration and Data Update Utility
==============================================
Python script for migrating and updating transaction data in Oracle database.
Handles batch updates, data transformations, and legacy system migrations.

This script demonstrates:
- Batch transaction processing
- Data migration from legacy systems
- Transaction status updates
- Historical data archival
- Data quality checks and corrections

Tables Updated:
- TRANSACTIONS
- PAYMENTS
- RECONCILIATION_LOG

Author: Enterprise Payment System Team
Version: 1.0.0
"""

import csv
import json
import sys
from datetime import datetime, timedelta
from typing import List, Dict, Any, Optional

try:
    import cx_Oracle
except ImportError:
    print("Warning: cx_Oracle not installed. Install with: pip install cx_Oracle")
    cx_Oracle = None


class TransactionMigration:
    """Handles transaction data migration and updates"""

    def __init__(self, connection_string: str):
        """
        Initialize migration utility with database connection.

        Args:
            connection_string: Oracle connection string
        """
        self.connection_string = connection_string
        self.connection = None
        self.cursor = None
        self.stats = {
            'transactions_migrated': 0,
            'transactions_updated': 0,
            'payments_updated': 0,
            'records_archived': 0,
            'data_quality_fixes': 0,
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

    def migrate_legacy_transactions(self, legacy_file: str) -> int:
        """
        Migrate transaction data from legacy system file.

        CSV Format: transaction_id,payment_id,transaction_type,amount,status,
                   transaction_date,gateway_txn_id,response_code

        Args:
            legacy_file: Path to legacy transaction CSV file

        Returns:
            Number of transactions migrated
        """
        print(f"\n=== Migrating Legacy Transactions from {legacy_file} ===")

        try:
            migrated_count = 0

            with open(legacy_file, 'r', encoding='utf-8') as file:
                csv_reader = csv.DictReader(file)

                for row in csv_reader:
                    try:
                        # Parse transaction date
                        txn_date = datetime.strptime(row['transaction_date'], '%Y-%m-%d %H:%M:%S')

                        # Check if transaction already exists
                        self.cursor.execute("""
                            SELECT COUNT(*) FROM TRANSACTIONS
                            WHERE TRANSACTION_ID = :txn_id
                        """, {'txn_id': int(row['transaction_id'])})

                        exists = self.cursor.fetchone()[0] > 0

                        if not exists:
                            # Insert legacy transaction
                            self.cursor.execute("""
                                INSERT INTO TRANSACTIONS (
                                    TRANSACTION_ID, PAYMENT_ID, TRANSACTION_TYPE,
                                    TRANSACTION_AMOUNT, TRANSACTION_STATUS,
                                    GATEWAY_TXN_ID, RESPONSE_CODE, RESPONSE_MESSAGE,
                                    TRANSACTION_DATE, PROCESSED_DATE, CREATED_BY
                                ) VALUES (
                                    :txn_id, :payment_id, :txn_type, :amount, :status,
                                    :gateway_txn_id, :response_code, :response_message,
                                    :txn_date, :txn_date, 'LEGACY_MIGRATION'
                                )
                            """, {
                                'txn_id': int(row['transaction_id']),
                                'payment_id': int(row['payment_id']),
                                'txn_type': row['transaction_type'],
                                'amount': float(row['amount']),
                                'status': row['status'],
                                'gateway_txn_id': row.get('gateway_txn_id'),
                                'response_code': row.get('response_code', '00'),
                                'response_message': 'Migrated from legacy system',
                                'txn_date': txn_date
                            })

                            migrated_count += 1

                            if migrated_count % 100 == 0:
                                print(f"  Progress: {migrated_count} transactions migrated...")
                                self.connection.commit()

                        else:
                            print(f"  ⊘ Transaction {row['transaction_id']} already exists, skipping")

                    except Exception as e:
                        error_msg = f"Error migrating transaction {row.get('transaction_id', 'unknown')}: {str(e)}"
                        print(f"  ✗ {error_msg}")
                        self.stats['errors'].append(error_msg)

            self.connection.commit()
            self.stats['transactions_migrated'] = migrated_count
            print(f"✓ Successfully migrated {migrated_count} transactions")
            return migrated_count

        except FileNotFoundError:
            print(f"✗ File not found: {legacy_file}")
            return 0
        except Exception as e:
            self.connection.rollback()
            print(f"✗ Migration failed: {str(e)}")
            self.stats['errors'].append(f"Migration error: {str(e)}")
            return 0

    def update_transaction_statuses(self, status_updates: List[Dict[str, Any]]) -> int:
        """
        Batch update transaction statuses.

        Args:
            status_updates: List of dicts with transaction_id, new_status, response_message

        Returns:
            Number of transactions updated
        """
        print(f"\n=== Updating Transaction Statuses ({len(status_updates)} records) ===")

        updated_count = 0

        try:
            for update in status_updates:
                try:
                    self.cursor.execute("""
                        UPDATE TRANSACTIONS
                        SET TRANSACTION_STATUS = :new_status,
                            RESPONSE_MESSAGE = :response_message,
                            PROCESSED_DATE = SYSDATE
                        WHERE TRANSACTION_ID = :txn_id
                    """, {
                        'txn_id': update['transaction_id'],
                        'new_status': update['new_status'],
                        'response_message': update.get('response_message', 'Status updated by migration')
                    })

                    if self.cursor.rowcount > 0:
                        updated_count += 1

                        if updated_count % 50 == 0:
                            print(f"  Progress: {updated_count} transactions updated...")

                except Exception as e:
                    error_msg = f"Error updating transaction {update.get('transaction_id', 'unknown')}: {str(e)}"
                    print(f"  ✗ {error_msg}")
                    self.stats['errors'].append(error_msg)

            self.connection.commit()
            self.stats['transactions_updated'] = updated_count
            print(f"✓ Successfully updated {updated_count} transaction statuses")
            return updated_count

        except Exception as e:
            self.connection.rollback()
            print(f"✗ Batch update failed: {str(e)}")
            return updated_count

    def sync_payment_statuses(self) -> int:
        """
        Synchronize payment statuses based on transaction states.
        Ensures data consistency between PAYMENTS and TRANSACTIONS tables.

        Returns:
            Number of payments updated
        """
        print("\n=== Synchronizing Payment Statuses ===")

        try:
            updated_count = 0

            # Find payments with completed capture transactions but not settled
            self.cursor.execute("""
                SELECT DISTINCT p.PAYMENT_ID, p.PAYMENT_STATUS
                FROM PAYMENTS p
                INNER JOIN TRANSACTIONS t ON p.PAYMENT_ID = t.PAYMENT_ID
                WHERE t.TRANSACTION_TYPE = 'CAPTURE'
                AND t.TRANSACTION_STATUS = 'COMPLETED'
                AND p.PAYMENT_STATUS != 'SETTLED'
            """)

            payments_to_update = self.cursor.fetchall()
            print(f"Found {len(payments_to_update)} payments to synchronize")

            for payment_id, current_status in payments_to_update:
                try:
                    self.cursor.execute("""
                        UPDATE PAYMENTS
                        SET PAYMENT_STATUS = 'SETTLED',
                            SETTLEMENT_DATE = SYSDATE,
                            UPDATED_DATE = SYSDATE,
                            UPDATED_BY = 'TRANSACTION_MIGRATION'
                        WHERE PAYMENT_ID = :payment_id
                    """, {'payment_id': payment_id})

                    updated_count += 1
                    print(f"  ✓ Updated Payment #{payment_id}: {current_status} → SETTLED")

                except Exception as e:
                    error_msg = f"Error updating payment {payment_id}: {str(e)}"
                    print(f"  ✗ {error_msg}")
                    self.stats['errors'].append(error_msg)

            self.connection.commit()
            self.stats['payments_updated'] = updated_count
            print(f"✓ Successfully synchronized {updated_count} payment statuses")
            return updated_count

        except Exception as e:
            self.connection.rollback()
            print(f"✗ Synchronization failed: {str(e)}")
            return 0

    def fix_data_quality_issues(self) -> int:
        """
        Identify and fix common data quality issues.

        Returns:
            Number of issues fixed
        """
        print("\n=== Fixing Data Quality Issues ===")

        fixes_applied = 0

        try:
            # Fix 1: Update missing processed dates
            self.cursor.execute("""
                UPDATE TRANSACTIONS
                SET PROCESSED_DATE = TRANSACTION_DATE
                WHERE PROCESSED_DATE IS NULL
                AND TRANSACTION_STATUS IN ('APPROVED', 'COMPLETED')
            """)
            fix1_count = self.cursor.rowcount
            if fix1_count > 0:
                print(f"  ✓ Fixed {fix1_count} missing processed dates")
                fixes_applied += fix1_count

            # Fix 2: Update missing response codes
            self.cursor.execute("""
                UPDATE TRANSACTIONS
                SET RESPONSE_CODE = '00'
                WHERE RESPONSE_CODE IS NULL
                AND TRANSACTION_STATUS IN ('APPROVED', 'COMPLETED')
            """)
            fix2_count = self.cursor.rowcount
            if fix2_count > 0:
                print(f"  ✓ Fixed {fix2_count} missing response codes")
                fixes_applied += fix2_count

            # Fix 3: Standardize transaction types
            self.cursor.execute("""
                UPDATE TRANSACTIONS
                SET TRANSACTION_TYPE = UPPER(TRANSACTION_TYPE)
                WHERE TRANSACTION_TYPE != UPPER(TRANSACTION_TYPE)
            """)
            fix3_count = self.cursor.rowcount
            if fix3_count > 0:
                print(f"  ✓ Standardized {fix3_count} transaction types")
                fixes_applied += fix3_count

            # Fix 4: Update missing gateway transaction IDs
            self.cursor.execute("""
                UPDATE TRANSACTIONS
                SET GATEWAY_TXN_ID = 'GW' || TO_CHAR(TRANSACTION_DATE, 'YYYYMMDD') || TRANSACTION_ID
                WHERE GATEWAY_TXN_ID IS NULL
                AND TRANSACTION_STATUS = 'COMPLETED'
            """)
            fix4_count = self.cursor.rowcount
            if fix4_count > 0:
                print(f"  ✓ Generated {fix4_count} missing gateway transaction IDs")
                fixes_applied += fix4_count

            self.connection.commit()
            self.stats['data_quality_fixes'] = fixes_applied
            print(f"✓ Applied {fixes_applied} data quality fixes")
            return fixes_applied

        except Exception as e:
            self.connection.rollback()
            print(f"✗ Data quality fix failed: {str(e)}")
            return fixes_applied

    def archive_old_transactions(self, days_old: int = 365) -> int:
        """
        Archive old completed transactions to reduce active table size.
        In production, this would move data to an archive table or partition.

        Args:
            days_old: Number of days before archival (default 365)

        Returns:
            Number of transactions archived
        """
        print(f"\n=== Archiving Transactions Older Than {days_old} Days ===")

        try:
            cutoff_date = datetime.now() - timedelta(days=days_old)

            # Count transactions to archive
            self.cursor.execute("""
                SELECT COUNT(*)
                FROM TRANSACTIONS
                WHERE TRANSACTION_DATE < :cutoff_date
                AND TRANSACTION_STATUS IN ('COMPLETED', 'CANCELLED')
            """, {'cutoff_date': cutoff_date})

            archive_count = self.cursor.fetchone()[0]

            if archive_count == 0:
                print("  No transactions to archive")
                return 0

            print(f"Found {archive_count} transactions to archive")

            # In a real implementation, we would:
            # 1. Copy data to TRANSACTIONS_ARCHIVE table
            # 2. Delete from TRANSACTIONS table
            # For this demo, we'll just mark them

            self.cursor.execute("""
                UPDATE TRANSACTIONS
                SET RESPONSE_MESSAGE = RESPONSE_MESSAGE || ' [ARCHIVED]'
                WHERE TRANSACTION_DATE < :cutoff_date
                AND TRANSACTION_STATUS IN ('COMPLETED', 'CANCELLED')
                AND RESPONSE_MESSAGE NOT LIKE '%ARCHIVED%'
            """, {'cutoff_date': cutoff_date})

            archived_count = self.cursor.rowcount
            self.connection.commit()

            self.stats['records_archived'] = archived_count
            print(f"✓ Marked {archived_count} transactions for archival")
            return archived_count

        except Exception as e:
            self.connection.rollback()
            print(f"✗ Archival failed: {str(e)}")
            return 0

    def generate_migration_report(self) -> Dict[str, Any]:
        """
        Generate comprehensive migration report with statistics.

        Returns:
            Dictionary containing report data
        """
        print("\n=== Generating Migration Report ===")

        try:
            report = {
                'report_date': datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                'database_statistics': {},
                'migration_statistics': self.stats,
                'data_quality_metrics': {}
            }

            # Get database statistics
            self.cursor.execute("SELECT COUNT(*) FROM PAYMENTS")
            report['database_statistics']['total_payments'] = self.cursor.fetchone()[0]

            self.cursor.execute("SELECT COUNT(*) FROM TRANSACTIONS")
            report['database_statistics']['total_transactions'] = self.cursor.fetchone()[0]

            self.cursor.execute("SELECT COUNT(*) FROM RECONCILIATION_LOG")
            report['database_statistics']['total_reconciliations'] = self.cursor.fetchone()[0]

            # Get status distribution
            self.cursor.execute("""
                SELECT PAYMENT_STATUS, COUNT(*)
                FROM PAYMENTS
                GROUP BY PAYMENT_STATUS
            """)
            status_dist = {}
            for status, count in self.cursor.fetchall():
                status_dist[status] = count
            report['database_statistics']['payment_status_distribution'] = status_dist

            # Data quality metrics
            self.cursor.execute("""
                SELECT COUNT(*)
                FROM TRANSACTIONS
                WHERE GATEWAY_TXN_ID IS NULL
            """)
            report['data_quality_metrics']['missing_gateway_ids'] = self.cursor.fetchone()[0]

            self.cursor.execute("""
                SELECT COUNT(*)
                FROM TRANSACTIONS
                WHERE PROCESSED_DATE IS NULL
                AND TRANSACTION_STATUS IN ('APPROVED', 'COMPLETED')
            """)
            report['data_quality_metrics']['missing_processed_dates'] = self.cursor.fetchone()[0]

            # Generate report file
            report_filename = f"migration_report_{datetime.now().strftime('%Y%m%d_%H%M%S')}.json"
            try:
                with open(report_filename, 'w') as f:
                    json.dump(report, f, indent=2)
                print(f"✓ Report saved to {report_filename}")
            except:
                print(f"  Note: Could not write report file")

            return report

        except Exception as e:
            print(f"✗ Error generating report: {str(e)}")
            return {}

    def print_statistics(self):
        """Print migration statistics"""
        print("\n" + "=" * 60)
        print("TRANSACTION MIGRATION STATISTICS")
        print("=" * 60)
        print(f"Transactions Migrated:  {self.stats['transactions_migrated']}")
        print(f"Transactions Updated:   {self.stats['transactions_updated']}")
        print(f"Payments Updated:       {self.stats['payments_updated']}")
        print(f"Records Archived:       {self.stats['records_archived']}")
        print(f"Data Quality Fixes:     {self.stats['data_quality_fixes']}")
        print(f"Errors Encountered:     {len(self.stats['errors'])}")

        if self.stats['errors']:
            print("\nErrors:")
            for error in self.stats['errors'][:5]:
                print(f"  - {error}")
            if len(self.stats['errors']) > 5:
                print(f"  ... and {len(self.stats['errors']) - 5} more errors")

        print("=" * 60)


def main():
    """Main execution function"""
    print("=" * 60)
    print("TRANSACTION MIGRATION UTILITY")
    print("Enterprise Payment Processing System")
    print("=" * 60)

    # Configuration
    connection_string = "payment_user/your_password@localhost:1521/ORCL"

    # Initialize migration utility
    migration = TransactionMigration(connection_string)

    if not migration.connect():
        print("Failed to connect to database. Exiting.")
        sys.exit(1)

    try:
        # Example: Migrate legacy transactions (if file exists)
        legacy_file = "legacy_transactions.csv"
        # Uncomment to run if file exists
        # migration.migrate_legacy_transactions(legacy_file)

        # Synchronize payment statuses
        migration.sync_payment_statuses()

        # Fix data quality issues
        migration.fix_data_quality_issues()

        # Example: Batch update transaction statuses
        # status_updates = [
        #     {'transaction_id': 1001, 'new_status': 'COMPLETED', 'response_message': 'Updated via migration'},
        #     {'transaction_id': 1002, 'new_status': 'COMPLETED', 'response_message': 'Updated via migration'}
        # ]
        # migration.update_transaction_statuses(status_updates)

        # Archive old transactions (older than 365 days)
        migration.archive_old_transactions(days_old=365)

        # Generate migration report
        report = migration.generate_migration_report()

        # Print statistics
        migration.print_statistics()

    except Exception as e:
        print(f"\n✗ Unexpected error: {str(e)}")
    finally:
        migration.disconnect()

    print("\n✓ Transaction migration completed!")


if __name__ == "__main__":
    main()
