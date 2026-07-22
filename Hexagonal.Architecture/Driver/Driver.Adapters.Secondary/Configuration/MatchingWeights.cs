using Driver.Core.Ports.Secondary;

namespace Driver.Adapters.Secondary.Configuration;

public class MatchingWeights : IMatchingWeights
{
  public double DistanceWeight { get; set; }
  public double RatingWeight { get; set; }
  public double UserMinRating { get; set; }
  public double UserMaxRating { get; set; }
}
