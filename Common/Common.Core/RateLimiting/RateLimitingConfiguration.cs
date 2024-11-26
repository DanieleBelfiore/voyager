using System.Collections.Generic;

namespace Common.Core.RateLimiting
{
  /// <summary>
  /// Configuration for API rate limiting policies.
  /// Implements a token bucket algorithm where:
  /// - PermitLimit: Number of requests allowed in the time window
  /// - Window: Time period in seconds for the permit limit
  /// - QueueLimit: Maximum requests that can be queued when limit is exceeded
  /// 
  /// Endpoints can have specific limits defined to handle different load patterns
  /// Example: Ride requests might have stricter limits than status checks
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
}
