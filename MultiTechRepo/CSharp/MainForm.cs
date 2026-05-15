using System;
using System.Windows.Forms;

namespace PaymentProcessingApp
{
    /// <summary>
    /// Main form for Payment Processing Application
    /// Provides UI for payment creation, authorization, settlement, and reconciliation
    /// </summary>
    public partial class MainForm : Form
    {
        private DatabaseHelper _dbHelper;
        private const string CurrentUser = "SYSTEM_USER";

        // UI Controls
        private TabControl tabControl;
        private TabPage tabCreatePayment;
        private TabPage tabManagePayment;
        private TabPage tabReconciliation;

        // Create Payment Tab Controls
        private TextBox txtCustomerId;
        private TextBox txtPaymentMethodId;
        private TextBox txtPaymentAmount;
        private ComboBox cmbCurrencyCode;
        private ComboBox cmbPaymentType;
        private TextBox txtMerchantId;
        private TextBox txtDescription;
        private Button btnCreatePayment;
        private Label lblPaymentResult;

        // Manage Payment Tab Controls
        private TextBox txtPaymentId;
        private Button btnGetPaymentDetails;
        private DataGridView dgvPaymentDetails;
        private DataGridView dgvTransactions;
        private TextBox txtAuthCode;
        private Button btnAuthorizePayment;
        private Button btnSettlePayment;
        private Button btnCancelPayment;
        private TextBox txtRefundAmount;
        private Button btnRefundPayment;
        private Label lblManageResult;

        // Reconciliation Tab Controls
        private DateTimePicker dtpReconDate;
        private TextBox txtGatewayFile;
        private Button btnPerformRecon;
        private DataGridView dgvReconciliationReport;
        private Button btnGetReconReport;
        private DateTimePicker dtpReconFromDate;
        private DateTimePicker dtpReconToDate;
        private Label lblReconResult;

        public MainForm()
        {
            InitializeComponent();
            _dbHelper = new DatabaseHelper();
            SetupForm();
        }

        private void SetupForm()
        {
            this.Text = "Payment Processing System - Enterprise Demo";
            this.Size = new System.Drawing.Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;

            tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            SetupCreatePaymentTab();
            SetupManagePaymentTab();
            SetupReconciliationTab();

            this.Controls.Add(tabControl);
        }

        #region Create Payment Tab

        private void SetupCreatePaymentTab()
        {
            tabCreatePayment = new TabPage("Create Payment");
            tabControl.TabPages.Add(tabCreatePayment);

            int labelX = 20, controlX = 180, yPos = 20, yIncrement = 40;

            // Customer ID
            AddLabel(tabCreatePayment, "Customer ID:", labelX, yPos);
            txtCustomerId = AddTextBox(tabCreatePayment, controlX, yPos);
            yPos += yIncrement;

            // Payment Method ID
            AddLabel(tabCreatePayment, "Payment Method ID:", labelX, yPos);
            txtPaymentMethodId = AddTextBox(tabCreatePayment, controlX, yPos);
            yPos += yIncrement;

            // Payment Amount
            AddLabel(tabCreatePayment, "Payment Amount:", labelX, yPos);
            txtPaymentAmount = AddTextBox(tabCreatePayment, controlX, yPos);
            yPos += yIncrement;

            // Currency Code
            AddLabel(tabCreatePayment, "Currency Code:", labelX, yPos);
            cmbCurrencyCode = AddComboBox(tabCreatePayment, controlX, yPos, new[] { "USD", "EUR", "GBP", "CAD" });
            cmbCurrencyCode.SelectedIndex = 0;
            yPos += yIncrement;

            // Payment Type
            AddLabel(tabCreatePayment, "Payment Type:", labelX, yPos);
            cmbPaymentType = AddComboBox(tabCreatePayment, controlX, yPos, new[] { "PURCHASE", "AUTHORIZATION", "REFUND" });
            cmbPaymentType.SelectedIndex = 0;
            yPos += yIncrement;

            // Merchant ID
            AddLabel(tabCreatePayment, "Merchant ID:", labelX, yPos);
            txtMerchantId = AddTextBox(tabCreatePayment, controlX, yPos);
            txtMerchantId.Text = "MERCH001";
            yPos += yIncrement;

            // Description
            AddLabel(tabCreatePayment, "Description:", labelX, yPos);
            txtDescription = AddTextBox(tabCreatePayment, controlX, yPos, 300);
            yPos += yIncrement;

            // Create Payment Button
            btnCreatePayment = new Button
            {
                Text = "Create Payment",
                Location = new System.Drawing.Point(controlX, yPos),
                Size = new System.Drawing.Size(150, 35)
            };
            btnCreatePayment.Click += BtnCreatePayment_Click;
            tabCreatePayment.Controls.Add(btnCreatePayment);
            yPos += yIncrement + 10;

            // Result Label
            lblPaymentResult = new Label
            {
                Location = new System.Drawing.Point(labelX, yPos),
                Size = new System.Drawing.Size(700, 100),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = System.Drawing.Color.LightYellow,
                Font = new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Bold)
            };
            tabCreatePayment.Controls.Add(lblPaymentResult);
        }

