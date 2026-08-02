namespace Driver.Module.Features.SearchBestDriver;

internal class MatchingWeights
{
  public double DistanceWeight { get; set; }
  public double RatingWeight { get; set; }
  public double UserMinRating { get; set; }
  public double UserMaxRating { get; set; }

  /// <summary>
  /// Hard ceiling on how many candidates the proximity query may return. Without it a large
  /// DistanceThresholdInMeters pulls every available driver into memory and then into the
  /// GetUsersRatings id list, which becomes an IN (...) that can exceed SQL Server's
  /// 2100-parameter limit. The nearest N are taken first, then scored among themselves.
  /// </summary>
  public int MaxCandidates { get; set; } = 200;
}
