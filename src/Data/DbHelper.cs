using Microsoft.Data.Sqlite;
using ISO11820System.Models;
using System.Text.Json;

namespace ISO11820System.Data
{
    /// <summary>
    /// 数据库操作辅助类
    /// </summary>
    public class DbHelper
    {
        private readonly string _connectionString;

        public DbHelper(string dbPath)
        {
            _connectionString = $"Data Source={dbPath}";
            InitializeDatabase();
        }

        /// <summary>
        /// 初始化数据库（创建表和初始数据）
        /// </summary>
        private void InitializeDatabase()
        {
            // 确保数据库文件所在目录存在
            var dbFile = _connectionString.Replace("Data Source=", "");
            var directory = Path.GetDirectoryName(dbFile);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            // 创建表
            CreateTables(conn);

            // 插入初始数据
            InsertInitialData(conn);
        }

        private void CreateTables(SqliteConnection conn)
        {
            var commands = new[]
            {
                // operators表
                @"CREATE TABLE IF NOT EXISTS operators (
                    userid TEXT NOT NULL,
                    username TEXT NOT NULL,
                    pwd TEXT NOT NULL,
                    usertype TEXT NOT NULL
                )",

                // apparatus表
                @"CREATE TABLE IF NOT EXISTS apparatus (
                    apparatusid INTEGER NOT NULL PRIMARY KEY,
                    innernumber TEXT NOT NULL,
                    apparatusname TEXT NOT NULL,
                    checkdatef DATE NOT NULL,
                    checkdatet DATE NOT NULL,
                    pidport TEXT NOT NULL,
                    powerport TEXT NOT NULL,
                    constpower INTEGER NULL
                )",

                // productmaster表
                @"CREATE TABLE IF NOT EXISTS productmaster (
                    productid TEXT NOT NULL PRIMARY KEY,
                    productname TEXT NOT NULL,
                    specific TEXT NOT NULL,
                    diameter REAL NOT NULL,
                    height REAL NOT NULL,
                    flag TEXT NULL
                )",

