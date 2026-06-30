using ISO11820System.Models;

namespace ISO11820System.Services
{
    /// <summary>
    /// 数据广播事件参数
    /// </summary>
    public class DataBroadcastEventArgs : EventArgs
    {
        /// <summary>炉温1（°C）</summary>
        public double TF1 { get; set; }

        /// <summary>炉温2（°C）</summary>
        public double TF2 { get; set; }

        /// <summary>表面温度（°C）</summary>
        public double TS { get; set; }

        /// <summary>中心温度（°C）</summary>
        public double TC { get; set; }

        /// <summary>校准温度（°C）</summary>
        public double TCal { get; set; }

        /// <summary>当前试验状态</summary>
        public TestState State { get; set; }

        /// <summary>已记录秒数</summary>
        public int RecordedSeconds { get; set; }

        /// <summary>温度漂移（°C/10min）</summary>
        public double TempDrift { get; set; }

        /// <summary>系统消息列表</summary>
        public List<MasterMessage> Messages { get; set; } = new();

        /// <summary>当前样品编号</summary>
        public string CurrentProductId { get; set; } = string.Empty;

        /// <summary>是否有未保存的已完成试验</summary>
        public bool HasUnsavedCompleteTest { get; set; }

        /// <summary>图表数据点（用于OxyPlot实时曲线）</summary>
        public List<ChartDataPoint> ChartData { get; set; } = new();
    }

    /// <summary>
    /// 系统消息模型
    /// </summary>
    public class MasterMessage
    {
        /// <summary>消息时间（HH:mm:ss）</summary>
        public string Time { get; set; } = string.Empty;

        /// <summary>消息内容</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>消息类型（用于颜色区分）</summary>
        public MessageType Type { get; set; } = MessageType.Info;
    }

    /// <summary>
    /// 消息类型枚举
    /// </summary>
    public enum MessageType
    {
        /// <summary>普通信息（白色）</summary>
        Info,
        /// <summary>提示信息（黄色）</summary>
        Warning,
        /// <summary>成功信息（绿色）</summary>
        Success,
        /// <summary>错误信息（红色）</summary>
        Error
    }

    /// <summary>
    /// 图表数据点
    /// </summary>
    public class ChartDataPoint
    {
        /// <summary>时间（秒）</summary>
        public double Time { get; set; }

        /// <summary>炉温1</summary>
        public double TF1 { get; set; }

        /// <summary>炉温2</summary>
        public double TF2 { get; set; }

        /// <summary>表面温</summary>
        public double TS { get; set; }

        /// <summary>中心温</summary>
        public double TC { get; set; }
    }
}
