namespace Driver.Core.Ports.Secondary;

public interface IMatchingWeights
{
  double DistanceWeight { get; }
  double RatingWeight { get; }
  double UserMinRating { get; }
  double UserMaxRating { get; }
}
