namespace Driver.Application.Ports;

/// <summary>
/// Matching algorithm tuning, bound from configuration in the composition root (Api layer)
/// instead of the handler reaching into IConfiguration directly.
/// </summary>
public interface IMatchingWeights
{
  double DistanceWeight { get; }
  double RatingWeight { get; }
  double UserMinRating { get; }
  double UserMaxRating { get; }
}