        private void BtnCreatePayment_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate inputs
                if (!long.TryParse(txtCustomerId.Text, out long customerId))
                {
                    MessageBox.Show("Please enter a valid Customer ID", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!long.TryParse(txtPaymentMethodId.Text, out long paymentMethodId))
                {
                    MessageBox.Show("Please enter a valid Payment Method ID", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!decimal.TryParse(txtPaymentAmount.Text, out decimal paymentAmount))
                {
                    MessageBox.Show("Please enter a valid Payment Amount", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Call stored procedure through DatabaseHelper
                var result = _dbHelper.CreatePayment(
                    customerId,
                    paymentMethodId,
                    paymentAmount,
                    cmbCurrencyCode.SelectedItem.ToString(),
                    cmbPaymentType.SelectedItem.ToString(),
                    txtMerchantId.Text,
                    txtDescription.Text,
                    CurrentUser,
                    out long paymentId,
                    out string referenceNumber
                );

                if (result.IsSuccess)
                {
                    lblPaymentResult.Text = $"SUCCESS!\n\n" +
                                          $"Payment ID: {paymentId}\n" +
                                          $"Reference Number: {referenceNumber}\n" +
                                          $"Status: PENDING\n" +
                                          $"Amount: ${paymentAmount:N2} {cmbCurrencyCode.SelectedItem}\n\n" +
                                          $"{result.ErrorMessage}";
                    lblPaymentResult.ForeColor = System.Drawing.Color.Green;

                    MessageBox.Show($"Payment created successfully!\n\nPayment ID: {paymentId}\nReference: {referenceNumber}",
                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Clear form
                    ClearCreatePaymentForm();
                }
                else
                {
                    lblPaymentResult.Text = $"ERROR:\n\n{result.ErrorMessage}";
                    lblPaymentResult.ForeColor = System.Drawing.Color.Red;
                    MessageBox.Show(result.ErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                lblPaymentResult.Text = $"EXCEPTION:\n\n{ex.Message}";
                lblPaymentResult.ForeColor = System.Drawing.Color.Red;
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearCreatePaymentForm()
        {
            txtCustomerId.Clear();
            txtPaymentMethodId.Clear();
            txtPaymentAmount.Clear();
            txtDescription.Clear();
            cmbCurrencyCode.SelectedIndex = 0;
            cmbPaymentType.SelectedIndex = 0;
        }

        #endregion

        #region Manage Payment Tab

        private void SetupManagePaymentTab()
        {
            tabManagePayment = new TabPage("Manage Payment");
            tabControl.TabPages.Add(tabManagePayment);

            int labelX = 20, controlX = 150, yPos = 20;

            // Payment ID and Get Details
            AddLabel(tabManagePayment, "Payment ID:", labelX, yPos);
            txtPaymentId = AddTextBox(tabManagePayment, controlX, yPos, 150);

            btnGetPaymentDetails = new Button
            {
                Text = "Get Details",
                Location = new System.Drawing.Point(controlX + 160, yPos - 2),
                Size = new System.Drawing.Size(100, 25)
            };
            btnGetPaymentDetails.Click += BtnGetPaymentDetails_Click;
            tabManagePayment.Controls.Add(btnGetPaymentDetails);
            yPos += 40;

            // Payment Details Grid
            dgvPaymentDetails = new DataGridView
            {
                Location = new System.Drawing.Point(labelX, yPos),
                Size = new System.Drawing.Size(1140, 120),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            tabManagePayment.Controls.Add(dgvPaymentDetails);
            yPos += 130;

            // Transactions Grid
            AddLabel(tabManagePayment, "Transactions:", labelX, yPos);
            yPos += 25;

            dgvTransactions = new DataGridView
            {
                Location = new System.Drawing.Point(labelX, yPos),
                Size = new System.Drawing.Size(1140, 150),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            tabManagePayment.Controls.Add(dgvTransactions);
            yPos += 160;

            // Action Buttons
            int buttonY = yPos;
            int buttonX = labelX;

            // Authorize Payment
            AddLabel(tabManagePayment, "Auth Code:", buttonX, buttonY);
            txtAuthCode = AddTextBox(tabManagePayment, buttonX + 80, buttonY, 150);
            btnAuthorizePayment = new Button
            {
                Text = "Authorize Payment",
                Location = new System.Drawing.Point(buttonX + 240, buttonY - 2),
                Size = new System.Drawing.Size(130, 25)
            };
            btnAuthorizePayment.Click += BtnAuthorizePayment_Click;
            tabManagePayment.Controls.Add(btnAuthorizePayment);
            buttonY += 35;

            // Settle Payment
            btnSettlePayment = new Button
            {
                Text = "Settle Payment",
                Location = new System.Drawing.Point(buttonX, buttonY),
                Size = new System.Drawing.Size(130, 25)
            };
            btnSettlePayment.Click += BtnSettlePayment_Click;
            tabManagePayment.Controls.Add(btnSettlePayment);

            // Cancel Payment
            btnCancelPayment = new Button
            {
                Text = "Cancel Payment",
                Location = new System.Drawing.Point(buttonX + 140, buttonY),
                Size = new System.Drawing.Size(130, 25)
            };
            btnCancelPayment.Click += BtnCancelPayment_Click;
            tabManagePayment.Controls.Add(btnCancelPayment);
            buttonY += 35;

            // Refund
            AddLabel(tabManagePayment, "Refund Amt:", buttonX, buttonY);
            txtRefundAmount = AddTextBox(tabManagePayment, buttonX + 80, buttonY, 150);
            btnRefundPayment = new Button
            {
                Text = "Process Refund",
                Location = new System.Drawing.Point(buttonX + 240, buttonY - 2),
                Size = new System.Drawing.Size(130, 25)
            };
            btnRefundPayment.Click += BtnRefundPayment_Click;
            tabManagePayment.Controls.Add(btnRefundPayment);
            buttonY += 40;

            // Result Label
            lblManageResult = new Label
            {
                Location = new System.Drawing.Point(labelX, buttonY),
                Size = new System.Drawing.Size(700, 60),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = System.Drawing.Color.LightYellow,
                Font = new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Bold)
            };
            tabManagePayment.Controls.Add(lblManageResult);
        }

        private void BtnGetPaymentDetails_Click(object sender, EventArgs e)
        {
            try
            {
                if (!long.TryParse(txtPaymentId.Text, out long paymentId))
                {
                    MessageBox.Show("Please enter a valid Payment ID", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var payments = _dbHelper.GetPaymentDetails(paymentId, out var result);

                if (result.IsSuccess && payments.Count > 0)
                {
                    dgvPaymentDetails.DataSource = payments;
                    lblManageResult.Text = $"Payment details loaded successfully";
                    lblManageResult.ForeColor = System.Drawing.Color.Green;

                    // Load transactions
                    var transactions = _dbHelper.GetPaymentTransactions(paymentId, out var txnResult);
                    dgvTransactions.DataSource = transactions;
                }
                else
                {
                    lblManageResult.Text = $"Error: {result.ErrorMessage}";
                    lblManageResult.ForeColor = System.Drawing.Color.Red;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAuthorizePayment_Click(object sender, EventArgs e)
        {
            try
            {
                if (!long.TryParse(txtPaymentId.Text, out long paymentId))
                {
                    MessageBox.Show("Please enter a valid Payment ID", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string authCode = string.IsNullOrEmpty(txtAuthCode.Text) ? "AUTH" + DateTime.Now.Ticks : txtAuthCode.Text;
                string gatewayResponse = "Approved by Gateway";

                var result = _dbHelper.AuthorizePayment(paymentId, authCode, gatewayResponse, CurrentUser);

                if (result.IsSuccess)
                {
                    lblManageResult.Text = $"Payment authorized successfully! Auth Code: {authCode}";
                    lblManageResult.ForeColor = System.Drawing.Color.Green;
                    MessageBox.Show(result.ErrorMessage, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    BtnGetPaymentDetails_Click(sender, e); // Refresh
                }
                else
                {
                    lblManageResult.Text = $"Error: {result.ErrorMessage}";
                    lblManageResult.ForeColor = System.Drawing.Color.Red;
                    MessageBox.Show(result.ErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSettlePayment_Click(object sender, EventArgs e)
        {
            try
            {
                if (!long.TryParse(txtPaymentId.Text, out long paymentId))
                {
                    MessageBox.Show("Please enter a valid Payment ID", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Get payment amount from grid
                if (dgvPaymentDetails.Rows.Count == 0)
                {
                    MessageBox.Show("Please load payment details first", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                decimal amount = Convert.ToDecimal(dgvPaymentDetails.Rows[0].Cells["PaymentAmount"].Value);

                var result = _dbHelper.SettlePayment(paymentId, amount, CurrentUser);

                if (result.IsSuccess)
                {
                    lblManageResult.Text = $"Payment settled successfully!";
                    lblManageResult.ForeColor = System.Drawing.Color.Green;
                    MessageBox.Show(result.ErrorMessage, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    BtnGetPaymentDetails_Click(sender, e); // Refresh
                }
                else
                {
                    lblManageResult.Text = $"Error: {result.ErrorMessage}";
                    lblManageResult.ForeColor = System.Drawing.Color.Red;
                    MessageBox.Show(result.ErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCancelPayment_Click(object sender, EventArgs e)
        {
            try
            {
                if (!long.TryParse(txtPaymentId.Text, out long paymentId))
                {
                    MessageBox.Show("Please enter a valid Payment ID", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string reason = "Cancelled by user request";

                var result = _dbHelper.CancelPayment(paymentId, reason, CurrentUser);

                if (result.IsSuccess)
                {
                    lblManageResult.Text = $"Payment cancelled successfully!";
                    lblManageResult.ForeColor = System.Drawing.Color.Green;
                    MessageBox.Show(result.ErrorMessage, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    BtnGetPaymentDetails_Click(sender, e); // Refresh
                }
                else
                {
                    lblManageResult.Text = $"Error: {result.ErrorMessage}";
                    lblManageResult.ForeColor = System.Drawing.Color.Red;
                    MessageBox.Show(result.ErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRefundPayment_Click(object sender, EventArgs e)
        {
            try
            {
                if (!long.TryParse(txtPaymentId.Text, out long paymentId))
                {
                    MessageBox.Show("Please enter a valid Payment ID", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!decimal.TryParse(txtRefundAmount.Text, out decimal refundAmount))
                {
                    MessageBox.Show("Please enter a valid Refund Amount", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var result = _dbHelper.CreateRefundTransaction(paymentId, refundAmount, "Customer requested refund", CurrentUser, out long transactionId);

                if (result.IsSuccess)
                {
                    lblManageResult.Text = $"Refund processed successfully! Transaction ID: {transactionId}";
                    lblManageResult.ForeColor = System.Drawing.Color.Green;
                    MessageBox.Show($"{result.ErrorMessage}\nTransaction ID: {transactionId}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    BtnGetPaymentDetails_Click(sender, e); // Refresh
                }
                else
                {
                    lblManageResult.Text = $"Error: {result.ErrorMessage}";
                    lblManageResult.ForeColor = System.Drawing.Color.Red;
                    MessageBox.Show(result.ErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Reconciliation Tab

        private void SetupReconciliationTab()
        {
            tabReconciliation = new TabPage("Reconciliation");
            tabControl.TabPages.Add(tabReconciliation);

            int labelX = 20, controlX = 180, yPos = 20;

            // Daily Reconciliation Section
            AddLabel(tabReconciliation, "Daily Reconciliation", labelX, yPos, true);
            yPos += 30;

            AddLabel(tabReconciliation, "Reconciliation Date:", labelX, yPos);
            dtpReconDate = new DateTimePicker
            {
                Location = new System.Drawing.Point(controlX, yPos),
                Size = new System.Drawing.Size(200, 25),
                Format = DateTimePickerFormat.Short
            };
            tabReconciliation.Controls.Add(dtpReconDate);
            yPos += 35;

            AddLabel(tabReconciliation, "Gateway File:", labelX, yPos);
            txtGatewayFile = AddTextBox(tabReconciliation, controlX, yPos, 300);
            txtGatewayFile.Text = "gateway_settlement_" + DateTime.Now.ToString("yyyyMMdd") + ".dat";
            yPos += 35;

            btnPerformRecon = new Button
            {
                Text = "Perform Daily Reconciliation",
                Location = new System.Drawing.Point(controlX, yPos),
                Size = new System.Drawing.Size(200, 30)
            };
            btnPerformRecon.Click += BtnPerformRecon_Click;
            tabReconciliation.Controls.Add(btnPerformRecon);
            yPos += 50;

            // Reconciliation Report Section
            AddLabel(tabReconciliation, "Reconciliation Report", labelX, yPos, true);
            yPos += 30;

            AddLabel(tabReconciliation, "From Date:", labelX, yPos);
            dtpReconFromDate = new DateTimePicker
            {
                Location = new System.Drawing.Point(controlX, yPos),
                Size = new System.Drawing.Size(150, 25),
                Format = DateTimePickerFormat.Short
            };
            dtpReconFromDate.Value = DateTime.Now.AddDays(-30);
            tabReconciliation.Controls.Add(dtpReconFromDate);

            AddLabel(tabReconciliation, "To Date:", controlX + 180, yPos);
            dtpReconToDate = new DateTimePicker
            {
                Location = new System.Drawing.Point(controlX + 260, yPos),
                Size = new System.Drawing.Size(150, 25),
                Format = DateTimePickerFormat.Short
            };
            tabReconciliation.Controls.Add(dtpReconToDate);
            yPos += 35;

            btnGetReconReport = new Button
            {
                Text = "Get Reconciliation Report",
                Location = new System.Drawing.Point(controlX, yPos),
                Size = new System.Drawing.Size(200, 30)
            };
            btnGetReconReport.Click += BtnGetReconReport_Click;
            tabReconciliation.Controls.Add(btnGetReconReport);
            yPos += 45;

            // Reconciliation Report Grid
            dgvReconciliationReport = new DataGridView
            {
                Location = new System.Drawing.Point(labelX, yPos),
                Size = new System.Drawing.Size(1140, 300),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells
            };
            tabReconciliation.Controls.Add(dgvReconciliationReport);
            yPos += 310;

            // Result Label
            lblReconResult = new Label
            {
                Location = new System.Drawing.Point(labelX, yPos),
                Size = new System.Drawing.Size(700, 60),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = System.Drawing.Color.LightYellow,
                Font = new System.Drawing.Font("Arial", 9, System.Drawing.FontStyle.Bold)
            };
            tabReconciliation.Controls.Add(lblReconResult);
        }

        private void BtnPerformRecon_Click(object sender, EventArgs e)
        {
            try
            {
                var result = _dbHelper.PerformDailyReconciliation(
                    dtpReconDate.Value,
                    txtGatewayFile.Text,
                    CurrentUser,
                    out int matched,
                    out int mismatched
                );

                if (result.IsSuccess)
                {
                    lblReconResult.Text = $"Reconciliation completed!\n" +
                                        $"Matched: {matched}, Mismatched: {mismatched}";
                    lblReconResult.ForeColor = System.Drawing.Color.Green;
                    MessageBox.Show($"{result.ErrorMessage}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    lblReconResult.Text = $"Error: {result.ErrorMessage}";
                    lblReconResult.ForeColor = System.Drawing.Color.Red;
                    MessageBox.Show(result.ErrorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnGetReconReport_Click(object sender, EventArgs e)
        {
            try
            {
                var reconLogs = _dbHelper.GetReconciliationReport(
                    dtpReconFromDate.Value,
                    dtpReconToDate.Value,
                    null,
                    out var result
                );

                if (result.IsSuccess)
                {
                    dgvReconciliationReport.DataSource = reconLogs;
                    lblReconResult.Text = $"Loaded {reconLogs.Count} reconciliation records";
                    lblReconResult.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    lblReconResult.Text = $"Error: {result.ErrorMessage}";
                    lblReconResult.ForeColor = System.Drawing.Color.Red;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Helper Methods

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ResumeLayout(false);
        }

        private Label AddLabel(Control parent, string text, int x, int y, bool isBold = false)
        {
            var label = new Label
            {
                Text = text,
                Location = new System.Drawing.Point(x, y),
                Size = new System.Drawing.Size(150, 20),
                AutoSize = true
            };

            if (isBold)
            {
                label.Font = new System.Drawing.Font("Arial", 10, System.Drawing.FontStyle.Bold);
            }

            parent.Controls.Add(label);
            return label;
        }

        private TextBox AddTextBox(Control parent, int x, int y, int width = 200)
        {
            var textBox = new TextBox
            {
                Location = new System.Drawing.Point(x, y),
                Size = new System.Drawing.Size(width, 25)
            };
            parent.Controls.Add(textBox);
            return textBox;
        }

        private ComboBox AddComboBox(Control parent, int x, int y, string[] items)
        {
            var comboBox = new ComboBox
            {
                Location = new System.Drawing.Point(x, y),
                Size = new System.Drawing.Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            comboBox.Items.AddRange(items);
            parent.Controls.Add(comboBox);
            return comboBox;
        }

        #endregion
    }
}
