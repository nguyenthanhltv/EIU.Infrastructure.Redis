using EIU.Infrastructure.Redis.Core;

namespace EIU.Infrastructure.Redis.Manager
{
    /// <summary>
    /// Quản lý logic cache cấp cao, có thể mở rộng cho nhiều use case phức tạp hơn
    /// </summary>
    public class RedisCacheManager : IRedisCacheManager
    {
        private readonly IRedisCacheService _cacheService;

        public RedisCacheManager(IRedisCacheService cacheService)
        {
            _cacheService = cacheService;
        }

        /// <summary>
        /// Lấy dữ liệu cache theo key, hoặc chạy func để cache lại nếu chưa có
        /// </summary>
        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)
        {
            var data = await _cacheService.GetAsync<T>(key);
            if (data != null)
                return data;

            var result = await factory();
            if (result != null)
                await _cacheService.SetAsync(key, result, expiry);

            return result;
        }

        /// <summary>
        /// Xóa cache theo key
        /// </summary>
        public async Task RemoveAsync(string key)
        {
            await _cacheService.RemoveAsync(key);
        }

        /// <summary>
        /// Xóa tất cả cache có prefix (ví dụ: "eiu:student:")
        /// </summary>
        public async Task RemoveByPrefixAsync(string prefix)
        {
            await _cacheService.RemoveByPrefixAsync(prefix);
        }
    }
}
