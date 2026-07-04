using System.Text;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ISO11820System.Models;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

namespace ISO11820System.Services
{
    /// <summary>
    /// 导出服务（CSV/Excel/PDF — PDF 使用 QuestPDF 原生支持中文）
    /// </summary>
    public class ExportService
    {
        private static bool _fontRegistered = false;
        private static string _defaultFontFamily = "Arial"; // 兜底字体

        public ExportService()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            QuestPDF.Settings.License = LicenseType.Community;
            RegisterChineseFont();
        }

        /// <summary>
        /// 注册中文字体（QuestPDF一次注册，全局生效）
        /// 自动检测系统可用字体，优先微软雅黑，次选宋体，最终回退Arial
        /// </summary>
        private static void RegisterChineseFont()
        {
            if (_fontRegistered) return;
            _fontRegistered = true;

            var fontsDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

            // 按优先级尝试注册中文字体：微软雅黑 → 宋体 → 微软雅黑粗体
            var candidates = new[] {
                ("msyh.ttc", "Microsoft YaHei"),
                ("simsun.ttc", "SimSun"),
                ("msyhbd.ttc", "Microsoft YaHei")
            };
            foreach (var (fileName, fontFamily) in candidates)
            {
                var path = Path.Combine(fontsDir, fileName);
                if (File.Exists(path))
                {
                    try
                    {
                        using var fs = File.OpenRead(path);
                        QuestPDF.Drawing.FontManager.RegisterFont(fs);
                        _defaultFontFamily = fontFamily; // 记录成功注册的字体
                        break;
                    }
                    catch { }
                }
            }

            // 同时也注册 Arial 作为兜底
            var arialPath = Path.Combine(fontsDir, "arial.ttf");
            if (File.Exists(arialPath))
            {
                try
                {
                    using var afs = File.OpenRead(arialPath);
                    QuestPDF.Drawing.FontManager.RegisterFont(afs);
                }
                catch { }
            }
        }

        // ======================== CSV ========================

