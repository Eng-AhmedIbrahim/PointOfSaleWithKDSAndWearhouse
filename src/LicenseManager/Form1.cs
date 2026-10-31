using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LicenseManager
{
    public class Form1 : Form
    {
        private TabControl tabControl = null!;
        private TabPage tabLicense = null!;
        private TabPage tabDbCrypt = null!;
        private TabPage tabAdminHash = null!;

        // --- Tab 1: API License Generator ---
        private TextBox txtApiUrl = null!;
        private Button btnFetch = null!;
        private ComboBox cmbBranches = null!;
        private NumericUpDown numFull = null!; // For Full
        private NumericUpDown numBackOnly = null!; // For BackOffice Only
        private NumericUpDown numPOS = null!; // For POS Only
        private NumericUpDown numMonths = null!;
        private Button btnGenerateSave = null!;
        private TextBox txtResultTab1 = null!;

        private const string DbCryptKey = "b14ca5898a4e4133bbce2ea2315a1916";

        // --- Tab 2: Database Connection Crypt ---
        private TextBox txtPlainDbStr = null!;
        private Button btnEncryptDb = null!;
        private TextBox txtEncDbStr = null!;

        // --- Tab 3: Admin Password Hasher ---
        private TextBox txtPlainAdminPass = null!;
        private Button btnHashAdminPass = null!;
        private TextBox txtHashAdminPass = null!;

        // Colors for Dark Theme
        private readonly Color bgDark = Color.FromArgb(30, 30, 30);
        private readonly Color bgPanel = Color.FromArgb(45, 45, 48);
        private readonly Color inputBg = Color.FromArgb(60, 60, 60);
        private readonly Color accentBlue = Color.FromArgb(0, 122, 204);
        private readonly Color accentGreen = Color.FromArgb(40, 167, 69);
        private readonly Color accentTeal = Color.FromArgb(32, 201, 151);
        private readonly Color textWhite = Color.White;
        private readonly Color textGray = Color.Silver;

        private class BranchItem
        {
            public int Id { get; set; }
            public string CompanyName { get; set; } = string.Empty;
            public string BranchName { get; set; } = string.Empty;
            public override string ToString() => $"[{Id}] {CompanyName ?? "N/A"} - {BranchName ?? "N/A"}";
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "System Administrator Toolkit (PRO)";
            this.Size = new Size(800, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = bgDark;
            this.ForeColor = textWhite;
            this.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);

            tabControl = new TabControl { Dock = DockStyle.Fill, ItemSize = new Size(200, 40), SizeMode = TabSizeMode.Fixed, Padding = new Point(10, 5) };
            
            tabLicense = new TabPage("إصدار التراخيص");
            tabDbCrypt = new TabPage("تشفير الاتصال");
            tabAdminHash = new TabPage("تشفير الباسورد");

            tabControl.TabPages.Add(tabLicense);
            tabControl.TabPages.Add(tabDbCrypt);
            tabControl.TabPages.Add(tabAdminHash);
            this.Controls.Add(tabControl);

            SetupLicenseTab();
            SetupDbCryptTab();
            SetupAdminHashTab();
        }

        private void SetupLicenseTab()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = bgPanel };
            tabLicense.Controls.Add(pnl);

            var lblTitle = new Label { Text = "نظام توليد التراخيص عن بعد (عبر API)", Location = new Point(20, 15), Width = 700, Height = 40, Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = accentBlue, RightToLeft = RightToLeft.Yes };
            
            var lblApi = new Label { Text = "API Base URL:", Location = new Point(20, 70), Width = 150 };
            txtApiUrl = new TextBox { Text = "http://localhost:5050", Location = new Point(180, 68), Width = 400, BackColor = inputBg, ForeColor = textWhite, BorderStyle = BorderStyle.FixedSingle };
            
            btnFetch = new Button { Text = "Connect API", Location = new Point(590, 66), Width = 150, Height = 32, BackColor = accentGreen, ForeColor = textWhite, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnFetch.FlatAppearance.BorderSize = 0;
            btnFetch.Click += BtnFetch_Click;

            var lblBranch = new Label { Text = "Select Target Branch:", Location = new Point(20, 120), Width = 150 };
            cmbBranches = new ComboBox { Location = new Point(180, 118), Width = 560, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = inputBg, ForeColor = textWhite, FlatStyle = FlatStyle.Flat };

            var grpCounts = new GroupBox { Text = "License Requirements", Location = new Point(20, 170), Size = new Size(720, 130), BackColor = bgPanel, ForeColor = accentBlue, Font = new Font("Segoe UI", 12, FontStyle.Bold) };
            
            var lblFullCount = new Label { Text = "Full (Back+Front):", Location = new Point(20, 35), Width = 140, ForeColor = textWhite, Font = new Font("Segoe UI", 10) };
            numFull = new NumericUpDown { Value = 1, Location = new Point(160, 33), Width = 60, Minimum = 0, Maximum = 100, BackColor = inputBg, ForeColor = textWhite, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };

            var lblBackOnlyCount = new Label { Text = "BackOffice Only:", Location = new Point(240, 35), Width = 140, ForeColor = textWhite, Font = new Font("Segoe UI", 10) };
            numBackOnly = new NumericUpDown { Value = 0, Location = new Point(380, 33), Width = 60, Minimum = 0, Maximum = 100, BackColor = inputBg, ForeColor = textWhite, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };

            var lblPosCount = new Label { Text = "POS Only:", Location = new Point(460, 35), Width = 100, ForeColor = textWhite, Font = new Font("Segoe UI", 10) };
            numPOS = new NumericUpDown { Value = 0, Location = new Point(570, 33), Width = 60, Minimum = 0, Maximum = 100, BackColor = inputBg, ForeColor = textWhite, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };

            var lblMonths = new Label { Text = "Validity (Months):", Location = new Point(20, 80), Width = 140, ForeColor = textWhite, Font = new Font("Segoe UI", 10) };
            numMonths = new NumericUpDown { Value = 12, Location = new Point(160, 78), Width = 60, Minimum = 1, Maximum = 120, BackColor = inputBg, ForeColor = textWhite, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };

            grpCounts.Controls.AddRange(new Control[] { lblFullCount, numFull, lblBackOnlyCount, numBackOnly, lblPosCount, numPOS, lblMonths, numMonths });

            btnGenerateSave = new Button { 
                Text = "⚡ Generate & Securely Submit to Server", 
                Location = new Point(20, 320), 
                Width = 720, 
                Height = 60, 
                BackColor = accentBlue,
                ForeColor = textWhite,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            btnGenerateSave.FlatAppearance.BorderSize = 0;
            btnGenerateSave.Click += BtnGenerateSave_Click;

            txtResultTab1 = new TextBox { Multiline = true, ReadOnly = true, Location = new Point(20, 400), Size = new Size(720, 240), ScrollBars = ScrollBars.Vertical, BackColor = inputBg, ForeColor = accentTeal, BorderStyle = BorderStyle.FixedSingle };

            pnl.Controls.AddRange(new Control[] { lblTitle, lblApi, txtApiUrl, btnFetch, lblBranch, cmbBranches, grpCounts, btnGenerateSave, txtResultTab1 });
        }

        private void SetupDbCryptTab()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = bgPanel };
            tabDbCrypt.Controls.Add(pnl);

            var lblTitle = new Label { Text = "أداة تشفير نصوص الاتصال (Connection String)", Location = new Point(20, 20), Width = 700, Height = 40, Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = accentTeal, RightToLeft = RightToLeft.Yes };
            
            var lblPlain = new Label { Text = "Plain Text Server Connection String:", Location = new Point(20, 80), Width = 300 };
            txtPlainDbStr = new TextBox { Location = new Point(20, 110), Width = 720, Height = 120, Multiline = true, BackColor = inputBg, ForeColor = textWhite, BorderStyle = BorderStyle.FixedSingle };

            btnEncryptDb = new Button { Text = "▼ Encrypt to Base64 ▼", Location = new Point(20, 250), Width = 720, Height = 50, BackColor = accentTeal, ForeColor = Color.Black, Font = new Font("Segoe UI", 12, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnEncryptDb.FlatAppearance.BorderSize = 0;
            btnEncryptDb.Click += BtnEncryptDb_Click;

            var lblEnc = new Label { Text = "Encrypted String (Paste into appsettings.json):", Location = new Point(20, 330), Width = 400 };
            txtEncDbStr = new TextBox { Location = new Point(20, 360), Width = 720, Height = 160, Multiline = true, ReadOnly = true, BackColor = inputBg, ForeColor = accentGreen, BorderStyle = BorderStyle.FixedSingle };

            pnl.Controls.AddRange(new Control[] { lblTitle, lblPlain, txtPlainDbStr, btnEncryptDb, lblEnc, txtEncDbStr });
        }

        private void SetupAdminHashTab()
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = bgPanel };
            tabAdminHash.Controls.Add(pnl);

            var lblTitle = new Label { Text = "أداة التشفير الآمن لكلمة مرور الإدارة (SHA256)", Location = new Point(20, 20), Width = 700, Height = 40, Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = accentGreen, RightToLeft = RightToLeft.Yes };
            
            var lblPlain = new Label { Text = "Enter Administrator Plain Password:", Location = new Point(20, 80), Width = 350 };
            txtPlainAdminPass = new TextBox { Location = new Point(20, 110), Width = 720, Height = 30, BackColor = inputBg, ForeColor = textWhite, BorderStyle = BorderStyle.FixedSingle };

            btnHashAdminPass = new Button { Text = "▼ Generate Irreversible Hash ▼", Location = new Point(20, 160), Width = 720, Height = 50, BackColor = accentGreen, ForeColor = Color.Black, Font = new Font("Segoe UI", 12, FontStyle.Bold), FlatStyle = FlatStyle.Flat };
            btnHashAdminPass.FlatAppearance.BorderSize = 0;
            btnHashAdminPass.Click += BtnHashAdminPass_Click;

            var lblHash = new Label { Text = "SHA256 Hash Result:", Location = new Point(20, 240), Width = 350 };
            txtHashAdminPass = new TextBox { Location = new Point(20, 270), Width = 720, Height = 30, ReadOnly = true, BackColor = inputBg, ForeColor = accentTeal, BorderStyle = BorderStyle.FixedSingle };

            pnl.Controls.AddRange(new Control[] { lblTitle, lblPlain, txtPlainAdminPass, btnHashAdminPass, lblHash, txtHashAdminPass });
        }

        private async void BtnFetch_Click(object? sender, EventArgs e)
        {
            btnFetch.Enabled = false;
            cmbBranches.Items.Clear();
            btnGenerateSave.Enabled = false;
            try
            {
                using var client = new HttpClient();
                string url = $"{txtApiUrl.Text.TrimEnd('/')}/api/License/GetBranches";
                var response = await client.GetStringAsync(url);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var branches = JsonSerializer.Deserialize<List<BranchItem>>(response, options);
                
                if (branches != null && branches.Count > 0)
                {
                    foreach (var b in branches) cmbBranches.Items.Add(b);
                    cmbBranches.SelectedIndex = 0;
                    btnGenerateSave.Enabled = true;
                    txtResultTab1.Text = $"✅ Connection OK. Loaded {branches.Count} Branches.";
                }
                else
                {
                    txtResultTab1.Text = "❌ Connected, but no branches were returned from DB.";
                }
            }
            catch (Exception ex)
            {
                txtResultTab1.Text = $"❌ Fetch Error: {ex.Message}\r\nMake sure the POS API is running!";
            }
            finally
            {
                btnFetch.Enabled = true;
            }
        }

        private async void BtnGenerateSave_Click(object? sender, EventArgs e)
        {
            if (cmbBranches.SelectedItem == null) return;

            var branch = (BranchItem)cmbBranches.SelectedItem;

            if (numFull.Value == 0 && numBackOnly.Value == 0 && numPOS.Value == 0)
            {
                MessageBox.Show("Please specify at least 1 device count!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnGenerateSave.Enabled = false;
            txtResultTab1.Clear();
            txtResultTab1.AppendText($"[System] Starting Network Sequence to Branch {branch.BranchName}...\r\n\r\n");

            try
            {
                using var client = new HttpClient();
                int totalSuccess = 0;

                // 1. Generate FULL (BackOffice + POS) - Type 3
                for (int i = 0; i < (int)numFull.Value; i++)
                {
                    txtResultTab1.AppendText($"[API] Issuing Full License ({i + 1}/{(int)numFull.Value})...");
                    string url = $"{txtApiUrl.Text.TrimEnd('/')}/api/License/GenerateKey?branchId={branch.Id}&licenseType=3&maxDevices=1&expiryMonths={(int)numMonths.Value}";
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode) { txtResultTab1.AppendText(" SUCCESS\r\n"); totalSuccess++; }
                }

                // 2. Generate BackOffice Only - Type 2
                for (int i = 0; i < (int)numBackOnly.Value; i++)
                {
                    txtResultTab1.AppendText($"[API] Issuing BackOffice-Only License ({i + 1}/{(int)numBackOnly.Value})...");
                    string url = $"{txtApiUrl.Text.TrimEnd('/')}/api/License/GenerateKey?branchId={branch.Id}&licenseType=2&maxDevices=1&expiryMonths={(int)numMonths.Value}";
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode) { txtResultTab1.AppendText(" SUCCESS\r\n"); totalSuccess++; }
                }

                // 3. Generate POS Only - Type 1
                for (int i = 0; i < (int)numPOS.Value; i++)
                {
                    txtResultTab1.AppendText($"[API] Issuing POS-Only License ({i + 1}/{(int)numPOS.Value})...");
                    string url = $"{txtApiUrl.Text.TrimEnd('/')}/api/License/GenerateKey?branchId={branch.Id}&licenseType=1&maxDevices=1&expiryMonths={(int)numMonths.Value}";
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode) { txtResultTab1.AppendText(" SUCCESS\r\n"); totalSuccess++; }
                }

                txtResultTab1.AppendText($"\r\n[Completed] {totalSuccess} keys generated and inserted to database securely.");
                MessageBox.Show($"Generated {totalSuccess} keys into the remote system successfully!", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                txtResultTab1.AppendText($"\r\n[FATAL ERROR] {ex.Message}");
            }
            finally
            {
                btnGenerateSave.Enabled = true;
            }
        }

        private void BtnEncryptDb_Click(object? sender, EventArgs e)
        {
            string plain = txtPlainDbStr.Text.Trim();
            if (string.IsNullOrEmpty(plain)) return;

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(DbCryptKey);
                    aes.IV = new byte[16]; 
                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter sw = new StreamWriter(cs))
                            {
                                sw.Write(plain);
                            }
                        }
                        txtEncDbStr.Text = Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Encryption Error: {ex.Message}");
            }
        }

        private void BtnHashAdminPass_Click(object? sender, EventArgs e)
        {
            string plain = txtPlainAdminPass.Text.Trim();
            if (string.IsNullOrEmpty(plain)) return;

            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(plain));
            txtHashAdminPass.Text = BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }
}
