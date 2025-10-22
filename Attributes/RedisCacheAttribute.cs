using System.Security.Cryptography;
using System.Text;
using EIU.Infrastructure.Redis.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EIU.Infrastructure.Redis.Attributes
{
    /// <summary>
    /// Attribute dùng để cache dữ liệu trả về của Action (ví dụ GET /api/student/list)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RedisCacheAttribute : Attribute, IAsyncActionFilter
    {
        private readonly int _durationSeconds;

        public RedisCacheAttribute(int durationSeconds = 0)
        {
            _durationSeconds = durationSeconds;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next
        )
        {
            var cacheService =
                context.HttpContext.RequestServices.GetRequiredService<IRedisCacheService>();

            var options = context
                .HttpContext.RequestServices.GetRequiredService<IOptions<RedisCacheOptions>>()
                .Value;

            // 🔧 Nếu cache bị tắt trong cấu hình, bỏ qua
            if (!options.Enabled)
            {
                await next();
                return;
            }

            // Controller + Action name
            var controller = context.Controller.GetType().Name.Replace("Controller", "");
            var action = context.ActionDescriptor.RouteValues["action"];
            var project = options.ProjectAlias ?? "default";

            // Tạo key dựa trên param
            string paramKey = BuildParameterKey(context.ActionArguments, options);
            var cacheKey = $"{project}:{controller}:{action}{paramKey}".ToLowerInvariant();

            // ✅ Kiểm tra có cache chưa
            var cached = await cacheService.GetAsync(cacheKey);
            if (!string.IsNullOrEmpty(cached))
            {
                context.Result = new ContentResult
                {
                    Content = cached,
                    ContentType = "application/json",
                    StatusCode = 200,
                };
                return;
            }

            // ⚙️ Nếu chưa có cache → thực thi thật
            var executed = await next();

            if (executed.Result is ObjectResult result && result.Value != null)
            {
                await cacheService.SetAsync(
                    cacheKey,
                    result.Value,
                    TimeSpan.FromSeconds(
                        _durationSeconds > 0 ? _durationSeconds : options.DefaultDurationSeconds
                    )
                );
            }
        }

        /// <summary>
        /// Sinh phần key phụ dựa trên tham số của action (hỗ trợ DataSourceLoadOptions, DTO hoặc primitive)
        /// </summary>
        private string BuildParameterKey(
            IDictionary<string, object> args,
            RedisCacheOptions options
        )
        {
            if (!options.AutoKeyByParameters || args == null || args.Count == 0)
                return string.Empty;

            try
            {
                // DevExtreme: DataSourceLoadOptions
                var loadOpt = args.Values.FirstOrDefault(v =>
                    v?.GetType().Name == "DataSourceLoadOptions"
                );
                if (loadOpt != null)
                {
                    string json = JsonSerializer.Serialize(loadOpt);
                    return $":dxo:{CreateHash(json)}";
                }

                // Nếu chỉ có 1 object param (paramDto)
                if (
                    args.Count == 1
                    && args.First().Value != null
                    && !IsSimpleType(args.First().Value.GetType())
                )
                {
                    string json = JsonSerializer.Serialize(args.First().Value);
                    return $":obj:{CreateHash(json)}";
                }

                // Nếu là các primitive param (id, name, ...)
                var sb = new StringBuilder();
                foreach (var kv in args)
                    sb.Append($":{kv.Key}-{kv.Value}");
                return sb.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool IsSimpleType(Type type)
        {
            return type.IsPrimitive
                || type.IsEnum
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(Guid);
        }

        private static string CreateHash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).Substring(0, 12).ToLowerInvariant();
        }
    }
}
