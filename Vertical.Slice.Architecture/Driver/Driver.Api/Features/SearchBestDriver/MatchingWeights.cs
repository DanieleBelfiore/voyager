namespace Driver.Api.Features.SearchBestDriver;

/// <summary>Config bound straight from appsettings — feature owns its own config shape, no shared IMatchingWeights port.</summary>
public class MatchingWeights
{
  public double DistanceWeight { get; set; }
  public double RatingWeight { get; set; }
  public double UserMinRating { get; set; }
  public double UserMaxRating { get; set; }
}
