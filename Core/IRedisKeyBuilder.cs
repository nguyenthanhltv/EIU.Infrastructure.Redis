public interface IRedisKeyBuilder
{
    string Alias { get; }
    string Build(string entity, string context, params object[] identifiers);
}
