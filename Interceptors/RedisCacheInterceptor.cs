using Castle.DynamicProxy;
using EIU.Infrastructure.Redis.Attributes;
using EIU.Infrastructure.Redis.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;

namespace EIU.Infrastructure.Redis.Interceptors
{
    /// <summary>
    /// Interceptor dùng để cache ở tầng Service (qua Castle DynamicProxy)
    /// </summary>
    public class RedisCacheInterceptor : IInterceptor
    {
        private readonly IServiceProvider _serviceProvider;

        public RedisCacheInterceptor(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Intercept(IInvocation invocation)
        {
            var method = invocation.MethodInvocationTarget ?? invocation.Method;

            var cacheAttr = method.GetCustomAttributes(typeof(RedisCacheAttribute), false)
                                  .FirstOrDefault() as RedisCacheAttribute;
            var removeAttr = method.GetCustomAttributes(typeof(RedisCacheRemoveAttribute), false)
                                   .FirstOrDefault() as RedisCacheRemoveAttribute;

            var cacheService = _serviceProvider.GetRequiredService<IRedisCacheService>();
            var options = _serviceProvider.GetRequiredService<IOptions<RedisCacheOptions>>().Value;

            var project = options.ProjectAlias ?? "default";
            var controller = method.DeclaringType?.Name ?? "Unknown";

            // Trường hợp có RedisCache → đọc cache
            if (cacheAttr != null)
            {
                string key = BuildCacheKey(project, controller, method.Name, invocation.Arguments);
                var cachedValue = cacheService.GetAsync<string>(key).GetAwaiter().GetResult();

                if (!string.IsNullOrEmpty(cachedValue))
                {
                    var returnType = invocation.Method.ReturnType;
                    if (returnType.IsGenericType)
                    {
                        var innerType = returnType.GenericTypeArguments[0];
                        var deserialized = JsonSerializer.Deserialize(cachedValue, innerType);
                        invocation.ReturnValue = Task.FromResult(deserialized);
                    }
                    else
                    {
                        invocation.ReturnValue = JsonSerializer.Deserialize(cachedValue, returnType);
                    }
                    return;
                }

                invocation.Proceed();

                // Lưu lại kết quả vào Redis
                var task = invocation.ReturnValue as Task;
                if (task != null)
                {
                    task.ContinueWith(async t =>
                    {
                        object? result = null;
                        if (t.GetType().IsGenericType)
                            result = t.GetType().GetProperty("Result")?.GetValue(t);

                        if (result != null)
                        {
                            await cacheService.SetAsync(
                                key,
                                result,
                                TimeSpan.FromSeconds(options.DefaultDurationSeconds)
                            );
                        }
                    });
                }
                return;
            }

            // Trường hợp có RedisCacheRemove → xóa cache
            if (removeAttr != null)
            {
                string prefix = $"{project}:{removeAttr.Entity ?? controller}".ToLowerInvariant();
                cacheService.RemoveByPrefixAsync(prefix).GetAwaiter().GetResult();
            }

            invocation.Proceed();
        }

        private string BuildCacheKey(string project, string controller, string action, object[] args)
        {
            var sb = new StringBuilder($"{project}:{controller}:{action}");
            if (args != null && args.Length > 0)
            {
                foreach (var arg in args)
                {
                    sb.Append(":");
                    sb.Append(JsonSerializer.Serialize(arg));
                }
            }
            return sb.ToString().ToLowerInvariant();
        }
    }
}
