using EIU.Infrastructure.Redis.Core;
using EIU.Infrastructure.Redis.Interceptors;
using EIU.Infrastructure.Redis.Manager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EIU.Infrastructure.Redis.Extensions
{
    /// <summary>
    /// Tiện ích mở rộng giúp cấu hình và đăng ký Redis Cache vào Dependency Injection container.
    /// Hỗ trợ đọc cấu hình từ appsettings.json (section mặc định: "RedisCache").
    /// </summary>
    public static class RedisServiceCollectionExtensions
    {
        /// <summary>
        /// 🧩 Đăng ký toàn bộ Redis caching components:
        /// - RedisCacheOptions (từ appsettings)
        /// - RedisCacheService, RedisCacheManager, RedisCacheInterceptor
        /// </summary>
        /// <param name="services">IServiceCollection trong Startup hoặc Program.cs</param>
        /// <param name="configuration">Cấu hình appsettings.json hoặc môi trường</param>
        /// <returns>IServiceCollection để chain tiếp cấu hình</returns>
        public static IServiceCollection AddRedisCaching(
                    this IServiceCollection services,
                    IConfiguration configuration)
        {
            // 1️⃣ Đọc cấu hình RedisCacheOptions (toàn bộ appsettings)
            var redisConfig = new RedisCacheOptions();
            configuration.Bind(redisConfig); // Không cần section, bind toàn bộ

            // 2️⃣ Nếu Redis bị tắt thì bỏ qua
            if (!redisConfig.Enabled)
                return services;

            // 3️⃣ Đăng ký IOptions<RedisCacheOptions> thủ công
            services.AddSingleton<IOptions<RedisCacheOptions>>(
                Options.Create(redisConfig)
            );

            // 4️⃣ Đăng ký RedisCacheOptions trực tiếp (nếu muốn inject không qua IOptions)
            services.AddSingleton(redisConfig);

            // 5️⃣ Đăng ký ConnectionMultiplexer (Redis client)
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                return ConnectionMultiplexer.Connect(redisConfig.ConnectionString);
            });

            // 6️⃣ Đăng ký Redis services
            services.AddSingleton<IRedisCacheService, RedisCacheService>();
            services.AddSingleton<IRedisCacheManager, RedisCacheManager>();
            services.AddSingleton<RedisCacheInterceptor>();

            return services;
        }
    }
}
