using ISO11820System.Data;
using ISO11820System.Models;
using Microsoft.Data.Sqlite;

namespace ISO11820System.Tests
{
    public class DbHelperTests
    {
        [Fact]
        public void Login_ValidAdmin_ReturnsTrue()
        {
            // Arrange: 使用临时 SQLite 数据库（构造函数自动建表并插入初始数据 admin/123456）
            var tempDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.db");
            var dbHelper = new DbHelper(tempDbPath);

            try
            {
                // Act: 使用正确的管理员账号密码登录
                var result = dbHelper.Login("admin", "123456", out Operator? user);

                // Assert: 登录成功
                Assert.True(result);
                Assert.NotNull(user);
                Assert.Equal("admin", user!.Username);
                Assert.Equal("admin", user.UserType);
            }
            finally
            {
                // Cleanup: 释放 SQLite 连接池后再删除临时数据库文件
                SqliteConnection.ClearAllPools();
                if (File.Exists(tempDbPath)) File.Delete(tempDbPath);
            }
        }

        [Fact]
        public void Login_WrongPassword_ReturnsFalse()
        {
            // Arrange: 使用临时 SQLite 数据库
            var tempDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.db");
            var dbHelper = new DbHelper(tempDbPath);

            try
            {
                // Act: 使用正确的用户名但错误的密码登录
                var result = dbHelper.Login("admin", "wrongpassword", out Operator? user);

                // Assert: 登录失败
                Assert.False(result);
                Assert.Null(user);
            }
            finally
            {
                // Cleanup: 释放 SQLite 连接池后再删除临时数据库文件
                SqliteConnection.ClearAllPools();
                if (File.Exists(tempDbPath)) File.Delete(tempDbPath);
            }
        }
    }
}
