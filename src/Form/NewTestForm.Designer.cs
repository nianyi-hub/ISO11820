using ISO11820System.Models;
using ISO11820System.Data;
using ISO11820System.Utilities;

namespace ISO11820System.Forms
{
    /// <summary>
    /// 新建试验窗体
    /// </summary>
    public partial class NewTestForm : Form
    {
        private readonly DbHelper _dbHelper;
        public TestMaster? CreatedTest { get; private set; }
        public bool IsStandardDuration { get; private set; } = true;
        public int CustomDurationSeconds { get; private set; } = 3600;

        private TextBox txtProductId, txtTestId, txtProductName, txtSpecific, txtDiameter, txtHeight;
        private TextBox txtAmbTemp, txtAmbHumi, txtPreWeight;
        private RadioButton rbStandard, rbCustom;
        private TextBox txtCustomDuration;
        private Button btnSave, btnCancel;

        public NewTestForm(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "新建试验";
            this.Size = new Size(520, 580);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            int y = 15;

            // 样品信息
            AddField("样品编号:", ref txtProductId, ref y);
            AddField("试验标识:", ref txtTestId, ref y);
            AddField("样品名称:", ref txtProductName, ref y);
            AddField("规格型号:", ref txtSpecific, ref y);
            AddField("直径(mm):", ref txtDiameter, ref y);
            AddField("高度(mm):", ref txtHeight, ref y);

            y += 5;

            // 环境信息
            AddField("环境温度(°C):", ref txtAmbTemp, ref y);
            AddField("环境湿度(%):", ref txtAmbHumi, ref y);

            // 质量信息
            AddField("试验前质量(g):", ref txtPreWeight, ref y);

            y += 10;

            // 试验时长模式
            var lblMode = new Label { Text = "试验时长模式:", Location = new Point(30, y), Size = new Size(120, 25) };
            this.Controls.Add(lblMode);

            rbStandard = new RadioButton
            {
                Text = "标准60分钟",
                Location = new Point(160, y),
                Size = new Size(120, 25),
                Checked = true
            };
            rbStandard.CheckedChanged += (s, e) =>
            {
                if (rbStandard.Checked)
                {
                    txtCustomDuration.Enabled = false;
                    IsStandardDuration = true;
                }
            };

            rbCustom = new RadioButton
            {
                Text = "自定义时长",
                Location = new Point(290, y),
                Size = new Size(100, 25)
            };
            rbCustom.CheckedChanged += (s, e) =>
            {
                if (rbCustom.Checked)
                {
                    txtCustomDuration.Enabled = true;
                    IsStandardDuration = false;
                }
            };

            this.Controls.AddRange(new Control[] { rbStandard, rbCustom });
            y += 30;

            // 自定义时长输入
            var lblCustom = new Label { Text = "自定义时长(秒):", Location = new Point(30, y), Size = new Size(120, 25) };
            txtCustomDuration = new TextBox { Location = new Point(160, y), Size = new Size(120, 25), Text = "3600", Enabled = false };
            this.Controls.AddRange(new Control[] { lblCustom, txtCustomDuration });
            y += 40;

            // 设备信息（只读显示）
            var apparatus = AppGlobals.Instance.CurrentApparatus;
            var lblDevice = new Label
            {
                Text = $"设备: {apparatus?.ApparatusName ?? "一号试验炉"} ({apparatus?.InnerNumber ?? "FURNACE-01"})",
                Location = new Point(30, y),
                Size = new Size(440, 25),
                ForeColor = Color.Gray
            };
            this.Controls.Add(lblDevice);
            y += 25;

            var lblOp = new Label
            {
                Text = $"操作员: {AppGlobals.Instance.CurrentUser?.Username ?? "unknown"}",
                Location = new Point(30, y),
                Size = new Size(440, 25),
                ForeColor = Color.Gray
            };
            this.Controls.Add(lblOp);
            y += 35;

            // 按钮
            btnSave = new Button { Text = "创建试验", Location = new Point(150, y), Size = new Size(110, 35) };
            btnCancel = new Button { Text = "取消", Location = new Point(280, y), Size = new Size(110, 35) };

            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            this.Controls.AddRange(new Control[] { btnSave, btnCancel });

            // 设置默认值
            txtAmbTemp.Text = "25";
            txtAmbHumi.Text = "50";
            txtTestId.Text = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        }

        private void AddField(string label, ref TextBox textBox, ref int y)
        {
            var lbl = new Label { Text = label, Location = new Point(30, y), Size = new Size(120, 25) };
            textBox = new TextBox { Location = new Point(160, y), Size = new Size(310, 25) };
            this.Controls.AddRange(new Control[] { lbl, textBox });
            y += 35;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            // 验证输入
            if (string.IsNullOrWhiteSpace(txtProductId.Text))
            {
                MessageBox.Show("请输入样品编号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 自定义时长验证
                if (rbCustom.Checked)
                {
                    if (!int.TryParse(txtCustomDuration.Text, out int customDur) || customDur <= 0)
                    {
                        MessageBox.Show("请输入有效的自定义时长（正整数秒）", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    CustomDurationSeconds = customDur;
                }

                // 创建样品信息
                var product = new ProductMaster
                {
                    ProductId = txtProductId.Text.Trim(),
                    ProductName = txtProductName.Text.Trim(),
                    Specific = txtSpecific.Text.Trim(),
                    Diameter = double.TryParse(txtDiameter.Text, out double dia) ? dia : 0,
                    Height = double.TryParse(txtHeight.Text, out double h) ? h : 0
                };
                _dbHelper.SaveProduct(product);

                // 试验ID（用户可自定义或使用默认值）
                string testId = string.IsNullOrWhiteSpace(txtTestId.Text)
                    ? DateTime.Now.ToString("yyyyMMdd-HHmmss")
                    : txtTestId.Text.Trim();

                // 创建试验记录
                var apparatus = AppGlobals.Instance.CurrentApparatus;
                CreatedTest = new TestMaster
                {
                    ProductId = product.ProductId,
                    TestId = testId,
                    TestDate = DateTime.Now,
                    AmbientTemp = double.TryParse(txtAmbTemp.Text, out double ambT) ? ambT : 25,
                    AmbientHumidity = double.TryParse(txtAmbHumi.Text, out double ambH) ? ambH : 50,
                    PreWeight = double.TryParse(txtPreWeight.Text, out double preW) ? preW : 0,
                    Operator = AppGlobals.Instance.CurrentUser?.Username ?? "unknown",
                    ApparatusId = apparatus?.InnerNumber ?? "FURNACE-01",
                    ApparatusName = apparatus?.ApparatusName ?? "一号试验炉",
                    ApparatusCheckDate = apparatus?.CheckDateFrom ?? DateTime.Now,
                    ReportNo = product.ProductId,
                    According = "ISO 11820:2022"
                };

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"输入有误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
