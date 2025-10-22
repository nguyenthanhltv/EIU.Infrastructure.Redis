using Microsoft.Extensions.Configuration;

public class RedisKeyBuilder : IRedisKeyBuilder
{
    public string Alias { get; }

    public RedisKeyBuilder(IConfiguration config)
    {
        Alias = config["Redis:Alias"] ?? "default";
    }

    public string Build(string entity, string context, params object[] identifiers)
    {
        var idPart = identifiers?.Any() == true ? string.Join(":", identifiers) : "all";
        return $"{Alias}:{entity}:{context}:{idPart}".ToLowerInvariant();
    }
}
