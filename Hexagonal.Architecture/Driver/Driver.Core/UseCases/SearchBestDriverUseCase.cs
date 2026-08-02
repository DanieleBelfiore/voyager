using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Driver.Core.Dtos;
using Driver.Core.Ports.Primary;
using Driver.Core.Ports.Secondary;

namespace Driver.Core.UseCases;

/// <summary>
/// Driver matching algorithm — same weighted scoring as the other variants:
/// score = (distanceWeight * normalizedDistance) + (ratingWeight * (1 - normalizedRating))
/// </summary>
public class SearchBestDriverUseCase(
  IDriverRepository repository,
  IRatingsQueryService ratingsQuery,
  IMatchingWeights weights) : ISearchBestDriverUseCase
{
  public async Task<List<SearchBestDriverResponse>> Handle(SearchBestDriver request, CancellationToken cancellationToken)
  {
    // No cache here on purpose: driver location/availability changes every few seconds, so a
    // TTL-cached "all available drivers" list is either stale or constantly invalidated. A
    // fresh, spatially-indexed query is both more correct and cheaper than pulling the full
    // table through a cache that adds staleness without saving much.
    var nearbyDrivers = await repository.GetAvailableWithinDistanceAsync(request.Location, request.DistanceThresholdInMeters, cancellationToken);
    if (nearbyDrivers.Count == 0)
      return [];

    var driverIds = nearbyDrivers.Select(d => d.Driver.Id).ToList();
    var userRatings = await ratingsQuery.GetRatingsAsync(driverIds, cancellationToken);

    var result = (
      from nearby in nearbyDrivers
      let driver = nearby.Driver
      // A driver with no ratings yet defaults to the midpoint of the rating range, not the
      // floor — treating "no ratings" the same as "worst possible rating" would unfairly bury
      // brand-new drivers in the ranking.
      let driverRating = userRatings.GetValueOrDefault(driver.Id, (weights.UserMinRating + weights.UserMaxRating) / 2)
      let normalizedDistance = nearby.DistanceInMeters / request.DistanceThresholdInMeters
      let normalizedRating = (driverRating - weights.UserMinRating) / (weights.UserMaxRating - weights.UserMinRating)
      let score = weights.DistanceWeight * normalizedDistance + weights.RatingWeight * (1 - normalizedRating)
      select new SearchBestDriverResponse
      {
        DriverId = driver.Id,
        LastLocation = driver.LastLocation,
        LastUpdateDate = driver.LastUpdateDate,
        Distance = nearby.DistanceInMeters,
        Score = score
      }).ToList();

    return [.. result.OrderBy(f => f.Score)];
  }
}
