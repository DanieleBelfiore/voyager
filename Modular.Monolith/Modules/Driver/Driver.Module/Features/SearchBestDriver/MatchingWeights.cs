namespace Driver.Module.Features.SearchBestDriver;

internal class MatchingWeights
{
  public double DistanceWeight { get; set; }
  public double RatingWeight { get; set; }
  public double UserMinRating { get; set; }
  public double UserMaxRating { get; set; }
}
