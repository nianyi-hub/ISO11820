using ISO11820System.Models;
using ISO11820System.Data;
using ISO11820System.Core;
using ISO11820System.Services;
using ISO11820System.Utilities;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using OxyPlot.WindowsForms;

namespace ISO11820System.Forms
{
    /// <summary>
    /// 主界面（含试验控制、记录查询、设备校准三个Tab）
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly TestMasterController _controller;
        private readonly DbHelper _dbHelper;
        private readonly ExportService _exportService;
        private readonly SensorSimulator _simulator;

        // 系统托盘
        private NotifyIcon notifyIcon;
        private ContextMenuStrip trayContextMenu;

        // ===== Tab 1: 试验控制 =====
        private Label lblTF1, lblTF2, lblTS, lblTC, lblTCal;
        private Label lblState, lblTime, lblProductId, lblDrift;
        private Panel statusLed;  // 状态指示灯
        private RichTextBox txtMessages;
        private Button btnNewTest, btnStartHeating, btnStopHeating;
        private Button btnStartRecording, btnStopRecording, btnSaveResult, btnSettings, btnDataMaintenance;
        private PlotView plotView;
        private PlotModel plotModel;
        private LineSeries seriesTF1, seriesTF2, seriesTS, seriesTC;

        // 升温速度调节
        private TrackBar trackHeatingRate;
        private Label lblHeatingRate;

        // ===== Tab 2: 记录查询 =====
        private DateTimePicker dtpFrom, dtpTo;
        private TextBox txtQueryProductId;
        private ComboBox cmbQueryOperator;
        private DataGridView dgvTests;
        private Button btnQuery, btnQueryDetail, btnQueryExport, btnQueryCompare;

        // ===== Tab 3: 设备校准 =====
        private Label lblCalTemp;
        private ListView lvCalibrations;
        private Button btnRecordCal, btnRefreshCal;

        public MainForm()
        {
            // 初始化数据库和服务
            _dbHelper = new DbHelper(AppGlobals.Instance.Config.SqlitePath);
            _exportService = new ExportService();

            // 初始化仿真引擎和控制器
            _simulator = new SensorSimulator
            {
                TargetTemp = AppGlobals.Instance.Config.TargetFurnaceTemp,
                HeatingRate = AppGlobals.Instance.Config.HeatingRatePerSecond,
                TempFluctuation = AppGlobals.Instance.Config.TempFluctuation,
                StableThreshold = AppGlobals.Instance.Config.StableThreshold
            };
            _simulator.Reset(AppGlobals.Instance.Config.InitialFurnaceTemp);

            var daqWorker = new DaqWorker(_simulator, AppGlobals.Instance.Config.EnableSimulation);
            _controller = new TestMasterController(_dbHelper, _simulator, daqWorker);
            _controller.DataBroadcast += OnDataBroadcast;

            InitializeComponent();

            // 启用键盘快捷键
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;

            // 初始化系统托盘
            InitializeTray();

            // 启动控制器
            _controller.Start();
        }

        private void InitializeComponent()
        {
            this.Text = $"ISO 11820 试验系统 - {AppGlobals.Instance.CurrentUser?.Username}";
            this.Size = new Size(1200, 850);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1000, 700);

            var tabControl = new TabControl
            {
                Location = new Point(10, 10),
                Size = new Size(1160, 780),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // ===== Tab 1: 试验控制 =====
            var tabTest = new TabPage("试验控制");
            BuildTestControlTab(tabTest);
            tabControl.TabPages.Add(tabTest);

            // ===== Tab 2: 记录查询 =====
            var tabQuery = new TabPage("记录查询");
            BuildRecordQueryTab(tabQuery);
            tabControl.TabPages.Add(tabQuery);

            // ===== Tab 3: 设备校准 =====
            var tabCal = new TabPage("设备校准");
            BuildCalibrationTab(tabCal);
            tabControl.TabPages.Add(tabCal);

            this.Controls.Add(tabControl);
        }

        #region Tab 1: 试验控制

        private void BuildTestControlTab(TabPage tab)
        {
            // 温度数值显示（左侧）
            var panelTemp = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(320, 210),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblTitle = new Label
            {
                Text = "实时温度",
                Location = new Point(10, 8),
                Size = new Size(300, 20),
                Font = new Font("微软雅黑", 10, FontStyle.Bold)
            };

            lblTF1 = CreateLedLabel("炉温1: -- °C", 10, 35, Color.Red);
            lblTF2 = CreateLedLabel("炉温2: -- °C", 10, 70, Color.Orange);
            lblTS = CreateLedLabel("表面温: -- °C", 10, 105, Color.Blue);
            lblTC = CreateLedLabel("中心温: -- °C", 10, 140, Color.Green);
            lblTCal = CreateLedLabel("校准温: -- °C", 10, 175, Color.Gray);

            panelTemp.Controls.AddRange(new Control[] { lblTitle, lblTF1, lblTF2, lblTS, lblTC, lblTCal });

            // 状态信息
            var panelState = new Panel
            {
                Location = new Point(10, 230),
                Size = new Size(320, 120),
                BorderStyle = BorderStyle.FixedSingle
            };

            lblState = new Label { Text = "状态: 空闲", Location = new Point(35, 8), Size = new Size(280, 22), Font = new Font("微软雅黑", 10, FontStyle.Bold) };

            // 状态指示灯（圆形色块）
            statusLed = new Panel
            {
                Location = new Point(12, 12),
                Size = new Size(16, 16),
                BackColor = Color.Gray
            };
            statusLed.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(statusLed.BackColor);
                e.Graphics.FillEllipse(brush, 1, 1, 14, 14);
            };

