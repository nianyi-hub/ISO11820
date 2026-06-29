namespace ISO11820System.Forms
{
    /// <summary>
    /// 试验记录窗体（保存试验后质量和火焰信息）
    /// </summary>
    public partial class TestRecordForm : Form
    {
        public double PostWeight { get; private set; }
        public bool HasFlame { get; private set; }
        public int FlameTime { get; private set; }
        public int FlameDuration { get; private set; }
        public string Memo { get; private set; } = "";

        private TextBox txtPostWeight, txtFlameTime, txtFlameDuration, txtMemo;
        private CheckBox chkHasFlame;
        private Button btnSave, btnCancel;

        public TestRecordForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "保存试验记录";
            this.Size = new Size(450, 380);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            int y = 25;

            // 试验后质量
            var lblPostWeight = new Label { Text = "试验后质量(g):", Location = new Point(30, y), Size = new Size(120, 25) };
            txtPostWeight = new TextBox { Location = new Point(160, y), Size = new Size(200, 25) };
            y += 40;

            // 火焰信息
            chkHasFlame = new CheckBox { Text = "出现持续火焰", Location = new Point(30, y), Size = new Size(150, 25) };
            chkHasFlame.CheckedChanged += (s, e) =>
            {
                txtFlameTime.Enabled = chkHasFlame.Checked;
                txtFlameDuration.Enabled = chkHasFlame.Checked;
            };
            y += 35;

            var lblFlameTime = new Label { Text = "火焰发生时刻(秒):", Location = new Point(30, y), Size = new Size(120, 25) };
            txtFlameTime = new TextBox { Location = new Point(160, y), Size = new Size(200, 25), Enabled = false };
            y += 35;

            var lblFlameDuration = new Label { Text = "火焰持续时间(秒):", Location = new Point(30, y), Size = new Size(120, 25) };
            txtFlameDuration = new TextBox { Location = new Point(160, y), Size = new Size(200, 25), Enabled = false };
            y += 40;

            // 备注
            var lblMemo = new Label { Text = "备注:", Location = new Point(30, y), Size = new Size(120, 25) };
            txtMemo = new TextBox
            {
                Location = new Point(160, y),
                Size = new Size(200, 60),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            y += 70;

            // 按钮
            btnSave = new Button { Text = "保存", Location = new Point(120, y), Size = new Size(100, 35) };
            btnCancel = new Button { Text = "取消", Location = new Point(240, y), Size = new Size(100, 35) };

            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            this.Controls.AddRange(new Control[] {
                lblPostWeight, txtPostWeight,
                chkHasFlame,
                lblFlameTime, txtFlameTime,
                lblFlameDuration, txtFlameDuration,
                lblMemo, txtMemo,
                btnSave, btnCancel
            });
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                PostWeight = double.Parse(txtPostWeight.Text);
                HasFlame = chkHasFlame.Checked;

                if (HasFlame)
                {
                    FlameTime = int.TryParse(txtFlameTime.Text, out int ft) ? ft : 0;
                    FlameDuration = int.TryParse(txtFlameDuration.Text, out int fd) ? fd : 0;
                }
                else
                {
                    FlameTime = 0;
                    FlameDuration = 0;
                }

                Memo = txtMemo.Text.Trim();

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch
            {
                MessageBox.Show("请输入有效的数值", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
