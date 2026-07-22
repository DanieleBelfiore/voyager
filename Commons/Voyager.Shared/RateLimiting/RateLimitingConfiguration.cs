using System.Collections.Generic;

namespace Voyager.Shared.RateLimiting;

/// <summary>
/// Configuration for API rate limiting policies (token bucket / fixed window per endpoint).
/// </summary>
public class RateLimitingConfiguration
{
  public int PermitLimit { get; set; } = 100;
  public int Window { get; set; } = 60;
  public int QueueLimit { get; set; } = 2;
  public Dictionary<string, EndpointLimit> EndpointLimits { get; set; } = [];
}

public class EndpointLimit
{
  public int PermitLimit { get; set; }
  public int Window { get; set; }
  public string Policy { get; set; }
}
