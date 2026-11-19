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
        private TabPage tabResetOrder = null!;
        private TabPage tabDbCrypt = null!;
        private TabPage tabAdminHash = null!;

        // --- Tab 1: API License Generator ---
        private TextBox txtApiUrl = null!;
        private Button btnFetch = null!;
        private ComboBox cmbBranches = null!;
        private NumericUpDown numFull = null!; // For Full
        private NumericUpDown numBackOnly = null!; // For BackOffice Only
        private NumericUpDown numPOS = null!; // For POS Only
        private NumericUpDown numWarehouse = null!; // For Warehouse Only
        private NumericUpDown numMonths = null!;
        private Button btnGenerateSave = null!;
        private TextBox txtResultTab1 = null!;

        // --- Tab 2: Reset Order Count ---
        private TextBox txtApiUrlReset = null!;
        private Button btnFetchReset = null!;
        private ComboBox cmbBranchesReset = null!;
        private Label lblCurrentPosDate = null!;
        private Label lblCurrentOrderNumber = null!;
        private Label lblScriptStatusBadge = null!;
        private CheckBox chkEnableAutoReset = null!;
        private NumericUpDown numMaxOrders = null!;
        private CheckBox chkResetCurrentNow = null!;
        private Button btnApplyResetConfig = null!;
        private NumericUpDown numManualCounter = null!;
        private Button btnSetManualCounter = null!;
        private TextBox txtResultTabReset = null!;

        private const string DbCryptKey = "b14ca5898a4e4133bbce2ea2315a1916";

        // --- Tab 3: Database Connection Crypt ---
        private TextBox txtPlainDbStr = null!;
        private Button btnEncryptDb = null!;
        private TextBox txtEncDbStr = null!;

        // --- Tab 4: Admin Password Hasher ---
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

        private class OrderCounterStatusResponse
        {
            public int BranchId { get; set; }
            public string PosDate { get; set; } = string.Empty;
            public string StoreDate { get; set; } = string.Empty;
            public int CurrentOrderNumber { get; set; }
            public bool IsAutoResetEnabled { get; set; }
            public int MaxOrderLimit { get; set; }
            public bool TriggerInstalled { get; set; }
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "System Administrator Toolkit (PRO)";
            this.Size = new Size(800, 760);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = bgDark;
            this.ForeColor = textWhite;
            this.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);

            tabControl = new TabControl { Dock = DockStyle.Fill, ItemSize = new Size(180, 40), SizeMode = TabSizeMode.Fixed, Padding = new Point(10, 5) };
            
            tabLicense = new TabPage("إصدار التراخيص");
            tabResetOrder = new TabPage("تصفير العداد");
            tabDbCrypt = new TabPage("تشفير الاتصال");
            tabAdminHash = new TabPage("تشفير الباسورد");

            tabControl.TabPages.Add(tabLicense);
            tabControl.TabPages.Add(tabResetOrder);
            tabControl.TabPages.Add(tabDbCrypt);
            tabControl.TabPages.Add(tabAdminHash);
            this.Controls.Add(tabControl);

            SetupLicenseTab();
            SetupOrderResetTab();
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
            
            var lblFullCount = new Label { Text = "Full (Back+Front):", Location = new Point(10, 35), Width = 130, ForeColor = textWhite, Font = new Font("Segoe UI", 10) };
            numFull = new NumericUpDown { Value = 1, Location = new Point(140, 33), Width = 55, Minimum = 0, Maximum = 100, BackColor = Color.FromArgb(230, 240, 255), ForeColor = Color.FromArgb(0, 60, 130), BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10, FontStyle.Bold) };

            var lblBackOnlyCount = new Label { Text = "BackOffice Only:", Location = new Point(205, 35), Width = 120, ForeColor = textWhite, Font = new Font("Segoe UI", 10) };
            numBackOnly = new NumericUpDown { Value = 0, Location = new Point(325, 33), Width = 55, Minimum = 0, Maximum = 100, BackColor = Color.FromArgb(230, 240, 255), ForeColor = Color.FromArgb(0, 60, 130), BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10, FontStyle.Bold) };

            var lblPosCount = new Label { Text = "POS Only:", Location = new Point(390, 35), Width = 85, ForeColor = textWhite, Font = new Font("Segoe UI", 10) };
            numPOS = new NumericUpDown { Value = 0, Location = new Point(475, 33), Width = 55, Minimum = 0, Maximum = 100, BackColor = Color.FromArgb(230, 240, 255), ForeColor = Color.FromArgb(0, 60, 130), BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10, FontStyle.Bold) };

            var lblWarehouseCount = new Label { Text = "Warehouse Only:", Location = new Point(540, 35), Width = 120, ForeColor = accentTeal, Font = new Font("Segoe UI", 10) };
            numWarehouse = new NumericUpDown { Value = 0, Location = new Point(660, 33), Width = 50, Minimum = 0, Maximum = 100, BackColor = Color.FromArgb(220, 255, 245), ForeColor = Color.FromArgb(0, 100, 80), BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10, FontStyle.Bold) };

            var lblMonths = new Label { Text = "Validity (Months):", Location = new Point(10, 80), Width = 140, ForeColor = accentGreen, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            numMonths = new NumericUpDown { Value = 12, Location = new Point(140, 78), Width = 70, Minimum = 1, Maximum = 120, BackColor = Color.FromArgb(230, 255, 240), ForeColor = Color.FromArgb(0, 100, 50), BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 11, FontStyle.Bold) };

            grpCounts.Controls.AddRange(new Control[] { lblFullCount, numFull, lblBackOnlyCount, numBackOnly, lblPosCount, numPOS, lblWarehouseCount, numWarehouse, lblMonths, numMonths });

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

            txtResultTab1 = new TextBox { Multiline = true, ReadOnly = true, Location = new Point(20, 400), Size = new Size(720, 280), ScrollBars = ScrollBars.Vertical, BackColor = inputBg, ForeColor = accentTeal, BorderStyle = BorderStyle.FixedSingle };

            pnl.Controls.AddRange(new Control[] { lblTitle, lblApi, txtApiUrl, btnFetch, lblBranch, cmbBranches, grpCounts, btnGenerateSave, txtResultTab1 });
        }

        private void SetupOrderResetTab()
        {
            tabResetOrder.RightToLeft = RightToLeft.Yes;
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = bgPanel, RightToLeft = RightToLeft.Yes };
            tabResetOrder.Controls.Add(pnl);

            var lblTitle = new Label { 
                Text = "التحكم في عداد الأوردرات والتصفير التلقائي (Reset Order Counter)", 
                Location = new Point(20, 12), 
                Width = 720, 
                Height = 35, 
                Font = new Font("Segoe UI", 14, FontStyle.Bold), 
                ForeColor = accentTeal, 
                TextAlign = ContentAlignment.MiddleRight
            };

            // API Row (RTL: Label on Right, TextBox in Middle, Button on Left)
            var lblApi = new Label { Text = "رابط السيرفر (API):", Location = new Point(590, 55), Width = 150, Font = new Font("Segoe UI", 10, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight };
            txtApiUrlReset = new TextBox { Text = "http://localhost:5050", Location = new Point(220, 53), Width = 365, BackColor = inputBg, ForeColor = textWhite, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10), RightToLeft = RightToLeft.No };
            
            btnFetchReset = new Button { Text = "🔍 فحص العداد والاتصال", Location = new Point(20, 51), Width = 190, Height = 32, BackColor = accentGreen, ForeColor = textWhite, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
            btnFetchReset.FlatAppearance.BorderSize = 0;
            btnFetchReset.Click += BtnFetchReset_Click;

            // Branch Row (RTL: Label on Right, ComboBox on Left/Middle)
            var lblBranch = new Label { Text = "الفرع المستهدف:", Location = new Point(590, 93), Width = 150, Font = new Font("Segoe UI", 10, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight };
            cmbBranchesReset = new ComboBox { Location = new Point(20, 91), Width = 565, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = inputBg, ForeColor = textWhite, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10), RightToLeft = RightToLeft.Yes };
            cmbBranchesReset.SelectedIndexChanged += async (s, e) => await RefreshOrderCounterStatusAsync();

            // Status GroupBox (RTL)
            var grpStatus = new GroupBox { Text = "حالة العداد الحالية بقاعدة البيانات (dbo.AppDates)", Location = new Point(20, 130), Size = new Size(720, 92), BackColor = bgPanel, ForeColor = accentBlue, Font = new Font("Segoe UI", 11, FontStyle.Bold), RightToLeft = RightToLeft.Yes };
            
            var lblDateTitle = new Label { Text = "تاريخ اليوم (POS Date):", Location = new Point(530, 25), Width = 175, ForeColor = textWhite, Font = new Font("Segoe UI", 10), TextAlign = ContentAlignment.MiddleRight };
            lblCurrentPosDate = new Label { Text = "--", Location = new Point(410, 25), Width = 120, ForeColor = accentTeal, Font = new Font("Segoe UI", 11, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, RightToLeft = RightToLeft.No };

            var lblCounterTitle = new Label { Text = "رقم الأوردر الحالي (Current #):", Location = new Point(140, 22), Width = 260, ForeColor = textWhite, Font = new Font("Segoe UI", 11, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight };
            lblCurrentOrderNumber = new Label { Text = "--", Location = new Point(20, 16), Width = 115, Height = 38, ForeColor = Color.Yellow, Font = new Font("Segoe UI", 20, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, RightToLeft = RightToLeft.No };

            var lblStatusTitle = new Label { Text = "حالة اسكريبت الداتا بيز:", Location = new Point(510, 58), Width = 195, ForeColor = textWhite, Font = new Font("Segoe UI", 10), TextAlign = ContentAlignment.MiddleRight };
            lblScriptStatusBadge = new Label { Text = "[فحص الحالة مطلوب]", Location = new Point(20, 58), Width = 485, ForeColor = textGray, Font = new Font("Segoe UI", 10, FontStyle.Bold), TextAlign = ContentAlignment.MiddleRight };

            grpStatus.Controls.AddRange(new Control[] { lblDateTitle, lblCurrentPosDate, lblCounterTitle, lblCurrentOrderNumber, lblStatusTitle, lblScriptStatusBadge });

            // Auto Reset Configuration GroupBox (RTL)
            var grpAutoReset = new GroupBox { Text = "إعدادات التصفير التلقائي الدوري (Cyclic Auto-Reset)", Location = new Point(20, 230), Size = new Size(720, 138), BackColor = bgPanel, ForeColor = accentTeal, Font = new Font("Segoe UI", 11, FontStyle.Bold), RightToLeft = RightToLeft.Yes };

            chkEnableAutoReset = new CheckBox { Text = "تفعيل خاصية التصفير التلقائي الدوري بعد عدد محدد من الأوردرات", Location = new Point(20, 26), Width = 680, ForeColor = textWhite, Font = new Font("Segoe UI", 10, FontStyle.Bold), RightToLeft = RightToLeft.Yes };

            var lblMax = new Label { Text = "إعادة التصفير إلى 0 بعد كل:", Location = new Point(490, 60), Width = 210, ForeColor = textWhite, Font = new Font("Segoe UI", 10), TextAlign = ContentAlignment.MiddleRight };
            numMaxOrders = new NumericUpDown { Value = 100, Minimum = 1, Maximum = 99999, Location = new Point(390, 58), Width = 95, BackColor = Color.FromArgb(230, 255, 240), ForeColor = Color.FromArgb(0, 100, 50), BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 11, FontStyle.Bold), RightToLeft = RightToLeft.No };
            var lblOrdersWord = new Label { Text = "أوردر (Threshold Limit)", Location = new Point(245, 60), Width = 140, ForeColor = textGray, Font = new Font("Segoe UI", 9.5f), TextAlign = ContentAlignment.MiddleRight };

            chkResetCurrentNow = new CheckBox { Text = "تصفير العداد الحالي فوراً إلى 0 الآن", Location = new Point(20, 60), Width = 220, ForeColor = Color.FromArgb(255, 180, 80), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), RightToLeft = RightToLeft.Yes };

            btnApplyResetConfig = new Button { 
                Text = "⚡ حفظ الإعداد وتفعيل الاسكريبت في الداتا بيز (Apply & Execute Script in DB)", 
                Location = new Point(20, 94), 
                Width = 680, 
                Height = 34, 
                BackColor = accentBlue, 
                ForeColor = textWhite, 
                FlatStyle = FlatStyle.Flat, 
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), 
                Cursor = Cursors.Hand, 
                Enabled = false 
            };
            btnApplyResetConfig.FlatAppearance.BorderSize = 0;
            btnApplyResetConfig.Click += BtnApplyResetConfig_Click;

            grpAutoReset.Controls.AddRange(new Control[] { chkEnableAutoReset, lblMax, numMaxOrders, lblOrdersWord, chkResetCurrentNow, btnApplyResetConfig });

            // Manual Counter Override GroupBox (RTL)
            var grpManual = new GroupBox { Text = "تعديل قيمة العداد يدوياً (Manual Counter Override)", Location = new Point(20, 375), Size = new Size(720, 72), BackColor = bgPanel, ForeColor = accentGreen, Font = new Font("Segoe UI", 11, FontStyle.Bold), RightToLeft = RightToLeft.Yes };

            var lblSetManual = new Label { Text = "تعيين قيمة CurrentOrderNumber يدوياً إلى:", Location = new Point(350, 26), Width = 350, ForeColor = textWhite, Font = new Font("Segoe UI", 10), TextAlign = ContentAlignment.MiddleRight };
            numManualCounter = new NumericUpDown { Value = 0, Minimum = 0, Maximum = 999999, Location = new Point(230, 24), Width = 110, BackColor = Color.FromArgb(230, 240, 255), ForeColor = Color.FromArgb(0, 60, 130), BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 11, FontStyle.Bold), RightToLeft = RightToLeft.No };

            btnSetManualCounter = new Button { 
                Text = "تحديث العداد الآن", 
                Location = new Point(20, 22), 
                Width = 200, 
                Height = 34, 
                BackColor = Color.FromArgb(70, 70, 75), 
                ForeColor = textWhite, 
                FlatStyle = FlatStyle.Flat, 
                Font = new Font("Segoe UI", 10, FontStyle.Bold), 
                Cursor = Cursors.Hand, 
                Enabled = false 
            };
            btnSetManualCounter.FlatAppearance.BorderSize = 0;
            btnSetManualCounter.Click += BtnSetManualCounter_Click;

            grpManual.Controls.AddRange(new Control[] { lblSetManual, numManualCounter, btnSetManualCounter });

            // Results Log Console
            txtResultTabReset = new TextBox { Multiline = true, ReadOnly = true, Location = new Point(20, 455), Size = new Size(720, 235), ScrollBars = ScrollBars.Vertical, BackColor = inputBg, ForeColor = accentTeal, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Consolas", 10), RightToLeft = RightToLeft.No };

            pnl.Controls.AddRange(new Control[] { lblTitle, lblApi, txtApiUrlReset, btnFetchReset, lblBranch, cmbBranchesReset, grpStatus, grpAutoReset, grpManual, txtResultTabReset });
        }

        private async void BtnFetchReset_Click(object? sender, EventArgs e)
        {
            btnFetchReset.Enabled = false;
            cmbBranchesReset.Items.Clear();
            txtResultTabReset.Clear();
            txtResultTabReset.AppendText($"[System] الاتصال بالسيرفر {txtApiUrlReset.Text.TrimEnd('/')}...\r\n");

            try
            {
                using var client = new HttpClient();
                string branchesUrl = $"{txtApiUrlReset.Text.TrimEnd('/')}/api/License/GetBranches";
                var response = await client.GetStringAsync(branchesUrl);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var branches = JsonSerializer.Deserialize<List<BranchItem>>(response, options);

                if (branches != null && branches.Count > 0)
                {
                    foreach (var b in branches) cmbBranchesReset.Items.Add(b);
                    cmbBranchesReset.SelectedIndex = 0;
                    btnApplyResetConfig.Enabled = true;
                    btnSetManualCounter.Enabled = true;
                    txtResultTabReset.AppendText($"✅ تم تحميل {branches.Count} فرع بنجاح.\r\n");
                    await RefreshOrderCounterStatusAsync();
                }
                else
                {
                    txtResultTabReset.AppendText("❌ تم الاتصال بالسيرفر لكن لم يتم العثور على فروع.\r\n");
                }
            }
            catch (Exception ex)
            {
                txtResultTabReset.AppendText($"❌ خطأ أثناء الاتصال: {ex.Message}\r\nتأكد أن الـ API يعمل بشكل صحيح.\r\n");
            }
            finally
            {
                btnFetchReset.Enabled = true;
            }
        }

        private async Task RefreshOrderCounterStatusAsync()
        {
            if (cmbBranchesReset.SelectedItem == null) return;
            var branch = (BranchItem)cmbBranchesReset.SelectedItem;

            try
            {
                using var client = new HttpClient();
                string statusUrl = $"{txtApiUrlReset.Text.TrimEnd('/')}/api/License/GetOrderCounterStatus?branchId={branch.Id}";
                var statusRes = await client.GetStringAsync(statusUrl);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var status = JsonSerializer.Deserialize<OrderCounterStatusResponse>(statusRes, options);

                if (status != null)
                {
                    lblCurrentPosDate.Text = status.PosDate;
                    lblCurrentOrderNumber.Text = status.CurrentOrderNumber.ToString();
                    chkEnableAutoReset.Checked = status.IsAutoResetEnabled;
                    numMaxOrders.Value = status.MaxOrderLimit > 0 ? status.MaxOrderLimit : 100;

                    if (status.TriggerInstalled)
                    {
                        lblScriptStatusBadge.Text = $"✅ تريجر الداتا بيز مفعل في SQL Server (حد التصفير: {status.MaxOrderLimit})";
                        lblScriptStatusBadge.ForeColor = accentGreen;
                    }
                    else
                    {
                        lblScriptStatusBadge.Text = "❌ التريجر غير مفعل في الداتا بيز (العداد يتزايد باستمرار دون تصفير دوري)";
                        lblScriptStatusBadge.ForeColor = textGray;
                    }

                    txtResultTabReset.AppendText($"[Status] الفرع [{branch.BranchName}] - تاريخ العمل: {status.PosDate} | العداد الحالي: {status.CurrentOrderNumber} | الاسكريبت: {(status.TriggerInstalled ? "مفعل" : "معطل")}\r\n");
                }
            }
            catch (Exception ex)
            {
                txtResultTabReset.AppendText($"⚠️ تعذر جلب تفاصيل العداد: {ex.Message}\r\n");
            }
        }

        private async void BtnApplyResetConfig_Click(object? sender, EventArgs e)
        {
            if (cmbBranchesReset.SelectedItem == null) return;
            var branch = (BranchItem)cmbBranchesReset.SelectedItem;

            btnApplyResetConfig.Enabled = false;
            txtResultTabReset.AppendText($"\r\n[Action] جاري حفظ الإعداد وتطبيق الاسكريبت في الداتا بيز...\r\n");

            try
            {
                using var client = new HttpClient();
                var req = new
                {
                    branchId = branch.Id,
                    enableAutoReset = chkEnableAutoReset.Checked,
                    maxOrderLimit = (int)numMaxOrders.Value,
                    resetCurrentNow = chkResetCurrentNow.Checked
                };

                var json = JsonSerializer.Serialize(req);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                string url = $"{txtApiUrlReset.Text.TrimEnd('/')}/api/License/ConfigureOrderReset";
                var res = await client.PostAsync(url, content);
                var body = await res.Content.ReadAsStringAsync();

                if (res.IsSuccessStatusCode)
                {
                    txtResultTabReset.AppendText($"✅ نجاح: {body}\r\n");
                    MessageBox.Show("تم تطبيق الإعداد وتفعيل الاسكريبت في قاعدة البيانات بنجاح!", "نجاح العملية", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    chkResetCurrentNow.Checked = false;
                    await RefreshOrderCounterStatusAsync();
                }
                else
                {
                    txtResultTabReset.AppendText($"❌ فشل ({res.StatusCode}): {body}\r\n");
                    MessageBox.Show($"فشل تطبيق الإعداد: {body}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                txtResultTabReset.AppendText($"❌ خطأ استثنائي: {ex.Message}\r\n");
            }
            finally
            {
                btnApplyResetConfig.Enabled = true;
            }
        }

        private async void BtnSetManualCounter_Click(object? sender, EventArgs e)
        {
            if (cmbBranchesReset.SelectedItem == null) return;
            var branch = (BranchItem)cmbBranchesReset.SelectedItem;

            btnSetManualCounter.Enabled = false;
            int targetValue = (int)numManualCounter.Value;
            txtResultTabReset.AppendText($"\r\n[Action] تعيين العداد يدوياً إلى {targetValue}...\r\n");

            try
            {
                using var client = new HttpClient();
                var req = new
                {
                    branchId = branch.Id,
                    enableAutoReset = chkEnableAutoReset.Checked,
                    maxOrderLimit = (int)numMaxOrders.Value,
                    resetCurrentNow = false,
                    setCurrentTo = targetValue
                };

                var json = JsonSerializer.Serialize(req);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                string url = $"{txtApiUrlReset.Text.TrimEnd('/')}/api/License/ConfigureOrderReset";
                var res = await client.PostAsync(url, content);
                var body = await res.Content.ReadAsStringAsync();

                if (res.IsSuccessStatusCode)
                {
                    txtResultTabReset.AppendText($"✅ تم تحديث العداد الحالي إلى {targetValue} بنجاح!\r\n");
                    MessageBox.Show($"تم تحديث العداد الحالي إلى {targetValue} بنجاح!", "تم التحديث", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await RefreshOrderCounterStatusAsync();
                }
                else
                {
                    txtResultTabReset.AppendText($"❌ فشل ({res.StatusCode}): {body}\r\n");
                }
            }
            catch (Exception ex)
            {
                txtResultTabReset.AppendText($"❌ خطأ: {ex.Message}\r\n");
            }
            finally
            {
                btnSetManualCounter.Enabled = true;
            }
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

            if (numFull.Value == 0 && numBackOnly.Value == 0 && numPOS.Value == 0 && numWarehouse.Value == 0)
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
                client.DefaultRequestHeaders.Add("X-License-Manager-Secret", "LM_SUPER_ADMIN_AUTHORIZATION_TOKEN_SECRET_2026");
                int totalSuccess = 0;

                // 1. Generate FULL (BackOffice + POS) - Type 3
                for (int i = 0; i < (int)numFull.Value; i++)
                {
                    txtResultTab1.AppendText($"[API] Issuing Full License ({i + 1}/{(int)numFull.Value})...");
                    string url = $"{txtApiUrl.Text.TrimEnd('/')}/api/License/GenerateKey?branchId={branch.Id}&licenseType=3&maxDevices=1&expiryMonths={(int)numMonths.Value}";
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode) 
                    { 
                        txtResultTab1.AppendText(" SUCCESS\r\n"); 
                        totalSuccess++; 
                    }
                    else
                    {
                        var err = await response.Content.ReadAsStringAsync();
                        txtResultTab1.AppendText($" FAILED ({(int)response.StatusCode}): {err}\r\n");
                    }
                }

                // 2. Generate BackOffice Only - Type 2
                for (int i = 0; i < (int)numBackOnly.Value; i++)
                {
                    txtResultTab1.AppendText($"[API] Issuing BackOffice-Only License ({i + 1}/{(int)numBackOnly.Value})...");
                    string url = $"{txtApiUrl.Text.TrimEnd('/')}/api/License/GenerateKey?branchId={branch.Id}&licenseType=2&maxDevices=1&expiryMonths={(int)numMonths.Value}";
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode) 
                    { 
                        txtResultTab1.AppendText(" SUCCESS\r\n"); 
                        totalSuccess++; 
                    }
                    else
                    {
                        var err = await response.Content.ReadAsStringAsync();
                        txtResultTab1.AppendText($" FAILED ({(int)response.StatusCode}): {err}\r\n");
                    }
                }

                // 3. Generate POS Only - Type 1
                for (int i = 0; i < (int)numPOS.Value; i++)
                {
                    txtResultTab1.AppendText($"[API] Issuing POS-Only License ({i + 1}/{(int)numPOS.Value})...");
                    string url = $"{txtApiUrl.Text.TrimEnd('/')}/api/License/GenerateKey?branchId={branch.Id}&licenseType=1&maxDevices=1&expiryMonths={(int)numMonths.Value}";
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode) 
                    { 
                        txtResultTab1.AppendText(" SUCCESS\r\n"); 
                        totalSuccess++; 
                    }
                    else
                    {
                        var err = await response.Content.ReadAsStringAsync();
                        txtResultTab1.AppendText($" FAILED ({(int)response.StatusCode}): {err}\r\n");
                    }
                }

                // 4. Generate Warehouse Only - Type 4
                for (int i = 0; i < (int)numWarehouse.Value; i++)
                {
                    txtResultTab1.AppendText($"[API] Issuing Warehouse-Only License ({i + 1}/{(int)numWarehouse.Value})...");
                    string url = $"{txtApiUrl.Text.TrimEnd('/')}/api/License/GenerateKey?branchId={branch.Id}&licenseType=4&maxDevices=1&expiryMonths={(int)numMonths.Value}";
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode) 
                    { 
                        txtResultTab1.AppendText(" SUCCESS\r\n"); 
                        totalSuccess++; 
                    }
                    else
                    {
                        var err = await response.Content.ReadAsStringAsync();
                        txtResultTab1.AppendText($" FAILED ({(int)response.StatusCode}): {err}\r\n");
                    }
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
            if (string.IsNullOrWhiteSpace(txtPlainDbStr.Text)) return;
            try
            {
                byte[] key = Encoding.UTF8.GetBytes(DbCryptKey);
                using var aes = Aes.Create();
                aes.Key = key;
                aes.GenerateIV();

                using var ms = new MemoryStream();
                ms.Write(aes.IV, 0, aes.IV.Length); // prepend IV

                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(txtPlainDbStr.Text.Trim());
                }

                txtEncDbStr.Text = Convert.ToBase64String(ms.ToArray());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Encryption failed: " + ex.Message);
            }
        }

        private void BtnHashAdminPass_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtPlainAdminPass.Text)) return;
            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(txtPlainAdminPass.Text.Trim()));
            var sb = new StringBuilder();
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            txtHashAdminPass.Text = sb.ToString();
        }
    }
}