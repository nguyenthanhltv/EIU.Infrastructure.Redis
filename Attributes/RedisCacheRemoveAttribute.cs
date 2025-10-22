using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using EIU.Infrastructure.Redis.Core;
using Microsoft.Extensions.Logging;

namespace EIU.Infrastructure.Redis.Attributes
{
    /// <summary>
    /// Attribute dùng để xóa cache khi dữ liệu thay đổi (POST/PUT/DELETE)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RedisCacheRemoveAttribute : Attribute, IAsyncActionFilter
    {
        /// <summary>
        /// Tên entity (bảng / controller) cần xóa cache.
        /// Nếu null → tự động dùng tên controller.
        /// </summary>
        public string? Entity { get; }

        public RedisCacheRemoveAttribute(string? entity = null)
        {
            Entity = entity;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var result = await next();

            if (result.Exception != null)
                return;

            // Resolve service từ DI
            var serviceProvider = context.HttpContext.RequestServices;
            var cacheService = serviceProvider.GetRequiredService<IRedisCacheService>();
            var options = serviceProvider.GetRequiredService<IOptions<RedisCacheOptions>>().Value;
            var logger = serviceProvider.GetService<ILogger<RedisCacheRemoveAttribute>>();

            // Nếu Redis bị disable → bỏ qua
            if (!options.Enabled)
            {
                logger?.LogDebug("RedisCacheRemove skipped: Redis is disabled.");
                return;
            }

            var controller = context.Controller.GetType().Name.Replace("Controller", "");
            var project = options.ProjectAlias ?? "default";
            var prefix = $"{project}:{Entity ?? controller}".ToLowerInvariant();

            try
            {
                await cacheService.RemoveByPrefixAsync(prefix);
                logger?.LogInformation("Redis cache removed for prefix: {Prefix}", prefix);
            }
            catch (Exception ex)
            {
                // Không ném lỗi để tránh làm crash request chính
                logger?.LogError(ex, "RedisCacheRemove failed for prefix {Prefix}", prefix);
            }
        }
    }
}
