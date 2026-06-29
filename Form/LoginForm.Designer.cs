using ISO11820System.Data;
using ISO11820System.Models;
using ISO11820System.Utilities;

namespace ISO11820System.Forms
{
    /// <summary>
    /// 登录窗体
    /// </summary>
    public partial class LoginForm : Form
    {
        private RadioButton rbAdmin;
        private RadioButton rbExperimenter;
        private TextBox txtPassword;
        private Button btnLogin;
        private Label lblTitle;
        private Label lblPassword;

        public LoginForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "ISO 11820 系统登录";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // 标题
            lblTitle = new Label
            {
                Text = "ISO 11820 试验系统",
                Location = new Point(100, 30),
                Size = new Size(200, 30),
                Font = new Font("微软雅黑", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // 角色选择
            var lblRole = new Label
            {
                Text = "选择角色:",
                Location = new Point(50, 80),
                Size = new Size(80, 25)
            };

            rbAdmin = new RadioButton
            {
                Text = "管理员",
                Location = new Point(150, 80),
                Size = new Size(80, 25),
                Checked = true
            };

            rbExperimenter = new RadioButton
            {
                Text = "试验员",
                Location = new Point(240, 80),
                Size = new Size(80, 25)
            };

            // 密码输入
            lblPassword = new Label
            {
                Text = "密码:",
                Location = new Point(50, 120),
                Size = new Size(80, 25)
            };

            txtPassword = new TextBox
            {
                Location = new Point(150, 120),
                Size = new Size(170, 25),
                PasswordChar = '*'
            };

            // 登录按钮
            btnLogin = new Button
            {
                Text = "登录",
                Location = new Point(150, 170),
                Size = new Size(100, 35)
            };
            btnLogin.Click += BtnLogin_Click;

            // 添加控件
            this.Controls.Add(lblTitle);
            this.Controls.Add(lblRole);
            this.Controls.Add(rbAdmin);
            this.Controls.Add(rbExperimenter);
            this.Controls.Add(lblPassword);
            this.Controls.Add(txtPassword);
            this.Controls.Add(btnLogin);

            // 回车键登录
            this.AcceptButton = btnLogin;
        }

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            var username = rbAdmin.Checked ? "admin" : "experimenter";
            var password = txtPassword.Text;

            var dbPath = AppGlobals.Instance.Config.SqlitePath;
            var dbHelper = new DbHelper(dbPath);

            if (dbHelper.Login(username, password, out var user) && user != null)
            {
                AppGlobals.Instance.CurrentUser = user;
                AppGlobals.Instance.CurrentApparatus = dbHelper.GetApparatus(0);

                Logger.Info($"用户登录成功: {username}");
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("密码错误，请重新输入", "登录失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Clear();
                txtPassword.Focus();
            }
        }
    }
}
