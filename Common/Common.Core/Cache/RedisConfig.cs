namespace Common.Core.Cache
{
  public class RedisConfig
  {
    public string ConnectionString { get; set; }
    public int DefaultExpirationMinutes { get; set; } = 60;
  }
}
