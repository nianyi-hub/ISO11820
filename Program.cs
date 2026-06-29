using ISO11820System.Utilities;

namespace ISO11820System
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点
        /// </summary>
        [STAThread]
        static void Main()
        {
            Directory.CreateDirectory("Backup");

            // 初始化日志
            Logger.Initialize();

            // 初始化全局配置
            var appContext = AppGlobals.Instance;

            // WinForms应用程序配置
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ApplicationConfiguration.Initialize();

            // 显示登录窗体
            using var loginForm = new Forms.LoginForm();
            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                // 登录成功，显示主窗体
                Application.Run(new Forms.MainForm());
            }
        }
    }
}
