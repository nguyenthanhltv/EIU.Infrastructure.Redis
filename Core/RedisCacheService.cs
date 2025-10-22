using StackExchange.Redis;
using System.Text.Json;

namespace EIU.Infrastructure.Redis.Core
{
    /// <summary>
    /// Service thao tác trực tiếp với Redis (get/set/remove)
    /// </summary>
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IConnectionMultiplexer _connection;

        public RedisCacheService(IConnectionMultiplexer connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Lấy dữ liệu từ cache và deserialize về kiểu T
        /// </summary>
        public async Task<T?> GetAsync<T>(string key)
        {
            var db = _connection.GetDatabase();
            var value = await db.StringGetAsync(key);
            if (value.IsNullOrEmpty) return default;
            return JsonSerializer.Deserialize<T>(value!);
        }

        /// <summary>
        /// Lấy dữ liệu cache dưới dạng chuỗi JSON
        /// </summary>
        public async Task<string?> GetAsync(string key)
        {
            var db = _connection.GetDatabase();
            var value = await db.StringGetAsync(key);
            return value.IsNullOrEmpty ? null : value.ToString();
        }

        /// <summary>
        /// Lưu dữ liệu vào cache, có thể chỉ định thời gian hết hạn
        /// </summary>
        public async Task SetAsync(string key, object value, TimeSpan? expiry = null)
        {
            var db = _connection.GetDatabase();
            var json = JsonSerializer.Serialize(value, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
            await db.StringSetAsync(key, json, expiry ?? TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Xóa cache theo key cụ thể
        /// </summary>
        public async Task RemoveAsync(string key)
        {
            var db = _connection.GetDatabase();
            await db.KeyDeleteAsync(key);
        }

        /// <summary>
        /// Xóa toàn bộ cache có prefix nhất định (ví dụ: "eiu:student:")
        /// </summary>
        public async Task RemoveByPrefixAsync(string prefix)
        {
            var server = _connection.GetServer(_connection.GetEndPoints().First());
            var db = _connection.GetDatabase();
            var keys = server.Keys(pattern: $"{prefix}*").ToArray();

            foreach (var key in keys)
                await db.KeyDeleteAsync(key);
        }
    }
}
