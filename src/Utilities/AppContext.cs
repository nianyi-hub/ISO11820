using ISO11820System.Models;

namespace ISO11820System.Utilities
{
    /// <summary>
    /// 全局应用上下文（单例模式）
    /// 持有配置、当前登录用户等全局信息
    /// </summary>
    public class AppGlobals
    {
        private static AppGlobals? _instance;
        private static readonly object _lock = new object();

        public ConfigManager Config { get; private set; }
        public Operator? CurrentUser { get; set; }
        public Apparatus? CurrentApparatus { get; set; }

        private AppGlobals()
        {
            Config = new ConfigManager();
        }

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static AppGlobals Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new AppGlobals();
                        }
                    }
                }
                return _instance;
            }
        }
    }
}