            lblTime = new Label { Text = "记录时间: 0 秒", Location = new Point(10, 35), Size = new Size(150, 20) };
            lblProductId = new Label { Text = "样品编号: 无", Location = new Point(10, 58), Size = new Size(300, 20) };
            lblDrift = new Label { Text = "温漂: 0.00 °C/10min", Location = new Point(10, 81), Size = new Size(200, 20) };

            panelState.Controls.AddRange(new Control[] { statusLed, lblState, lblTime, lblProductId, lblDrift });

            // OxyPlot温度曲线图
            plotModel = new PlotModel { Title = "温度曲线" };
            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "温度 (°C)",
                Minimum = 0,
                Maximum = 800,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            });
            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "时间 (秒)",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            });

            seriesTF1 = new LineSeries { Title = "炉温1", Color = OxyColor.FromRgb(255, 0, 0), StrokeThickness = 1.5 };
            seriesTF2 = new LineSeries { Title = "炉温2", Color = OxyColor.FromRgb(255, 165, 0), StrokeThickness = 1.5 };
            seriesTS = new LineSeries { Title = "表面温", Color = OxyColor.FromRgb(0, 0, 255), StrokeThickness = 1.5 };
            seriesTC = new LineSeries { Title = "中心温", Color = OxyColor.FromRgb(0, 180, 0), StrokeThickness = 1.5 };

            plotModel.Series.Add(seriesTF1);
            plotModel.Series.Add(seriesTF2);
            plotModel.Series.Add(seriesTS);
            plotModel.Series.Add(seriesTC);
            plotModel.IsLegendVisible = true;

            plotView = new PlotView
            {
                Location = new Point(340, 10),
                Size = new Size(800, 340),
                Model = plotModel,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            // 按钮区域
            var panelButtons = new Panel
            {
                Location = new Point(10, 360),
                Size = new Size(1130, 70),
                BorderStyle = BorderStyle.FixedSingle
            };

            btnNewTest = new Button { Text = "新建试验 (Ctrl+N)", Location = new Point(10, 18), Size = new Size(130, 35) };
            btnStartHeating = new Button { Text = "开始升温 (F5)", Location = new Point(148, 18), Size = new Size(130, 35), Enabled = false };
            btnStopHeating = new Button { Text = "停止升温 (F6)", Location = new Point(286, 18), Size = new Size(130, 35), Enabled = false };
            btnStartRecording = new Button { Text = "开始记录 (F7)", Location = new Point(424, 18), Size = new Size(130, 35), Enabled = false };
            btnStopRecording = new Button { Text = "停止记录 (F8)", Location = new Point(562, 18), Size = new Size(130, 35), Enabled = false };
            btnSaveResult = new Button { Text = "试验记录", Location = new Point(700, 18), Size = new Size(120, 35), Enabled = false };
            btnSettings = new Button { Text = "参数设置", Location = new Point(828, 18), Size = new Size(120, 35) };
            btnDataMaintenance = new Button { Text = "数据维护", Location = new Point(956, 18), Size = new Size(120, 35) };

            btnNewTest.Click += BtnNewTest_Click;
            btnStartHeating.Click += BtnStartHeating_Click;
            btnStopHeating.Click += BtnStopHeating_Click;
            btnStartRecording.Click += BtnStartRecording_Click;
            btnStopRecording.Click += BtnStopRecording_Click;
            btnSaveResult.Click += BtnSaveResult_Click;
            btnSettings.Click += BtnSettings_Click;
            btnDataMaintenance.Click += BtnDataMaintenance_Click;

            panelButtons.Controls.AddRange(new Control[] {
                btnNewTest, btnStartHeating, btnStopHeating, btnStartRecording,
                btnStopRecording, btnSaveResult, btnSettings, btnDataMaintenance
            });

            // 升温速度调节滑块
            lblHeatingRate = new Label
            {
                Text = $"升温速度: {AppGlobals.Instance.Config.HeatingRatePerSecond:F0} °C/秒",
                Location = new Point(10, 436),
                Size = new Size(200, 22),
                Font = new Font("微软雅黑", 9)
            };

            trackHeatingRate = new TrackBar
            {
                Location = new Point(10, 458),
                Size = new Size(400, 45),
                Minimum = 1,
                Maximum = 100,
                Value = (int)Math.Clamp(AppGlobals.Instance.Config.HeatingRatePerSecond, 1, 100),
                TickFrequency = 10,
                SmallChange = 1,
                LargeChange = 10
            };
            trackHeatingRate.Scroll += TrackHeatingRate_Scroll;

            // 系统消息区域
            var lblMessages = new Label { Text = "系统消息:", Location = new Point(10, 500), Size = new Size(150, 20) };
            txtMessages = new RichTextBox
            {
                Location = new Point(10, 520),
                Size = new Size(1130, 210),
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.White,
                Font = new Font("Consolas", 9),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            tab.Controls.AddRange(new Control[] {
                panelTemp, panelState, plotView, panelButtons,
                lblHeatingRate, trackHeatingRate, lblMessages, txtMessages
            });
        }

        private Label CreateLedLabel(string text, int x, int y, Color color)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(220, 28),
                Font = new Font("Consolas", 13, FontStyle.Bold),
                ForeColor = color,
                BackColor = Color.Black,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 0, 0)
            };
        }

        #endregion

        #region Tab 2: 记录查询

        private void BuildRecordQueryTab(TabPage tab)
        {
            // 查询条件区域
            var panelCriteria = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(1130, 55),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblFrom = new Label { Text = "开始日期:", Location = new Point(10, 18), Size = new Size(70, 22) };
            dtpFrom = new DateTimePicker
            {
                Location = new Point(80, 15),
                Size = new Size(130, 25),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now.AddMonths(-1)
            };

            var lblTo = new Label { Text = "结束日期:", Location = new Point(220, 18), Size = new Size(70, 22) };
            dtpTo = new DateTimePicker
            {
                Location = new Point(290, 15),
                Size = new Size(130, 25),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now
            };

            var lblPid = new Label { Text = "样品编号:", Location = new Point(435, 18), Size = new Size(70, 22) };
            txtQueryProductId = new TextBox { Location = new Point(505, 15), Size = new Size(100, 25) };

            var lblOp = new Label { Text = "操作员:", Location = new Point(620, 18), Size = new Size(60, 22) };
            cmbQueryOperator = new ComboBox { Location = new Point(680, 15), Size = new Size(110, 25), DropDownStyle = ComboBoxStyle.DropDownList };

            btnQuery = new Button { Text = "查询", Location = new Point(810, 13), Size = new Size(80, 30) };
            btnQuery.Click += BtnQuery_Click;

            btnQueryDetail = new Button { Text = "查看详情", Location = new Point(900, 13), Size = new Size(90, 30), Enabled = false };
            btnQueryDetail.Click += BtnQueryDetail_Click;

            btnQueryCompare = new Button { Text = "对比", Location = new Point(1000, 13), Size = new Size(60, 30), Enabled = false };
            btnQueryCompare.Click += BtnQueryCompare_Click;

            btnQueryExport = new Button { Text = "导出", Location = new Point(1065, 13), Size = new Size(65, 30), Enabled = false };
            btnQueryExport.Click += BtnQueryExport_Click;

            panelCriteria.Controls.AddRange(new Control[] {
                lblFrom, dtpFrom, lblTo, dtpTo,
                lblPid, txtQueryProductId, lblOp, cmbQueryOperator,
                btnQuery, btnQueryDetail, btnQueryCompare, btnQueryExport
            });

            // 查询结果表格
            dgvTests = new DataGridView
            {
                Location = new Point(10, 75),
                Size = new Size(1130, 640),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            dgvTests.SelectionChanged += (s, e) =>
            {
                btnQueryDetail.Enabled = dgvTests.SelectedRows.Count == 1;
                btnQueryCompare.Enabled = dgvTests.SelectedRows.Count == 2;
                btnQueryExport.Enabled = dgvTests.Rows.Count > 0;
            };
            dgvTests.CellDoubleClick += (s, e) => ShowTestDetail();

            tab.Controls.AddRange(new Control[] { panelCriteria, dgvTests });

            // 加载操作员列表
            LoadOperatorList();
        }

        private void LoadOperatorList()
        {
            try
            {
                var ops = _dbHelper.GetOperatorNames();
                cmbQueryOperator.Items.Clear();
                cmbQueryOperator.Items.Add("(全部)");
                foreach (var op in ops)
                    cmbQueryOperator.Items.Add(op);
                cmbQueryOperator.SelectedIndex = 0;
            }
            catch { }
        }

        #endregion

        #region Tab 3: 设备校准

        private void BuildCalibrationTab(TabPage tab)
        {
            // 左侧：当前校准温度显示
            var panelCalLeft = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(350, 200),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblCalTitle = new Label
            {
                Text = "校准温度通道（实时）",
                Location = new Point(10, 10),
                Size = new Size(330, 25),
                Font = new Font("微软雅黑", 11, FontStyle.Bold)
            };

            lblCalTemp = new Label
            {
                Text = "--- °C",
                Location = new Point(30, 60),
                Size = new Size(280, 50),
                Font = new Font("Consolas", 28, FontStyle.Bold),
                ForeColor = Color.Green,
                BackColor = Color.Black,
                TextAlign = ContentAlignment.MiddleCenter
            };

            btnRecordCal = new Button
            {
                Text = "记录校准数据",
                Location = new Point(80, 140),
                Size = new Size(180, 35)
            };
            btnRecordCal.Click += BtnRecordCal_Click;

            panelCalLeft.Controls.AddRange(new Control[] { lblCalTitle, lblCalTemp, btnRecordCal });

            // 右侧：历史校准记录
            var panelCalRight = new Panel
            {
                Location = new Point(375, 10),
                Size = new Size(765, 200),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblCalHistory = new Label
            {
                Text = "历史校准记录",
                Location = new Point(10, 8),
                Size = new Size(200, 20),
                Font = new Font("微软雅黑", 10, FontStyle.Bold)
            };

            lvCalibrations = new ListView
            {
                Location = new Point(10, 32),
                Size = new Size(745, 148),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            lvCalibrations.Columns.Add("日期", 150);
            lvCalibrations.Columns.Add("类型", 80);
            lvCalibrations.Columns.Add("操作员", 100);
            lvCalibrations.Columns.Add("平均温度", 80);
            lvCalibrations.Columns.Add("最大偏差", 80);
            lvCalibrations.Columns.Add("通过", 60);
            lvCalibrations.Columns.Add("备注", 180);

            btnRefreshCal = new Button
            {
                Text = "刷新",
                Location = new Point(700, 4),
                Size = new Size(55, 24),
                Font = new Font("微软雅黑", 8)
            };
            btnRefreshCal.Click += (s, e) => RefreshCalibrationList();

            panelCalRight.Controls.AddRange(new Control[] { lblCalHistory, lvCalibrations, btnRefreshCal });

            tab.Controls.AddRange(new Control[] { panelCalLeft, panelCalRight });

            // 初始加载
            RefreshCalibrationList();
        }

        #endregion

        #region 数据广播回调

        private void OnDataBroadcast(object? sender, DataBroadcastEventArgs e)
        {
            // 切换到UI线程
            this.Invoke(() =>
            {
                // 更新温度显示
                lblTF1.Text = $"炉温1: {e.TF1:F1} °C";
                lblTF2.Text = $"炉温2: {e.TF2:F1} °C";
                lblTS.Text = $"表面温: {e.TS:F1} °C";
                lblTC.Text = $"中心温: {e.TC:F1} °C";
                lblTCal.Text = $"校准温: {e.TCal:F1} °C";
                lblCalTemp.Text = $"{e.TCal:F1} °C";

                // 更新状态显示
                lblState.Text = $"状态: {GetStateText(e.State)}";
                statusLed.BackColor = GetStatusLedColor(e.State);
                statusLed.Invalidate();  // 触发重绘圆形
                lblTime.Text = $"记录时间: {e.RecordedSeconds} 秒";
                lblProductId.Text = $"样品编号: {e.CurrentProductId}";
                lblDrift.Text = $"温漂: {e.TempDrift:F2} °C/10min";

                // 更新按钮状态
                UpdateButtonStates(e.State, e.HasUnsavedCompleteTest);

                // 更新曲线图
                UpdateChart(e.ChartData, e.State);

                // 显示系统消息
                foreach (var msg in e.Messages)
                {
                    Color msgColor = msg.Type switch
                    {
                        MessageType.Warning => Color.Yellow,
                        MessageType.Success => Color.LimeGreen,
                        MessageType.Error => Color.Red,
                        _ => Color.White
                    };

                    txtMessages.SelectionStart = txtMessages.TextLength;
                    txtMessages.SelectionLength = 0;
                    txtMessages.SelectionColor = msgColor;
                    txtMessages.AppendText($"[{msg.Time}] {msg.Message}\n");
                    txtMessages.SelectionColor = txtMessages.ForeColor;

                    // 对于重要提示，弹窗提醒
                    if (msg.Type == MessageType.Warning && msg.Message.Contains("温度已稳定"))
                    {
                        // 仅就绪提示需要弹窗
                    }
                    if (msg.Message.Contains("温度已稳定，可以开始记录"))
                    {
                        txtMessages.SelectionStart = txtMessages.TextLength;
                        txtMessages.SelectionLength = 0;
                        txtMessages.SelectionColor = Color.LimeGreen;
                        txtMessages.AppendText($"[{msg.Time}] ⚠ 系统提示：温度已稳定，可以点击「开始记录」\n");
                        txtMessages.SelectionColor = txtMessages.ForeColor;
                    }
                }

                // 自动滚动到底部
                txtMessages.SelectionStart = txtMessages.TextLength;
                txtMessages.ScrollToCaret();
            });
        }

        private void UpdateChart(List<ChartDataPoint> data, TestState state)
        {
            if (data == null || data.Count == 0) return;

            seriesTF1.Points.Clear();
            seriesTF2.Points.Clear();
            seriesTS.Points.Clear();
            seriesTC.Points.Clear();

            // 只显示最近的750个数据点（750 × 0.8s = 600s = 10分钟）
            int startIdx = Math.Max(0, data.Count - 750);
            for (int i = startIdx; i < data.Count; i++)
            {
                var pt = data[i];
                seriesTF1.Points.Add(new DataPoint(pt.Time, pt.TF1));
                seriesTF2.Points.Add(new DataPoint(pt.Time, pt.TF2));
                seriesTS.Points.Add(new DataPoint(pt.Time, pt.TS));
                seriesTC.Points.Add(new DataPoint(pt.Time, pt.TC));
            }

            // 动态调整X轴范围（滚动显示）
            if (data.Count > 0)
            {
                var lastTime = data.Last().Time;
                var xAxis = plotModel.Axes[1] as LinearAxis;
                if (xAxis != null)
                {
                    // 固定窗口大小：显示最近100秒的数据窗口
                    // 数据不足100秒时，固定显示 0~100 秒范围，避免横坐标只显示几秒钟
                    const double windowSize = 100.0;
                    if (lastTime <= windowSize)
                    {
                        // 数据尚未填满一个窗口：固定显示 0 ~ windowSize
                        xAxis.Minimum = 0;
                        xAxis.Maximum = windowSize + 5;
                    }
                    else
                    {
                        // 数据已超过窗口大小：滚动显示最近 windowSize 秒
                        xAxis.Minimum = lastTime - windowSize;
                        xAxis.Maximum = lastTime + 5;
                    }
                }
            }

            plotModel.InvalidatePlot(true);
        }

        private string GetStateText(TestState state)
        {
            return state switch
            {
                TestState.Idle => "空闲",
                TestState.Preparing => _controller.CurrentTest != null ? "升温中" : "恒温待命",
                TestState.Ready => "就绪",
                TestState.Recording => "记录中",
                TestState.Complete => "完成",
                _ => "未知"
            };
        }

        private Color GetStatusLedColor(TestState state)
        {
            return state switch
            {
                TestState.Idle => Color.Gray,
                TestState.Preparing => Color.DarkOrange,
                TestState.Ready => Color.DodgerBlue,
                TestState.Recording => Color.LimeGreen,
                TestState.Complete => Color.DarkGreen,
                _ => Color.Gray
            };
        }

        private void UpdateButtonStates(TestState state, bool hasUnsaved)
        {
            bool hasActiveTest = _controller.CurrentTest != null;

            // 如果存在已完成但未保存的试验，禁止新建和开始记录
            if (hasUnsaved)
            {
                btnNewTest.Enabled = false;
                btnStartHeating.Enabled = false;
                btnStopHeating.Enabled = true;
                btnStartRecording.Enabled = false;
                btnStopRecording.Enabled = false;
                btnSaveResult.Enabled = true;
                btnSettings.Enabled = true;
                return;
            }

            switch (state)
            {
                case TestState.Idle:
                    btnNewTest.Enabled = true;
                    btnStartHeating.Enabled = hasActiveTest;
                    btnStopHeating.Enabled = false;
                    btnStartRecording.Enabled = false;
                    btnStopRecording.Enabled = false;
                    btnSaveResult.Enabled = false;
                    btnSettings.Enabled = true;
                    break;

                case TestState.Preparing:
                    btnNewTest.Enabled = !hasActiveTest;
                    btnStartHeating.Enabled = false;
                    btnStopHeating.Enabled = true;
                    btnStartRecording.Enabled = false;
                    btnStopRecording.Enabled = false;
                    btnSaveResult.Enabled = false;
                    btnSettings.Enabled = true;
                    break;

                case TestState.Ready:
                    btnNewTest.Enabled = false;
                    btnStartHeating.Enabled = false;
                    btnStopHeating.Enabled = true;
                    btnStartRecording.Enabled = true;
                    btnStopRecording.Enabled = false;
                    btnSaveResult.Enabled = false;
                    btnSettings.Enabled = true;
                    break;

                case TestState.Recording:
                    btnNewTest.Enabled = false;
                    btnStartHeating.Enabled = false;
                    btnStopHeating.Enabled = false;
                    btnStartRecording.Enabled = false;
                    btnStopRecording.Enabled = true;
                    btnSaveResult.Enabled = false;
                    btnSettings.Enabled = false;
                    break;

                case TestState.Complete:
                    btnNewTest.Enabled = false;
                    btnStartHeating.Enabled = false;
                    btnStopHeating.Enabled = true;
                    btnStartRecording.Enabled = false;
                    btnStopRecording.Enabled = false;
                    btnSaveResult.Enabled = true;
                    btnSettings.Enabled = true;
                    break;
            }
        }

        #endregion

        #region 按钮事件

        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            // Ctrl+N: 新建试验
            if (e.Control && e.KeyCode == Keys.N && btnNewTest.Enabled)
            {
                e.Handled = true;
                BtnNewTest_Click(sender, e);
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.F5:
                    if (btnStartHeating.Enabled)
                    {
                        e.Handled = true;
                        BtnStartHeating_Click(sender, e);
                    }
                    break;
                case Keys.F6:
                    if (btnStopHeating.Enabled)
                    {
                        e.Handled = true;
                        BtnStopHeating_Click(sender, e);
                    }
                    break;
                case Keys.F7:
                    if (btnStartRecording.Enabled)
                    {
                        e.Handled = true;
                        BtnStartRecording_Click(sender, e);
                    }
                    break;
                case Keys.F8:
                    if (btnStopRecording.Enabled)
                    {
                        e.Handled = true;
                        BtnStopRecording_Click(sender, e);
                    }
                    break;
            }
        }

        private void BtnNewTest_Click(object? sender, EventArgs e)
        {
            using var form = new NewTestForm(_dbHelper);
            if (form.ShowDialog() == DialogResult.OK && form.CreatedTest != null)
            {
                _controller.CreateNewTest(form.CreatedTest, form.IsStandardDuration, form.CustomDurationSeconds);
            }
        }

        private void BtnStartHeating_Click(object? sender, EventArgs e)
        {
            _controller.StartHeating();
        }

        private void BtnStopHeating_Click(object? sender, EventArgs e)
        {
            _controller.StopHeating();
        }

        private void BtnStartRecording_Click(object? sender, EventArgs e)
        {
            _controller.StartRecording();
        }

        private void BtnStopRecording_Click(object? sender, EventArgs e)
        {
            _controller.StopRecording();
        }

        private void BtnSaveResult_Click(object? sender, EventArgs e)
        {
            using var form = new TestRecordForm();
            if (form.ShowDialog() == DialogResult.OK)
            {
                // ⚠️ 必须在 SaveTestResult 之前捕获数据，因为保存后会清空
                var tempData = _controller.GetTempDataCache();
                var productId = _controller.CurrentTest?.ProductId ?? "";
                var testId = _controller.CurrentTest?.TestId ?? "";

                _controller.SaveTestResult(form.PostWeight, form.HasFlame, form.FlameTime, form.FlameDuration, form.Memo);

                // 导出Excel和PDF
                if (tempData.Count > 0)
                {
                    // 从数据库重新加载刚保存的试验（含完整计算结果）
                    var savedTest = _dbHelper.GetTest(productId, testId);
                    if (savedTest != null)
                    {
                        var reportDir = AppGlobals.Instance.Config.ReportOutputDirectory;
                        if (string.IsNullOrEmpty(reportDir))
                            reportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");

                        var excelPath = Path.Combine(reportDir, $"{savedTest.TestId}_报告.xlsx");
                        var pdfPath = Path.Combine(reportDir, $"{savedTest.TestId}_报告.pdf");

                        _exportService.ExportExcel(savedTest, tempData, excelPath);
                        _exportService.ExportPdf(savedTest, tempData, pdfPath);

                        MessageBox.Show($"试验完成！\nExcel: {excelPath}\nPDF: {pdfPath}", "完成",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void BtnSettings_Click(object? sender, EventArgs e)
        {
            // 参数设置：打开appsettings.json或显示配置对话框
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (File.Exists(configPath))
            {
                try
                {
                    System.Diagnostics.Process.Start("notepad.exe", configPath);
                }
                catch
                {
                    MessageBox.Show($"配置文件路径: {configPath}", "参数设置",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("未找到配置文件 appsettings.json", "参数设置",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnDataMaintenance_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "将先备份数据库，再清理30天前的旧试验记录。\n\n是否继续？",
                "数据维护", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);

            if (result != DialogResult.OK) return;

            try
            {
                var dbPath = AppGlobals.Instance.Config.SqlitePath;

                // 1. 备份数据库
                var backupPath = dbPath.Replace(".db", $"_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                File.Copy(dbPath, backupPath);

                // 2. 清理旧数据
                int deletedCount = 0;
                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
                {
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "DELETE FROM testmaster WHERE testdate < date('now', '-30 days')";
                    deletedCount = cmd.ExecuteNonQuery();
                }

                // 3. 报告结果
                MessageBox.Show(
                    $"备份已保存至:\n{backupPath}\n\n已清理 {deletedCount} 条旧记录",
                    "数据维护完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TrackHeatingRate_Scroll(object? sender, EventArgs e)
        {
            var rate = trackHeatingRate.Value;
            lblHeatingRate.Text = $"升温速度: {rate} °C/秒";
            _simulator.HeatingRate = rate;

            // 写入 appsettings.json
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var updated = System.Text.RegularExpressions.Regex.Replace(
                        json,
                        @"""HeatingRatePerSecond"":\s*\d+(\.\d+)?",
                        $"\"HeatingRatePerSecond\": {rate}");
                    File.WriteAllText(configPath, updated);
                }
            }
            catch { /* 静默失败，不影响主流程 */ }
        }

        #endregion

        #region Tab 2: 记录查询事件

        private void BtnQuery_Click(object? sender, EventArgs e)
        {
            try
            {
                string? opFilter = null;
                if (cmbQueryOperator.SelectedIndex > 0) // 跳过"(全部)"
                    opFilter = cmbQueryOperator.SelectedItem?.ToString();

                var tests = _dbHelper.QueryTests(
                    fromDate: dtpFrom.Value.Date,
                    toDate: dtpTo.Value.Date.AddDays(1).AddSeconds(-1),
                    productId: string.IsNullOrWhiteSpace(txtQueryProductId.Text) ? null : txtQueryProductId.Text.Trim(),
                    operatorName: opFilter
                );

                dgvTests.DataSource = null;
                dgvTests.Columns.Clear();

                dgvTests.DataSource = tests.Select(t => new
                {
                    样品编号 = t.ProductId,
                    试验ID = t.TestId,
                    试验日期 = t.TestDate.ToString("yyyy-MM-dd"),
                    操作员 = t.Operator,
                    试验前质量_g = t.PreWeight,
                    试验后质量_g = t.PostWeight,
                    失重率 = $"{t.LostWeightPercent:F2}%",
                    样品温升 = $"{t.DeltaTF:F1}°C",
                    时长_秒 = t.TotalTestTime,
                    判定 = (t.DeltaTF <= 50 && t.LostWeightPercent <= 50 && t.FlameDuration < 5) ? "合格" : "不合格"
                }).ToList();

                dgvTests.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

                // 行着色：合格绿色背景，不合格红色背景
                foreach (DataGridViewRow row in dgvTests.Rows)
                {
                    if (row.Cells["判定"].Value?.ToString() == "合格")
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(230, 255, 230);
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(0, 100, 0);
                    }
                    else
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 230, 230);
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(180, 0, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"查询失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnQueryDetail_Click(object? sender, EventArgs e)
        {
            ShowTestDetail();
        }

        private void ShowTestDetail()
        {
            if (dgvTests.SelectedRows.Count == 0) return;

            var row = dgvTests.SelectedRows[0];
            var testId = row.Cells["试验ID"].Value?.ToString() ?? "";
            var productId = row.Cells["样品编号"].Value?.ToString() ?? "";

            var test = _dbHelper.GetTest(productId, testId);
            if (test == null)
            {
                MessageBox.Show("未找到试验记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"样品编号: {test.ProductId}");
            sb.AppendLine($"试验ID: {test.TestId}");
            sb.AppendLine($"试验日期: {test.TestDate:yyyy-MM-dd}");
            sb.AppendLine($"操作员: {test.Operator}");
            sb.AppendLine($"设备: {test.ApparatusName}");
            sb.AppendLine($"环境: {test.AmbientTemp:F1}°C / {test.AmbientHumidity:F1}%");
            sb.AppendLine("---");
            sb.AppendLine($"试验前质量: {test.PreWeight:F2} g");
            sb.AppendLine($"试验后质量: {test.PostWeight:F2} g");
            sb.AppendLine($"失重量: {test.LostWeight:F2} g");
            sb.AppendLine($"失重率: {test.LostWeightPercent:F2}%");
            sb.AppendLine("---");
            sb.AppendLine($"炉温1温升: {test.DeltaTF1:F1} °C");
            sb.AppendLine($"炉温2温升: {test.DeltaTF2:F1} °C");
            sb.AppendLine($"表面温升: {test.DeltaTS:F1} °C");
            sb.AppendLine($"中心温升: {test.DeltaTC:F1} °C");
            sb.AppendLine($"样品温升(判定项): {test.DeltaTF:F1} °C");
            sb.AppendLine($"试验时长: {test.TotalTestTime} 秒");
            sb.AppendLine("---");
            sb.AppendLine($"火焰发生时刻: {test.FlameTime} 秒");
            sb.AppendLine($"火焰持续时间: {test.FlameDuration} 秒");
            sb.AppendLine($"恒功率值: {test.ConstPower}");
            sb.AppendLine($"备注: {test.Memo ?? "无"}");
            sb.AppendLine("---");
            bool passed = test.DeltaTF <= 50 && test.LostWeightPercent <= 50 && test.FlameDuration < 5;
            sb.AppendLine($"判定结论: {(passed ? "合格" : "不合格")}");

            MessageBox.Show(sb.ToString(), "试验详情", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnQueryExport_Click(object? sender, EventArgs e)
        {
            try
            {
                string? opFilter = null;
                if (cmbQueryOperator.SelectedIndex > 0)
                    opFilter = cmbQueryOperator.SelectedItem?.ToString();

                var tests = _dbHelper.QueryTests(
                    fromDate: dtpFrom.Value.Date,
                    toDate: dtpTo.Value.Date.AddDays(1).AddSeconds(-1),
                    productId: string.IsNullOrWhiteSpace(txtQueryProductId.Text) ? null : txtQueryProductId.Text.Trim(),
                    operatorName: opFilter
                );

                if (tests.Count == 0)
                {
                    MessageBox.Show("没有可导出的数据", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var reportDir = AppGlobals.Instance.Config.ReportOutputDirectory;
                if (string.IsNullOrEmpty(reportDir))
                    reportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");

                var filePath = Path.Combine(reportDir, $"查询结果_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                _exportService.ExportQueryResults(tests, filePath);

                MessageBox.Show($"查询结果已导出到:\n{filePath}", "导出完成",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnQueryCompare_Click(object? sender, EventArgs e)
        {
            if (dgvTests.SelectedRows.Count != 2) return;

            try
            {
                var row1 = dgvTests.SelectedRows[0];
                var row2 = dgvTests.SelectedRows[1];
                var pid1 = row1.Cells["样品编号"].Value?.ToString() ?? "";
                var tid1 = row1.Cells["试验ID"].Value?.ToString() ?? "";
                var pid2 = row2.Cells["样品编号"].Value?.ToString() ?? "";
                var tid2 = row2.Cells["试验ID"].Value?.ToString() ?? "";

                var test1 = _dbHelper.GetTest(pid1, tid1);
                var test2 = _dbHelper.GetTest(pid2, tid2);

                if (test1 == null || test2 == null)
                {
                    MessageBox.Show("未找到试验记录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                bool passed1 = test1.DeltaTF <= 50 && test1.LostWeightPercent <= 50 && test1.FlameDuration < 5;
                bool passed2 = test2.DeltaTF <= 50 && test2.LostWeightPercent <= 50 && test2.FlameDuration < 5;

                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"{"项目",-16} {"试验A",-24} {"试验B",-24}");
                sb.AppendLine(new string('-', 64));
                sb.AppendLine($"{"样品编号",-16} {test1.ProductId,-24} {test2.ProductId,-24}");
                sb.AppendLine($"{"试验日期",-16} {test1.TestDate,-24:yyyy-MM-dd} {test2.TestDate,-24:yyyy-MM-dd}");
                sb.AppendLine($"{"失重率(%)",-16} {test1.LostWeightPercent,-24:F2} {test2.LostWeightPercent,-24:F2}");
                sb.AppendLine($"{"样品温升(°C)",-16} {test1.DeltaTF,-24:F1} {test2.DeltaTF,-24:F1}");
                sb.AppendLine($"{"火焰时长(秒)",-16} {test1.FlameDuration,-24} {test2.FlameDuration,-24}");
                sb.AppendLine($"{"判定结论",-16} {(passed1 ? "合格" : "不合格"),-24} {(passed2 ? "合格" : "不合格"),-24}");

                MessageBox.Show(sb.ToString(), "试验记录对比", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"对比失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Tab 3: 设备校准事件

        private void BtnRecordCal_Click(object? sender, EventArgs e)
        {
            try
            {
                var (TF1, TF2, TS, TC, TCal) = _controller.GetCurrentTemperatures();

                // 简化版：记录当前校准温度
                var record = new CalibrationRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    CalibrationDate = DateTime.Now,
                    CalibrationType = "Surface",
                    ApparatusId = AppGlobals.Instance.CurrentApparatus?.ApparatusId ?? 0,
                    Operator = AppGlobals.Instance.CurrentUser?.Username ?? "unknown",
                    TemperatureData = $"{{\"TCal\": {TCal:F1}}}",
                    AverageTemperature = TCal,
                    MaxDeviation = Math.Abs(TCal - 750),
                    UniformityResult = 0,
                    PassedCriteria = (TCal >= 745 && TCal <= 755) ? 1 : 0,
                    Remarks = "",
                    CreatedAt = DateTime.Now
                };

                _dbHelper.SaveCalibrationRecord(record);
                RefreshCalibrationList();

                MessageBox.Show($"校准数据已记录！\n温度: {TCal:F1} °C\n通过: {(record.PassedCriteria == 1 ? "是" : "否")}",
                    "校准记录", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"记录失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshCalibrationList()
        {
            try
            {
                lvCalibrations.Items.Clear();
                var records = _dbHelper.QueryCalibrations();
                foreach (var r in records)
                {
                    var item = new ListViewItem(r.CalibrationDate.ToString("yyyy-MM-dd HH:mm"));
                    item.SubItems.Add(r.CalibrationType);
                    item.SubItems.Add(r.Operator);
                    item.SubItems.Add(r.AverageTemperature?.ToString("F1") ?? "-");
                    item.SubItems.Add(r.MaxDeviation?.ToString("F2") ?? "-");
                    item.SubItems.Add(r.PassedCriteria == 1 ? "是" : "否");
                    item.SubItems.Add(r.Remarks);
                    lvCalibrations.Items.Add(item);
                }
            }
            catch { }
        }

        #endregion

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 如果试验进行中，弹确认框
            if (_controller.CurrentTest != null && _controller.CurrentState != TestState.Idle)
            {
                var result = MessageBox.Show("试验正在进行中，确定退出吗？", "确认退出",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            _controller.Stop();
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            base.OnFormClosing(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                this.ShowInTaskbar = false;
            }
        }

        private void InitializeTray()
        {
            trayContextMenu = new ContextMenuStrip();

            var menuShow = new ToolStripMenuItem("显示主窗口");
            menuShow.Click += (s, e) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.ShowInTaskbar = true;
                this.Activate();
            };

            var menuExit = new ToolStripMenuItem("退出程序");
            menuExit.Click += (s, e) => this.Close();

            trayContextMenu.Items.Add(menuShow);
            trayContextMenu.Items.Add(new ToolStripSeparator());
            trayContextMenu.Items.Add(menuExit);

            notifyIcon = new NotifyIcon
            {
                Text = "ISO 11820 试验系统",
                Icon = this.Icon ?? SystemIcons.Application,
                ContextMenuStrip = trayContextMenu,
                Visible = true
            };

            notifyIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.ShowInTaskbar = true;
                this.Activate();
            };
        }
    }
}
