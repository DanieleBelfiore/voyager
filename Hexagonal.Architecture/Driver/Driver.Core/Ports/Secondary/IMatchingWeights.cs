namespace Driver.Core.Ports.Secondary;

public interface IMatchingWeights
{
  double DistanceWeight { get; }
  double RatingWeight { get; }
  double UserMinRating { get; }
  double UserMaxRating { get; }

  /// <summary>
  /// Hard ceiling on how many candidates the proximity query may return. Without it a large
  /// DistanceThresholdInMeters pulls every available driver into memory and then into the
  /// GetUsersRatings id list, which becomes an IN (...) that can exceed SQL Server's
  /// 2100-parameter limit. The nearest N are taken first, then scored among themselves.
  /// </summary>
  int MaxCandidates { get; }
}
