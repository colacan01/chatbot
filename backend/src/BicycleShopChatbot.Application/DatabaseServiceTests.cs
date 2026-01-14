using System;
using System.Threading.Tasks;
using Xunit;
using VectorDataLoader.Services;
using Npgsql;

namespace BicycleShopChatbot.Application.Tests
{
    public class DatabaseServiceTests
    {
        private readonly string _host = "localhost";
        private readonly int _port = 5432;
        private readonly string _database = "testdb";
        private readonly string _username = "testuser";
        private readonly string _password = "testpassword";

        [Fact]
        public async Task TestConnectionAsync_ReturnsTrue_WhenConnectionIsValid()
        {
            var dbService = new DatabaseService(_host, _port, _database, _username, _password);
            var result = await dbService.TestConnectionAsync();
            Assert.True(result);
        }

        [Fact]
        public async Task EnsurePgVectorExtensionAsync_DoesNotThrow()
        {
            var dbService = new DatabaseService(_host, _port, _database, _username, _password);
            await dbService.EnsurePgVectorExtensionAsync();
        }
    }
}
