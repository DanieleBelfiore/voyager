using Driver.Application.Ports;

namespace Driver.Infrastructure.Configuration;

public class MatchingWeights : IMatchingWeights
{
  public double DistanceWeight { get; set; }
  public double RatingWeight { get; set; }
  public double UserMinRating { get; set; }
  public double UserMaxRating { get; set; }
  public int MaxCandidates { get; set; } = 200;
}
