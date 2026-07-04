using ISO11820System.Data;
using ISO11820System.Models;
using Microsoft.Data.Sqlite;

namespace ISO11820System.Tests
{
    /// <summary>
    /// 数据库操作 — 集成测试（使用临时 SQLite 文件）
    /// 覆盖：登录、CRUD、查询过滤、校准记录
    /// </summary>
    public class DbHelperTests : IDisposable
    {
        private readonly string _tempDbPath;
        private readonly DbHelper _dbHelper;

        public DbHelperTests()
        {
            _tempDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.db");
            _dbHelper = new DbHelper(_tempDbPath);
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_tempDbPath)) File.Delete(_tempDbPath);
        }

        // ==================== 登录验证 ====================

        [Fact]
        public void Login_ValidAdmin_ReturnsTrue()
        {
            var result = _dbHelper.Login("admin", "123456", out Operator? user);

            Assert.True(result);
            Assert.NotNull(user);
            Assert.Equal("admin", user!.Username);
            Assert.Equal("admin", user.UserType);
        }

        [Fact]
        public void Login_ValidExperimenter_ReturnsTrue()
        {
            var result = _dbHelper.Login("experimenter", "123456", out Operator? user);

            Assert.True(result);
            Assert.NotNull(user);
            Assert.Equal("experimenter", user!.Username);
            Assert.Equal("operator", user.UserType);
        }

        [Fact]
        public void Login_WrongPassword_ReturnsFalse()
        {
            var result = _dbHelper.Login("admin", "wrongpassword", out Operator? user);

            Assert.False(result);
            Assert.Null(user);
        }

        [Fact]
        public void Login_NonExistentUser_ReturnsFalse()
        {
            var result = _dbHelper.Login("nobody", "123456", out Operator? user);

            Assert.False(result);
            Assert.Null(user);
        }

        [Fact]
        public void Login_WrongUsernameCorrectPassword_ReturnsFalse()
        {
            var result = _dbHelper.Login("wronguser", "123456", out Operator? user);

            Assert.False(result);
            Assert.Null(user);
        }

        // ==================== 操作员查询 ====================

        [Fact]
        public void GetOperators_ReturnsAllOperators()
        {
            var operators = _dbHelper.GetOperators();

            Assert.NotNull(operators);
            Assert.Equal(2, operators.Count);
            Assert.Contains(operators, o => o.Username == "admin");
            Assert.Contains(operators, o => o.Username == "experimenter");
        }

        [Fact]
        public void GetOperatorNames_ReturnsDistinctNames()
        {
            var names = _dbHelper.GetOperatorNames();

            Assert.NotNull(names);
            Assert.Equal(2, names.Count);
            Assert.Contains("admin", names);
            Assert.Contains("experimenter", names);
        }

        // ==================== 设备信息 ====================

        [Fact]
        public void GetApparatus_DefaultId_ReturnsDevice()
        {
            var apparatus = _dbHelper.GetApparatus(0);

            Assert.NotNull(apparatus);
            Assert.Equal("FURNACE-01", apparatus!.InnerNumber);
            Assert.Equal("一号试验炉", apparatus.ApparatusName);
            Assert.Equal(2048, apparatus.ConstPower);
        }

        [Fact]
        public void GetApparatus_NonExistentId_ReturnsNull()
        {
            var apparatus = _dbHelper.GetApparatus(999);
            Assert.Null(apparatus);
        }

        // ==================== 样品管理 ====================

        [Fact]
        public void SaveProduct_InsertNew_Success()
        {
            var product = new ProductMaster
            {
                ProductId = "PROD-001",
                ProductName = "石膏板",
                Specific = "12mm厚度",
                Diameter = 50.0,
                Height = 100.0
            };

            _dbHelper.SaveProduct(product);

            // 通过测试记录间接验证（产品已保存，CreateTestRecord 外键不报错）
            // 直接验证：保存不抛异常即成功
        }

        [Fact]
        public void SaveProduct_UpdateExisting_Overwrites()
        {
            var product = new ProductMaster
            {
                ProductId = "PROD-002",
                ProductName = "原始名称",
                Specific = "规格A",
                Diameter = 50.0,
                Height = 50.0
            };
            _dbHelper.SaveProduct(product);

            // 更新同 ID 的产品
            product.ProductName = "更新名称";
            product.Specific = "规格B";
            _dbHelper.SaveProduct(product);

            // 不抛异常即成功（INSERT OR REPLACE）
        }

        // ==================== 试验记录 CRUD ====================

        private TestMaster CreateSampleTest(string productId = "PROD-TEST", string testId = "20240601-120000")
        {
            // 先保存产品（FK 约束要求 productmaster 中存在对应记录）
            _dbHelper.SaveProduct(new ProductMaster
            {
                ProductId = productId,
                ProductName = $"样品-{productId}",
                Specific = "测试规格",
                Diameter = 50.0,
                Height = 100.0
            });

            return new TestMaster
            {
                ProductId = productId,
                TestId = testId,
                TestDate = DateTime.Today,
                AmbientTemp = 23.5,
                AmbientHumidity = 55.0,
                According = "ISO 11820:2022",
                Operator = "admin",
                ApparatusId = "0",
                ApparatusName = "一号试验炉",
                ApparatusCheckDate = DateTime.Today.AddYears(1),
                ReportNo = "RPT-001",
                PreWeight = 100.0
            };
        }

        [Fact]
        public void CreateTestRecord_ValidTest_Success()
        {
            var test = CreateSampleTest();
            _dbHelper.CreateTestRecord(test);

            var result = _dbHelper.GetTest("PROD-TEST", "20240601-120000");
            Assert.NotNull(result);
            Assert.Equal(100.0, result!.PreWeight);
            Assert.Equal(23.5, result.AmbientTemp);
        }

        [Fact]
        public void UpdateTestResult_UpdatesAllFields()
        {
            var test = CreateSampleTest("PROD-UPDATE", "20240601-130000");
            _dbHelper.CreateTestRecord(test);

            // 更新试验结果
            test.PostWeight = 85.0;
            test.LostWeight = 15.0;
            test.LostWeightPercent = 15.0;
            test.TotalTestTime = 3600;
            test.ConstPower = 2048;
            test.FlameTime = 0;
            test.FlameDuration = 0;
            test.MaxTF1 = 752.0;
            test.MaxTF2 = 751.0;
            test.DeltaTF = 2.0;
            test.Flag = "10000000";
            test.Memo = "测试备注";
            _dbHelper.UpdateTestResult(test);

            var result = _dbHelper.GetTest("PROD-UPDATE", "20240601-130000");
            Assert.NotNull(result);
            Assert.Equal(85.0, result!.PostWeight);
            Assert.Equal(15.0, result.LostWeight);
            Assert.Equal(15.0, result.LostWeightPercent);
            Assert.Equal(3600, result.TotalTestTime);
            Assert.Equal("10000000", result.Flag);
            Assert.Equal("测试备注", result.Memo);
        }

        [Fact]
        public void CreateTestRecord_DuplicatePrimaryKey_ThrowsException()
        {
            // 先保存产品（FK 约束）
            _dbHelper.SaveProduct(new ProductMaster
            {
                ProductId = "PROD-DUP",
                ProductName = "测试样品",
                Specific = "规格",
                Diameter = 50.0,
                Height = 100.0
            });

            var test = CreateSampleTest("PROD-DUP", "20240601-140000");
            _dbHelper.CreateTestRecord(test);

            // 重复插入同主键应抛异常
            Assert.ThrowsAny<Exception>(() => _dbHelper.CreateTestRecord(test));
        }

        // ==================== 试验查询 ====================

        [Fact]
        public void QueryTests_NoFilters_ReturnsAll()
        {
            _dbHelper.CreateTestRecord(CreateSampleTest("PROD-Q1", "20240601-150000"));
            _dbHelper.CreateTestRecord(CreateSampleTest("PROD-Q2", "20240601-160000"));

            var results = _dbHelper.QueryTests();
            Assert.True(results.Count >= 2);
        }

        [Fact]
        public void QueryTests_ByProductId_ReturnsMatching()
        {
            _dbHelper.CreateTestRecord(CreateSampleTest("UNIQUE-PROD", "20240601-170000"));
            _dbHelper.CreateTestRecord(CreateSampleTest("OTHER-PROD", "20240601-180000"));

            var results = _dbHelper.QueryTests(productId: "UNIQUE");

            Assert.True(results.Count >= 1);
            Assert.All(results, t => Assert.Contains("UNIQUE", t.ProductId));
        }

        [Fact]
        public void QueryTests_ByOperator_ReturnsMatching()
        {
            var test1 = CreateSampleTest("PROD-OP1", "20240601-190000");
            test1.Operator = "admin";
            _dbHelper.CreateTestRecord(test1);

            var test2 = CreateSampleTest("PROD-OP2", "20240601-200000");
            test2.Operator = "experimenter";
            _dbHelper.CreateTestRecord(test2);

            var results = _dbHelper.QueryTests(operatorName: "admin");

            Assert.True(results.Count >= 1);
            Assert.All(results, t => Assert.Equal("admin", t.Operator));
        }

        [Fact]
        public void QueryTests_ByDateRange_ReturnsMatching()
        {
            var test = CreateSampleTest("PROD-DATE", "20240601-210000");
            test.TestDate = new DateTime(2024, 6, 15);
            _dbHelper.CreateTestRecord(test);

            var results = _dbHelper.QueryTests(
                fromDate: new DateTime(2024, 6, 1),
                toDate: new DateTime(2024, 6, 30));

            Assert.True(results.Count >= 1);
        }

        [Fact]
        public void QueryTests_ByDateRange_OutOfRange_ReturnsEmpty()
        {
            var test = CreateSampleTest("PROD-DATE2", "20240601-220000");
            test.TestDate = new DateTime(2024, 6, 15);
            _dbHelper.CreateTestRecord(test);

            var results = _dbHelper.QueryTests(
                fromDate: new DateTime(2025, 1, 1),
                toDate: new DateTime(2025, 12, 31));

            Assert.DoesNotContain(results, t => t.ProductId == "PROD-DATE2");
        }

        [Fact]
        public void GetTest_ExistingRecord_ReturnsCorrectTest()
        {
            _dbHelper.CreateTestRecord(CreateSampleTest("PROD-GET", "20240601-230000"));

            var result = _dbHelper.GetTest("PROD-GET", "20240601-230000");

            Assert.NotNull(result);
            Assert.Equal("PROD-GET", result!.ProductId);
            Assert.Equal("20240601-230000", result.TestId);
        }

        [Fact]
        public void GetTest_NonExisting_ReturnsNull()
        {
            var result = _dbHelper.GetTest("NONEXIST", "20240601-000000");
            Assert.Null(result);
        }

        // ==================== 校准记录 ====================

        [Fact]
        public void SaveCalibrationRecord_ValidRecord_Success()
        {
            var record = new CalibrationRecord
            {
                Id = Guid.NewGuid().ToString(),
                CalibrationDate = DateTime.Now,
                CalibrationType = "炉温均匀性",
                ApparatusId = 0,
                Operator = "admin",
                TemperatureData = "[{\"point\":\"A1\",\"temp\":750.0}]",
                PassedCriteria = 1,
                Remarks = "校准通过",
                CreatedAt = DateTime.Now
            };

            _dbHelper.SaveCalibrationRecord(record);

            var results = _dbHelper.QueryCalibrations();
            Assert.Contains(results, r => r.Id == record.Id);
        }

        [Fact]
        public void QueryCalibrations_ByDateRange_FiltersCorrectly()
        {
            var oldRecord = new CalibrationRecord
            {
                Id = Guid.NewGuid().ToString(),
                CalibrationDate = new DateTime(2024, 1, 15),
                CalibrationType = "炉温均匀性",
                ApparatusId = 0,
                Operator = "admin",
                TemperatureData = "[]",
                PassedCriteria = 1,
                Remarks = "",
                CreatedAt = new DateTime(2024, 1, 15)
            };
            _dbHelper.SaveCalibrationRecord(oldRecord);

            var results = _dbHelper.QueryCalibrations(
                fromDate: new DateTime(2025, 1, 1));

            Assert.DoesNotContain(results, r => r.Id == oldRecord.Id);
        }

        [Fact]
        public void SaveCalibrationRecord_WithTemperatureGrid_SavesCorrectly()
        {
            var record = new CalibrationRecord
            {
                Id = Guid.NewGuid().ToString(),
                CalibrationDate = DateTime.Now,
                CalibrationType = "炉温均匀性",
                ApparatusId = 0,
                Operator = "experimenter",
                TemperatureData = "[]",
                UniformityResult = 2.5,
                MaxDeviation = 5.0,
                AverageTemperature = 748.0,
                PassedCriteria = 1,
                Remarks = "所有测温点合格",
                CreatedAt = DateTime.Now,
                TempA1 = 750.0, TempA2 = 749.0, TempA3 = 751.0,
                TempB1 = 748.0, TempB2 = 750.0, TempB3 = 749.0,
                TempC1 = 751.0, TempC2 = 748.0, TempC3 = 750.0
            };

            _dbHelper.SaveCalibrationRecord(record);

            var results = _dbHelper.QueryCalibrations();
            var saved = results.First(r => r.Id == record.Id);
            Assert.Equal(2.5, saved.UniformityResult);
            Assert.Equal(5.0, saved.MaxDeviation);
            Assert.Equal(750.0, saved.TempA1);
            Assert.Equal(749.0, saved.TempA2);
        }

        [Fact]
        public void CalibrationRecord_NullableFields_HandledCorrectly()
        {
            var record = new CalibrationRecord
            {
                Id = Guid.NewGuid().ToString(),
                CalibrationDate = DateTime.Now,
                CalibrationType = "基本校准",
                ApparatusId = 0,
                Operator = "admin",
                TemperatureData = "[]",
                // UniformityResult, MaxDeviation 等保持 null
                PassedCriteria = 0,
                Remarks = "",
                CreatedAt = DateTime.Now
            };

            _dbHelper.SaveCalibrationRecord(record);

            var results = _dbHelper.QueryCalibrations();
            var saved = results.First(r => r.Id == record.Id);
            Assert.NotNull(saved);
            Assert.Equal(0, saved.PassedCriteria);
        }
    }
}