                // testmaster表
                @"CREATE TABLE IF NOT EXISTS testmaster (
                    productid TEXT NOT NULL,
                    testid TEXT NOT NULL,
                    testdate DATE NOT NULL,
                    ambtemp REAL NOT NULL,
                    ambhumi REAL NOT NULL,
                    according TEXT NOT NULL,
                    operator TEXT NOT NULL,
                    apparatusid TEXT NOT NULL,
                    apparatusname TEXT NOT NULL,
                    apparatuschkdate DATE NOT NULL,
                    rptno TEXT NOT NULL,
                    preweight REAL NOT NULL,
                    postweight REAL NOT NULL,
                    lostweight REAL NOT NULL,
                    lostweight_per REAL NOT NULL,
                    totaltesttime INTEGER NOT NULL,
                    constpower INTEGER NOT NULL,
                    phenocode TEXT NOT NULL,
                    flametime INTEGER NOT NULL,
                    flameduration INTEGER NOT NULL,
                    maxtf1 REAL NOT NULL, maxtf2 REAL NOT NULL, maxts REAL NOT NULL, maxtc REAL NOT NULL,
                    maxtf1_time INTEGER NOT NULL, maxtf2_time INTEGER NOT NULL, maxts_time INTEGER NOT NULL, maxtc_time INTEGER NOT NULL,
                    finaltf1 REAL NOT NULL, finaltf2 REAL NOT NULL, finalts REAL NOT NULL, finaltc REAL NOT NULL,
                    finaltf1_time INTEGER NOT NULL, finaltf2_time INTEGER NOT NULL, finalts_time INTEGER NOT NULL, finaltc_time INTEGER NOT NULL,
                    deltatf1 REAL NOT NULL, deltatf2 REAL NOT NULL, deltatf REAL NOT NULL, deltats REAL NOT NULL, deltatc REAL NOT NULL,
                    memo TEXT NULL,
                    flag TEXT NULL,
                    PRIMARY KEY (productid, testid),
                    FOREIGN KEY (productid) REFERENCES productmaster (productid)
                )",

                // sensors表
                @"CREATE TABLE IF NOT EXISTS sensors (
                    sensorid INTEGER NOT NULL PRIMARY KEY,
                    sensorname TEXT NOT NULL,
                    dispname TEXT NOT NULL,
                    sensorgroup TEXT NOT NULL,
                    unit TEXT NOT NULL,
                    discription TEXT NOT NULL,
                    flag TEXT NOT NULL,
                    signalzero REAL NOT NULL,
                    signalspan REAL NOT NULL,
                    outputzero REAL NOT NULL,
                    outputspan REAL NOT NULL,
                    outputvalue REAL NOT NULL,
                    inputvalue REAL NOT NULL,
                    signaltype INTEGER NOT NULL
                )",

                // CalibrationRecords表
                @"CREATE TABLE IF NOT EXISTS CalibrationRecords (
                    Id TEXT NOT NULL PRIMARY KEY,
                    CalibrationDate TEXT NOT NULL,
                    CalibrationType TEXT NOT NULL,
                    ApparatusId INTEGER NOT NULL,
                    Operator TEXT NOT NULL,
                    TemperatureData TEXT NOT NULL,
                    UniformityResult REAL NULL,
                    MaxDeviation REAL NULL,
                    AverageTemperature REAL NULL,
                    PassedCriteria INTEGER NOT NULL,
                    Remarks TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    TempA1 REAL NULL, TempA2 REAL NULL, TempA3 REAL NULL,
                    TempB1 REAL NULL, TempB2 REAL NULL, TempB3 REAL NULL,
                    TempC1 REAL NULL, TempC2 REAL NULL, TempC3 REAL NULL,
                    TAvg REAL NULL, TAvgAxis1 REAL NULL, TAvgAxis2 REAL NULL, TAvgAxis3 REAL NULL,
                    TAvgLevela REAL NULL, TAvgLevelb REAL NULL, TAvgLevelc REAL NULL,
                    TDevAxis1 REAL NULL, TDevAxis2 REAL NULL, TDevAxis3 REAL NULL,
                    TDevLevela REAL NULL, TDevLevelb REAL NULL, TDevLevelc REAL NULL,
                    TAvgDevAxis REAL NULL, TAvgDevLevel REAL NULL,
                    CenterTempData TEXT NULL,
                    Memo TEXT NULL
                )",

                // 索引
                "CREATE INDEX IF NOT EXISTS IX_Testmaster_Testdate ON testmaster (testdate)",
                "CREATE INDEX IF NOT EXISTS IX_Testmaster_Operator ON testmaster (operator)",
                "CREATE INDEX IF NOT EXISTS IX_Testmaster_Testdate_Productid ON testmaster (testdate, productid)",
                "CREATE INDEX IF NOT EXISTS IX_CalibrationRecord_Date ON CalibrationRecords (CalibrationDate)",
                "CREATE INDEX IF NOT EXISTS IX_CalibrationRecord_Operator ON CalibrationRecords (Operator)"
            };

            foreach (var cmdText in commands)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = cmdText;
                cmd.ExecuteNonQuery();
            }
        }

        private void InsertInitialData(SqliteConnection conn)
        {
            // 插入默认操作员
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM operators";
                var count = Convert.ToInt32(cmd.ExecuteScalar());
                if (count == 0)
                {
                    cmd.CommandText = @"
                        INSERT INTO operators (userid, username, pwd, usertype) VALUES
                        ('1', 'admin', '123456', 'admin'),
                        ('2', 'experimenter', '123456', 'operator')";
                    cmd.ExecuteNonQuery();
                }
            }

            // 插入默认设备
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM apparatus";
                var count = Convert.ToInt32(cmd.ExecuteScalar());
                if (count == 0)
                {
                    cmd.CommandText = @"
                        INSERT INTO apparatus VALUES
                        (0, 'FURNACE-01', '一号试验炉', date('now'), date('now', '+1 year'), 'COM9', 'COM9', 2048)";
                    cmd.ExecuteNonQuery();
                }
            }

            // 插入传感器配置（包含所有17个通道：0~16）
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM sensors";
                var count = Convert.ToInt32(cmd.ExecuteScalar());
                if (count == 0)
                {
                    cmd.CommandText = @"
                        INSERT INTO sensors VALUES
                        (0,'Sensor0','炉温1','采集','℃','炉温1','启用',0,0,0,1000,0,0,4),
                        (1,'Sensor1','炉温2','采集','℃','炉温2','启用',0,0,0,1000,0,0,4),
                        (2,'Sensor2','表面温度','采集','℃','表面温度','启用',0,0,0,1000,0,0,4),
                        (3,'Sensor3','中心温度','采集','℃','中心温度','启用',0,0,0,1000,0,0,4),
                        (4,'Sensor4','备用通道5','备用','℃','备用通道5','备用',0,0,0,1000,0,0,4),
                        (5,'Sensor5','备用通道6','备用','℃','备用通道6','备用',0,0,0,1000,0,0,4),
                        (6,'Sensor6','备用通道7','备用','℃','备用通道7','备用',0,0,0,1000,0,0,4),
                        (7,'Sensor7','备用通道8','备用','℃','备用通道8','备用',0,0,0,1000,0,0,4),
                        (8,'Sensor8','备用通道9','备用','℃','备用通道9','备用',0,0,0,1000,0,0,4),
                        (9,'Sensor9','备用通道10','备用','℃','备用通道10','备用',0,0,0,1000,0,0,4),
                        (10,'Sensor10','备用通道11','备用','℃','备用通道11','备用',0,0,0,1000,0,0,4),
                        (11,'Sensor11','备用通道12','备用','℃','备用通道12','备用',0,0,0,1000,0,0,4),
                        (12,'Sensor12','备用通道13','备用','℃','备用通道13','备用',0,0,0,1000,0,0,4),
                        (13,'Sensor13','备用通道14','备用','℃','备用通道14','备用',0,0,0,1000,0,0,4),
                        (14,'Sensor14','备用通道15','备用','℃','备用通道15','备用',0,0,0,1000,0,0,4),
                        (15,'Sensor15','备用通道16','备用','℃','备用通道16','备用',0,0,0,1000,0,0,4),
                        (16,'Sensor16','校准温度','校准','℃','校准温度','启用',0,0,0,1000,0,0,4)";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ===== 登录验证 =====
        public bool Login(string username, string password, out Operator? user)
        {
            user = null;
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT userid, username, pwd, usertype FROM operators WHERE username=@name AND pwd=@pwd";
            cmd.Parameters.AddWithValue("@name", username);
            cmd.Parameters.AddWithValue("@pwd", password);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                user = new Operator
                {
                    UserId = reader.GetString(0),
                    Username = reader.GetString(1),
                    Password = reader.GetString(2),
                    UserType = reader.GetString(3)
                };
                return true;
            }
            return false;
        }

        // ===== 获取所有操作员列表 =====
        public List<Operator> GetOperators()
        {
            var result = new List<Operator>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT userid, username, pwd, usertype FROM operators ORDER BY userid";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new Operator
                {
                    UserId = reader.GetString(0),
                    Username = reader.GetString(1),
                    Password = reader.GetString(2),
                    UserType = reader.GetString(3)
                });
            }
            return result;
        }

        // ===== 获取设备信息 =====
        public Apparatus? GetApparatus(int apparatusId = 0)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM apparatus WHERE apparatusid=@id";
            cmd.Parameters.AddWithValue("@id", apparatusId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Apparatus
                {
                    ApparatusId = reader.GetInt32(0),
                    InnerNumber = reader.GetString(1),
                    ApparatusName = reader.GetString(2),
                    CheckDateFrom = DateTime.Parse(reader.GetString(3)),
                    CheckDateTo = DateTime.Parse(reader.GetString(4)),
                    PidPort = reader.GetString(5),
                    PowerPort = reader.GetString(6),
                    ConstPower = reader.IsDBNull(7) ? null : reader.GetInt32(7)
                };
            }
            return null;
        }

        // ===== 保存样品信息 =====
        public void SaveProduct(ProductMaster product)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
                INSERT OR REPLACE INTO productmaster (productid, productname, specific, diameter, height, flag)
                VALUES (@id, @name, @spec, @dia, @height, @flag)";
            cmd.Parameters.AddWithValue("@id", product.ProductId);
            cmd.Parameters.AddWithValue("@name", product.ProductName);
            cmd.Parameters.AddWithValue("@spec", product.Specific);
            cmd.Parameters.AddWithValue("@dia", product.Diameter);
            cmd.Parameters.AddWithValue("@height", product.Height);
            cmd.Parameters.AddWithValue("@flag", product.Flag ?? "");

            try
            {
                cmd.ExecuteNonQuery();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // ===== 创建新试验记录（初始值）=====
        public void CreateTestRecord(TestMaster test)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
                INSERT INTO testmaster (
                    productid, testid, testdate, operator, ambtemp, ambhumi,
                    according, apparatusid, apparatusname, apparatuschkdate, rptno,
                    preweight, postweight, lostweight, lostweight_per,
                    totaltesttime, constpower, phenocode, flametime, flameduration,
                    maxtf1,maxtf2,maxts,maxtc, maxtf1_time,maxtf2_time,maxts_time,maxtc_time,
                    finaltf1,finaltf2,finalts,finaltc, finaltf1_time,finaltf2_time,finalts_time,finaltc_time,
                    deltatf1,deltatf2,deltatf,deltats,deltatc, memo, flag
                ) VALUES (
                    @pid,@tid,@date,@op,@temp,@humi,
                    @accord,@aid,@aname,@achk,@rpt,
                    @pre,0,0,0,
                    0,0,'',0,0,
                    0,0,0,0,0,0,0,0,
                    0,0,0,0,0,0,0,0,
                    0,0,0,0,0,'',''
                )";
            cmd.Parameters.AddWithValue("@pid", test.ProductId);
            cmd.Parameters.AddWithValue("@tid", test.TestId);
            cmd.Parameters.AddWithValue("@date", test.TestDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@op", test.Operator);
            cmd.Parameters.AddWithValue("@temp", test.AmbientTemp);
            cmd.Parameters.AddWithValue("@humi", test.AmbientHumidity);
            cmd.Parameters.AddWithValue("@accord", test.According);
            cmd.Parameters.AddWithValue("@aid", test.ApparatusId);
            cmd.Parameters.AddWithValue("@aname", test.ApparatusName);
            cmd.Parameters.AddWithValue("@achk", test.ApparatusCheckDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@rpt", test.ReportNo);
            cmd.Parameters.AddWithValue("@pre", test.PreWeight);

            try
            {
                cmd.ExecuteNonQuery();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        // ===== 更新试验结果 =====
        public void UpdateTestResult(TestMaster test)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE testmaster SET
                    postweight=@post, lostweight=@lost, lostweight_per=@lostper,
                    totaltesttime=@time, constpower=@power, phenocode=@pheno,
                    flametime=@ftime, flameduration=@fdur,
                    maxtf1=@mtf1, maxtf2=@mtf2, maxts=@mts, maxtc=@mtc,
                    maxtf1_time=@mtf1t, maxtf2_time=@mtf2t, maxts_time=@mtst, maxtc_time=@mtct,
                    finaltf1=@ftf1, finaltf2=@ftf2, finalts=@fts, finaltc=@ftc,
                    finaltf1_time=@ftf1t, finaltf2_time=@ftf2t, finalts_time=@ftst, finaltc_time=@ftct,
                    deltatf1=@dtf1, deltatf2=@dtf2, deltatf=@dtf, deltats=@dts, deltatc=@dtc,
                    memo=@memo, flag=@flag
                WHERE productid=@pid AND testid=@tid";

            cmd.Parameters.AddWithValue("@pid", test.ProductId);
            cmd.Parameters.AddWithValue("@tid", test.TestId);
            cmd.Parameters.AddWithValue("@post", test.PostWeight);
            cmd.Parameters.AddWithValue("@lost", test.LostWeight);
            cmd.Parameters.AddWithValue("@lostper", test.LostWeightPercent);
            cmd.Parameters.AddWithValue("@time", test.TotalTestTime);
            cmd.Parameters.AddWithValue("@power", test.ConstPower);
            cmd.Parameters.AddWithValue("@pheno", test.PhenoCode);
            cmd.Parameters.AddWithValue("@ftime", test.FlameTime);
            cmd.Parameters.AddWithValue("@fdur", test.FlameDuration);
            cmd.Parameters.AddWithValue("@mtf1", test.MaxTF1);
            cmd.Parameters.AddWithValue("@mtf2", test.MaxTF2);
            cmd.Parameters.AddWithValue("@mts", test.MaxTS);
            cmd.Parameters.AddWithValue("@mtc", test.MaxTC);
            cmd.Parameters.AddWithValue("@mtf1t", test.MaxTF1Time);
            cmd.Parameters.AddWithValue("@mtf2t", test.MaxTF2Time);
            cmd.Parameters.AddWithValue("@mtst", test.MaxTSTime);
            cmd.Parameters.AddWithValue("@mtct", test.MaxTCTime);
            cmd.Parameters.AddWithValue("@ftf1", test.FinalTF1);
            cmd.Parameters.AddWithValue("@ftf2", test.FinalTF2);
            cmd.Parameters.AddWithValue("@fts", test.FinalTS);
            cmd.Parameters.AddWithValue("@ftc", test.FinalTC);
            cmd.Parameters.AddWithValue("@ftf1t", test.FinalTF1Time);
            cmd.Parameters.AddWithValue("@ftf2t", test.FinalTF2Time);
            cmd.Parameters.AddWithValue("@ftst", test.FinalTSTime);
            cmd.Parameters.AddWithValue("@ftct", test.FinalTCTime);
            cmd.Parameters.AddWithValue("@dtf1", test.DeltaTF1);
            cmd.Parameters.AddWithValue("@dtf2", test.DeltaTF2);
            cmd.Parameters.AddWithValue("@dtf", test.DeltaTF);
            cmd.Parameters.AddWithValue("@dts", test.DeltaTS);
            cmd.Parameters.AddWithValue("@dtc", test.DeltaTC);
            cmd.Parameters.AddWithValue("@memo", test.Memo ?? "");
            cmd.Parameters.AddWithValue("@flag", test.Flag ?? "");
            cmd.ExecuteNonQuery();
        }

        // ===== 查询试验记录列表（含操作员过滤）=====
        public List<TestMaster> QueryTests(DateTime? fromDate = null, DateTime? toDate = null, string? productId = null, string? operatorName = null)
        {
            var results = new List<TestMaster>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();

            var sql = "SELECT * FROM testmaster WHERE 1=1";
            if (fromDate.HasValue)
                sql += " AND testdate >= @from";
            if (toDate.HasValue)
                sql += " AND testdate <= @to";
            if (!string.IsNullOrEmpty(productId))
                sql += " AND productid LIKE '%' || @pid || '%'";
            if (!string.IsNullOrEmpty(operatorName))
                sql += " AND operator = @op";
            sql += " ORDER BY testdate DESC";

            cmd.CommandText = sql;
            if (fromDate.HasValue) cmd.Parameters.AddWithValue("@from", fromDate.Value.ToString("yyyy-MM-dd"));
            if (toDate.HasValue) cmd.Parameters.AddWithValue("@to", toDate.Value.ToString("yyyy-MM-dd"));
            if (!string.IsNullOrEmpty(productId)) cmd.Parameters.AddWithValue("@pid", productId);
            if (!string.IsNullOrEmpty(operatorName)) cmd.Parameters.AddWithValue("@op", operatorName);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new TestMaster
                {
                    ProductId = reader.GetString(0),
                    TestId = reader.GetString(1),
                    TestDate = DateTime.Parse(reader.GetString(2)),
                    AmbientTemp = reader.GetDouble(3),
                    AmbientHumidity = reader.GetDouble(4),
                    According = reader.GetString(5),
                    Operator = reader.GetString(6),
                    ApparatusId = reader.GetString(7),
                    ApparatusName = reader.GetString(8),
                    ApparatusCheckDate = DateTime.Parse(reader.GetString(9)),
                    ReportNo = reader.GetString(10),
                    PreWeight = reader.GetDouble(11),
                    PostWeight = reader.GetDouble(12),
                    LostWeight = reader.GetDouble(13),
                    LostWeightPercent = reader.GetDouble(14),
                    TotalTestTime = reader.GetInt32(15),
                    ConstPower = reader.GetInt32(16),
                    PhenoCode = reader.GetString(17),
                    FlameTime = reader.GetInt32(18),
                    FlameDuration = reader.GetInt32(19),
                    MaxTF1 = reader.GetDouble(20),
                    MaxTF2 = reader.GetDouble(21),
                    MaxTS = reader.GetDouble(22),
                    MaxTC = reader.GetDouble(23),
                    MaxTF1Time = reader.GetInt32(24),
                    MaxTF2Time = reader.GetInt32(25),
                    MaxTSTime = reader.GetInt32(26),
                    MaxTCTime = reader.GetInt32(27),
                    FinalTF1 = reader.GetDouble(28),
                    FinalTF2 = reader.GetDouble(29),
                    FinalTS = reader.GetDouble(30),
                    FinalTC = reader.GetDouble(31),
                    FinalTF1Time = reader.GetInt32(32),
                    FinalTF2Time = reader.GetInt32(33),
                    FinalTSTime = reader.GetInt32(34),
                    FinalTCTime = reader.GetInt32(35),
                    DeltaTF1 = reader.GetDouble(36),
                    DeltaTF2 = reader.GetDouble(37),
                    DeltaTF = reader.GetDouble(38),
                    DeltaTS = reader.GetDouble(39),
                    DeltaTC = reader.GetDouble(40),
                    Memo = reader.IsDBNull(41) ? null : reader.GetString(41),
                    Flag = reader.IsDBNull(42) ? null : reader.GetString(42)
                });
            }
            return results;
        }

        // ===== 获取单个试验详情 =====
        public TestMaster? GetTest(string productId, string testId)
        {
            var tests = QueryTests(productId: productId);
            return tests.FirstOrDefault(t => t.TestId == testId);
        }

        // ===== 设备校准记录 CRUD =====

        /// <summary>
        /// 保存校准记录
        /// </summary>
        public void SaveCalibrationRecord(CalibrationRecord record)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO CalibrationRecords (
                    Id, CalibrationDate, CalibrationType, ApparatusId, Operator,
                    TemperatureData, UniformityResult, MaxDeviation, AverageTemperature,
                    PassedCriteria, Remarks, CreatedAt,
                    TempA1,TempA2,TempA3, TempB1,TempB2,TempB3, TempC1,TempC2,TempC3,
                    TAvg, TAvgAxis1,TAvgAxis2,TAvgAxis3,
                    TAvgLevela,TAvgLevelb,TAvgLevelc,
                    TDevAxis1,TDevAxis2,TDevAxis3,
                    TDevLevela,TDevLevelb,TDevLevelc,
                    TAvgDevAxis, TAvgDevLevel, CenterTempData, Memo
                ) VALUES (
                    @id,@date,@type,@aid,@op,
                    @tempData,@ur,@md,@at,
                    @pc,@remarks,@created,
                    @ta1,@ta2,@ta3, @tb1,@tb2,@tb3, @tc1,@tc2,@tc3,
                    @tavg,@tax1,@tax2,@tax3,
                    @talva,@talvb,@talvc,
                    @tdx1,@tdx2,@tdx3,
                    @tdla,@tdlb,@tdlc,
                    @tadax,@tadl, @ctd, @memo
                )";

            cmd.Parameters.AddWithValue("@id", record.Id);
            cmd.Parameters.AddWithValue("@date", record.CalibrationDate.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@type", record.CalibrationType);
            cmd.Parameters.AddWithValue("@aid", record.ApparatusId);
            cmd.Parameters.AddWithValue("@op", record.Operator);
            cmd.Parameters.AddWithValue("@tempData", record.TemperatureData);
            cmd.Parameters.AddWithValue("@ur", (object?)record.UniformityResult ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@md", (object?)record.MaxDeviation ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@at", (object?)record.AverageTemperature ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@pc", record.PassedCriteria);
            cmd.Parameters.AddWithValue("@remarks", record.Remarks);
            cmd.Parameters.AddWithValue("@created", record.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

            cmd.Parameters.AddWithValue("@ta1", (object?)record.TempA1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ta2", (object?)record.TempA2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ta3", (object?)record.TempA3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tb1", (object?)record.TempB1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tb2", (object?)record.TempB2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tb3", (object?)record.TempB3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tc1", (object?)record.TempC1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tc2", (object?)record.TempC2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tc3", (object?)record.TempC3 ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@tavg", (object?)record.TAvg ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tax1", (object?)record.TAvgAxis1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tax2", (object?)record.TAvgAxis2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tax3", (object?)record.TAvgAxis3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@talva", (object?)record.TAvgLevela ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@talvb", (object?)record.TAvgLevelb ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@talvc", (object?)record.TAvgLevelc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tdx1", (object?)record.TDevAxis1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tdx2", (object?)record.TDevAxis2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tdx3", (object?)record.TDevAxis3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tdla", (object?)record.TDevLevela ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tdlb", (object?)record.TDevLevelb ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tdlc", (object?)record.TDevLevelc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tadax", (object?)record.TAvgDevAxis ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tadl", (object?)record.TAvgDevLevel ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ctd", (object?)record.CenterTempData ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@memo", (object?)record.Memo ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 查询校准历史记录
        /// </summary>
        public List<CalibrationRecord> QueryCalibrations(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var results = new List<CalibrationRecord>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();

            var sql = "SELECT * FROM CalibrationRecords WHERE 1=1";
            if (fromDate.HasValue)
                sql += " AND CalibrationDate >= @from";
            if (toDate.HasValue)
                sql += " AND CalibrationDate <= @to";
            sql += " ORDER BY CalibrationDate DESC";

            cmd.CommandText = sql;
            if (fromDate.HasValue) cmd.Parameters.AddWithValue("@from", fromDate.Value.ToString("yyyy-MM-dd"));
            if (toDate.HasValue) cmd.Parameters.AddWithValue("@to", toDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(ReadCalibrationRecord(reader));
            }
            return results;
        }

        private CalibrationRecord ReadCalibrationRecord(SqliteDataReader reader)
        {
            return new CalibrationRecord
            {
                Id = reader.GetString(0),
                CalibrationDate = DateTime.Parse(reader.GetString(1)),
                CalibrationType = reader.GetString(2),
                ApparatusId = reader.GetInt32(3),
                Operator = reader.GetString(4),
                TemperatureData = reader.GetString(5),
                UniformityResult = reader.IsDBNull(6) ? null : reader.GetDouble(6),
                MaxDeviation = reader.IsDBNull(7) ? null : reader.GetDouble(7),
                AverageTemperature = reader.IsDBNull(8) ? null : reader.GetDouble(8),
                PassedCriteria = reader.GetInt32(9),
                Remarks = reader.GetString(10),
                CreatedAt = DateTime.Parse(reader.GetString(11)),
                TempA1 = reader.IsDBNull(12) ? null : reader.GetDouble(12),
                TempA2 = reader.IsDBNull(13) ? null : reader.GetDouble(13),
                TempA3 = reader.IsDBNull(14) ? null : reader.GetDouble(14),
                TempB1 = reader.IsDBNull(15) ? null : reader.GetDouble(15),
                TempB2 = reader.IsDBNull(16) ? null : reader.GetDouble(16),
                TempB3 = reader.IsDBNull(17) ? null : reader.GetDouble(17),
                TempC1 = reader.IsDBNull(18) ? null : reader.GetDouble(18),
                TempC2 = reader.IsDBNull(19) ? null : reader.GetDouble(19),
                TempC3 = reader.IsDBNull(20) ? null : reader.GetDouble(20),
                TAvg = reader.IsDBNull(21) ? null : reader.GetDouble(21),
                TAvgAxis1 = reader.IsDBNull(22) ? null : reader.GetDouble(22),
                TAvgAxis2 = reader.IsDBNull(23) ? null : reader.GetDouble(23),
                TAvgAxis3 = reader.IsDBNull(24) ? null : reader.GetDouble(24),
                TAvgLevela = reader.IsDBNull(25) ? null : reader.GetDouble(25),
                TAvgLevelb = reader.IsDBNull(26) ? null : reader.GetDouble(26),
                TAvgLevelc = reader.IsDBNull(27) ? null : reader.GetDouble(27),
                TDevAxis1 = reader.IsDBNull(28) ? null : reader.GetDouble(28),
                TDevAxis2 = reader.IsDBNull(29) ? null : reader.GetDouble(29),
                TDevAxis3 = reader.IsDBNull(30) ? null : reader.GetDouble(30),
                TDevLevela = reader.IsDBNull(31) ? null : reader.GetDouble(31),
                TDevLevelb = reader.IsDBNull(32) ? null : reader.GetDouble(32),
                TDevLevelc = reader.IsDBNull(33) ? null : reader.GetDouble(33),
                TAvgDevAxis = reader.IsDBNull(34) ? null : reader.GetDouble(34),
                TAvgDevLevel = reader.IsDBNull(35) ? null : reader.GetDouble(35),
                CenterTempData = reader.IsDBNull(36) ? null : reader.GetString(36),
                Memo = reader.IsDBNull(37) ? null : reader.GetString(37)
            };
        }

        // ===== 获取所有操作员名称列表（用于下拉筛选）=====
        public List<string> GetOperatorNames()
        {
            var result = new List<string>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT username FROM operators ORDER BY username";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(reader.GetString(0));
            }
            return result;
        }
    }
}