        public void ExportCsv(List<(int Time, double TF1, double TF2, double TS, double TC, double TCal)> data, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Time,Temp1,Temp2,TempSurface,TempCenter,TempCalibration");
            foreach (var row in data)
                sb.AppendLine($"{row.Time},{row.TF1:F1},{row.TF2:F1},{row.TS:F1},{row.TC:F1},{row.TCal:F1}");

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, sb.ToString());
        }

        // ======================== Excel ========================

        public void ExportExcel(TestMaster test, List<(int Time, double TF1, double TF2, double TS, double TC, double TCal)> data, string filePath)
        {
            if (data == null || data.Count == 0) return;

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            using var package = new ExcelPackage(new FileInfo(filePath));

            // Sheet1: 试验信息
            var sheet1 = package.Workbook.Worksheets.Add("试验信息");
            sheet1.Cells["A1"].Value = "ISO 11820 建筑材料不燃性试验报告";
            sheet1.Cells["A1"].Style.Font.Size = 16;
            sheet1.Cells["A1"].Style.Font.Bold = true;

            int row = 3;
            var fields = new (string, string)[]
            {
                ("样品编号:", test.ProductId), ("试验ID:", test.TestId),
                ("试验日期:", test.TestDate.ToString("yyyy-MM-dd")), ("操作员:", test.Operator),
                ("环境温度:", $"{test.AmbientTemp:F1} °C"), ("环境湿度:", $"{test.AmbientHumidity:F1} %"), ("", ""),
                ("试验前质量:", $"{test.PreWeight:F2} g"), ("试验后质量:", $"{test.PostWeight:F2} g"),
                ("失重量:", $"{test.LostWeight:F2} g"), ("失重率:", $"{test.LostWeightPercent:F2} %"), ("", ""),
                ("样品温升:", $"{test.DeltaTF:F1} °C"), ("炉温1温升:", $"{test.DeltaTF1:F1} °C"),
                ("炉温2温升:", $"{test.DeltaTF2:F1} °C"), ("试验时长:", $"{test.TotalTestTime} 秒"), ("", ""),
                ("火焰持续时间:", $"{test.FlameDuration} 秒"), ("备注:", test.Memo ?? ""), ("", ""),
            };

            foreach (var (label, value) in fields)
            {
                sheet1.Cells[$"A{row}"].Value = label;
                sheet1.Cells[$"B{row}"].Value = value;
                row++;
            }

            bool passed = test.DeltaTF <= 50 && test.LostWeightPercent <= 50 && test.FlameDuration < 5;
            sheet1.Cells[$"A{row}"].Value = "判定结论:";
            sheet1.Cells[$"B{row}"].Value = passed ? "合格" : "不合格";
            sheet1.Cells[$"B{row}"].Style.Font.Bold = true;
            sheet1.Cells[$"B{row}"].Style.Font.Color.SetColor(passed
                ? System.Drawing.Color.Green : System.Drawing.Color.Red);
            sheet1.Column(1).Width = 20; sheet1.Column(2).Width = 35;

            // Sheet2: 温度数据
            var sheet2 = package.Workbook.Worksheets.Add("温度数据");
            var headers = new[] { "时间(s)", "炉温1(°C)", "炉温2(°C)", "表面温(°C)", "中心温(°C)", "校准温(°C)" };
            for (int i = 0; i < headers.Length; i++)
                sheet2.Cells[1, i + 1].Value = headers[i];
            sheet2.Cells["A1:F1"].Style.Font.Bold = true;

            for (int i = 0; i < data.Count; i++)
            {
                var d = data[i];
                sheet2.Cells[i + 2, 1].Value = d.Time; sheet2.Cells[i + 2, 2].Value = d.TF1;
                sheet2.Cells[i + 2, 3].Value = d.TF2; sheet2.Cells[i + 2, 4].Value = d.TS;
                sheet2.Cells[i + 2, 5].Value = d.TC; sheet2.Cells[i + 2, 6].Value = d.TCal;
            }
            sheet2.Cells[sheet2.Dimension.Address].AutoFitColumns();

            // Sheet3: 温度曲线图
            var sheet3 = package.Workbook.Worksheets.Add("温度曲线");
            CreateChart(sheet3, data);

            package.Save();
        }

        private void CreateChart(OfficeOpenXml.ExcelWorksheet sheet,
            List<(int Time, double TF1, double TF2, double TS, double TC, double TCal)> data)
        {
            if (data.Count == 0) return;
            sheet.Cells["A1"].Value = "时间(s)"; sheet.Cells["B1"].Value = "炉温1";
            sheet.Cells["C1"].Value = "炉温2"; sheet.Cells["D1"].Value = "表面温";
            sheet.Cells["E1"].Value = "中心温";
            for (int i = 0; i < data.Count; i++)
            {
                sheet.Cells[i + 2, 1].Value = data[i].Time; sheet.Cells[i + 2, 2].Value = data[i].TF1;
                sheet.Cells[i + 2, 3].Value = data[i].TF2; sheet.Cells[i + 2, 4].Value = data[i].TS;
                sheet.Cells[i + 2, 5].Value = data[i].TC;
            }
            var chart = sheet.Drawings.AddChart("Chart", eChartType.Line);
            chart.Title.Text = "温度曲线图"; chart.SetPosition(0, 0, 6, 0); chart.SetSize(800, 500);
            var end = data.Count + 1;
            chart.Series.Add(sheet.Cells[$"B2:B{end}"], sheet.Cells[$"A2:A{end}"]).Header = "炉温1";
            chart.Series.Add(sheet.Cells[$"C2:C{end}"], sheet.Cells[$"A2:A{end}"]).Header = "炉温2";
            chart.Series.Add(sheet.Cells[$"D2:D{end}"], sheet.Cells[$"A2:A{end}"]).Header = "表面温";
            chart.Series.Add(sheet.Cells[$"E2:E{end}"], sheet.Cells[$"A2:A{end}"]).Header = "中心温";
            chart.YAxis.MinValue = 0; chart.YAxis.MaxValue = 800;
            chart.YAxis.Title.Text = "温度(°C)"; chart.XAxis.Title.Text = "时间(s)";
        }

        // ======================== PDF（QuestPDF，原生中文支持）========================

        public void ExportPdf(TestMaster test,
            List<(int Time, double TF1, double TF2, double TS, double TC, double TCal)> data,
            string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            bool passed = test.DeltaTF <= 50 && test.LostWeightPercent <= 50 && test.FlameDuration < 5;

            // 预生成温度曲线图 PNG
            byte[]? chartPng = GenerateChartPng(data);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontFamily(_defaultFontFamily).FontSize(11));

                    page.Content().Column(col =>
                    {
                        // 标题
                        col.Item().AlignCenter().Text("ISO 11820 建筑材料不燃性试验报告")
                            .FontSize(18).Bold();
                        col.Item().PaddingTop(5).AlignCenter().Text("—".PadRight(40, '—'))
                            .FontSize(10).FontColor(Colors.Grey.Medium);

                        col.Item().PaddingTop(20);

                        // 基本信息
                        col.Item().Text(t => {
                            t.Span("样品编号: ").Bold(); t.Span(test.ProductId);
                        });
                        col.Item().Text(t => {
                            t.Span("试验ID: ").Bold(); t.Span(test.TestId);
                        });
                        col.Item().Text(t => {
                            t.Span("试验日期: ").Bold(); t.Span(test.TestDate.ToString("yyyy-MM-dd"));
                        });
                        col.Item().Text(t => {
                            t.Span("操作员: ").Bold(); t.Span(test.Operator);
                        });
                        col.Item().Text(t => {
                            t.Span("设备: ").Bold(); t.Span(test.ApparatusName);
                        });
                        col.Item().Text(t => {
                            t.Span("环境条件: ").Bold();
                            t.Span($"{test.AmbientTemp:F1}°C / {test.AmbientHumidity:F1}%");
                        });

                        col.Item().PaddingTop(10);

                        // 试验结果
                        col.Item().PaddingTop(5).Text("试验结果").FontSize(14).Bold();
                        col.Item().PaddingTop(5);

                        col.Item().Text(t => {
                            t.Span("试验前质量: "); t.Span($"{test.PreWeight:F2} g");
                        });
                        col.Item().Text(t => {
                            t.Span("试验后质量: "); t.Span($"{test.PostWeight:F2} g");
                        });
                        col.Item().Text(t => {
                            t.Span("失重量: "); t.Span($"{test.LostWeight:F2} g");
                        });
                        col.Item().Text(t => {
                            t.Span("失重率: "); t.Span($"{test.LostWeightPercent:F2} %");
                        });

                        col.Item().PaddingTop(3);

                        col.Item().Text(t => {
                            t.Span("炉温1温升: "); t.Span($"{test.DeltaTF1:F1} °C");
                        });
                        col.Item().Text(t => {
                            t.Span("炉温2温升: "); t.Span($"{test.DeltaTF2:F1} °C");
                        });
                        col.Item().Text(t => {
                            t.Span("样品温升(判定项): "); t.Span($"{test.DeltaTF:F1} °C");
                        });
                        col.Item().Text(t => {
                            t.Span("试验时长: "); t.Span($"{test.TotalTestTime} 秒");
                        });

                        col.Item().PaddingTop(3);

                        if (test.FlameDuration > 0)
                        {
                            col.Item().Text(t => {
                                t.Span("火焰发生时刻: "); t.Span($"{test.FlameTime} 秒");
                            });
                            col.Item().Text(t => {
                                t.Span("火焰持续时间: "); t.Span($"{test.FlameDuration} 秒");
                            });
                        }
                        else
                        {
                            col.Item().Text("火焰: 无持续火焰");
                        }

                        if (!string.IsNullOrEmpty(test.Memo))
                            col.Item().Text(t => { t.Span("备注: "); t.Span(test.Memo); });

                        // 温度曲线图（嵌入PDF）
                        if (chartPng != null && chartPng.Length > 0)
                        {
                            col.Item().PaddingTop(15);
                            col.Item().Text("温度曲线").FontSize(14).Bold();
                            col.Item().PaddingTop(5);
                            col.Item().MaxWidth(500).Image(chartPng, ImageScaling.FitWidth);
                        }

                        col.Item().PaddingTop(10);
                        col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
                        col.Item().PaddingTop(10);

                        // 判定结论
                        col.Item().Text("判定结论").FontSize(14).Bold();
                        col.Item().PaddingTop(5);

                        var resultColor = passed ? Colors.Green.Darken2 : Colors.Red.Darken2;
                        col.Item().Text(passed ? "合  格" : "不合格")
                            .FontSize(22).Bold().FontColor(resultColor);

                        col.Item().PaddingTop(10);

                        col.Item().Text("判定标准: 样品温升 ≤ 50°C | 失重率 ≤ 50% | 火焰持续时间 < 5秒").FontSize(10);
                        col.Item().PaddingTop(5);

                        string checkMark(double val, double limit) => val <= limit ? "✓" : "✗";
                        col.Item().Text(t => {
                            t.Span($"温升: {test.DeltaTF:F1}°C {checkMark(test.DeltaTF, 50)}");
                            t.Span("    ");
                            t.Span($"失重率: {test.LostWeightPercent:F2}% {checkMark(test.LostWeightPercent, 50)}");
                            t.Span("    ");
                            t.Span($"火焰: {test.FlameDuration}秒 {checkMark(test.FlameDuration, 5)}");
                        });

                        col.Item().PaddingTop(20);
                        col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                        col.Item().PaddingTop(5);

                        col.Item().Text($"报告生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}    ISO 11820 试验系统")
                            .FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            }).GeneratePdf(filePath);
        }

        // ======================== 查询结果导出 ========================

        /// <summary>
        /// 生成温度曲线图 PNG 字节数组（OxyPlot 渲染 → 内存流）
        /// </summary>
        private static byte[]? GenerateChartPng(
            List<(int Time, double TF1, double TF2, double TS, double TC, double TCal)> data)
        {
            if (data == null || data.Count == 0) return null;

            var model = new PlotModel { Title = "温度曲线" };
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "温度 (°C)",
                Minimum = 0,
                Maximum = 800
            });
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "时间 (s)"
            });

            model.Series.Add(new LineSeries { Title = "炉温1", Color = OxyColor.FromRgb(255, 0, 0), StrokeThickness = 1 });
            model.Series.Add(new LineSeries { Title = "炉温2", Color = OxyColor.FromRgb(255, 165, 0), StrokeThickness = 1 });
            model.Series.Add(new LineSeries { Title = "表面温", Color = OxyColor.FromRgb(0, 0, 255), StrokeThickness = 1 });
            model.Series.Add(new LineSeries { Title = "中心温", Color = OxyColor.FromRgb(0, 180, 0), StrokeThickness = 1 });
            model.IsLegendVisible = true;

            foreach (var d in data)
            {
                ((LineSeries)model.Series[0]).Points.Add(new DataPoint(d.Time, d.TF1));
                ((LineSeries)model.Series[1]).Points.Add(new DataPoint(d.Time, d.TF2));
                ((LineSeries)model.Series[2]).Points.Add(new DataPoint(d.Time, d.TS));
                ((LineSeries)model.Series[3]).Points.Add(new DataPoint(d.Time, d.TC));
            }

            using var ms = new MemoryStream();
            var exporter = new OxyPlot.WindowsForms.PngExporter { Width = 800, Height = 400, Resolution = 96 };
            exporter.Export(model, ms);
            return ms.ToArray();
        }

        public void ExportQueryResults(List<TestMaster> tests, string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            using var package = new ExcelPackage(new FileInfo(filePath));
            var sheet = package.Workbook.Worksheets.Add("查询结果");

            var headers = new[] { "样品编号", "试验ID", "试验日期", "操作员",
                "试验前质量(g)", "试验后质量(g)", "失重率(%)", "样品温升(°C)", "试验时长(秒)", "判定" };
            for (int i = 0; i < headers.Length; i++)
                sheet.Cells[1, i + 1].Value = headers[i];
            sheet.Cells["A1:J1"].Style.Font.Bold = true;

            for (int i = 0; i < tests.Count; i++)
            {
                var t = tests[i]; int r = i + 2;
                bool p = t.DeltaTF <= 50 && t.LostWeightPercent <= 50 && t.FlameDuration < 5;
                sheet.Cells[r, 1].Value = t.ProductId; sheet.Cells[r, 2].Value = t.TestId;
                sheet.Cells[r, 3].Value = t.TestDate.ToString("yyyy-MM-dd"); sheet.Cells[r, 4].Value = t.Operator;
                sheet.Cells[r, 5].Value = t.PreWeight; sheet.Cells[r, 6].Value = t.PostWeight;
                sheet.Cells[r, 7].Value = t.LostWeightPercent; sheet.Cells[r, 8].Value = t.DeltaTF;
                sheet.Cells[r, 9].Value = t.TotalTestTime; sheet.Cells[r, 10].Value = p ? "合格" : "不合格";
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
            package.Save();
        }
    }
}


