using System;
using System.Windows.Forms;

namespace PaymentProcessingApp
{
    /// <summary>
    /// Entry point for Payment Processing Application
    /// .NET Framework 4.7.1 WinForms Application
    /// </summary>
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Display splash/info message
                MessageBox.Show(
                    "Payment Processing System - Enterprise Demo\n\n" +
                    "This application demonstrates linkages between:\n" +
                    "- C# WinForms (.NET Framework 4.7.1)\n" +
                    "- Oracle PL/SQL Packages (PKG_PAYMENT_PROCESSING, PKG_TRANSACTION_MANAGEMENT, PKG_RECONCILIATION)\n" +
                    "- Python Scripts (Data Migration & External Integration)\n\n" +
                    "Financial Domain: Payment Processing\n\n" +
                    "Features:\n" +
                    "- Create and manage payments\n" +
                    "- Payment authorization and settlement\n" +
                    "- Transaction management and refunds\n" +
                    "- Daily reconciliation processing\n\n" +
                    "Note: Ensure Oracle database is running and connection string is configured in App.config",
                    "Payment Processing System",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                // Run the main form
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fatal Error: Unable to start application.\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    "Application Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}
