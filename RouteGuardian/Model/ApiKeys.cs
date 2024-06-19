namespace RouteGuardian.Model;

public class ApiKeyVault
{
    public List<ApiKey> ApiKeys { get; set; } = new();
}

public class ApiKey
{
    public string ClientId { get; set; }
    public string ClientName { get; set; }
    public List<string> IpAddresses { get; set; } = new();
    public List<Key> Keys { get; set; } = new();
}

public class Key
{
    public string Secret { get; set; }
    public DateTime ValidUntil { get; set; }
}