using System;
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
  ICacheService cache,
  IMatchingWeights weights) : ISearchBestDriverUseCase
{
  private const string CacheKeyPrefix = "drivers:available";
  private static readonly TimeSpan CacheExpiration = TimeSpan.FromSeconds(30);

  public async Task<List<SearchBestDriverResponse>> Handle(SearchBestDriver request, CancellationToken cancellationToken)
  {
    var availableDrivers = await cache.GetOrCreateAsync(CacheKeyPrefix,
      () => repository.GetAvailableWithLocationAsync(cancellationToken), CacheExpiration);

    var drivers = availableDrivers.Where(d => d.LastLocation!.Distance(request.Location) <= request.DistanceThresholdInMeters).ToList();
    if (drivers.Count == 0)
      return [];

    var driverIds = drivers.Select(d => d.Id).ToList();
    var userRatings = await ratingsQuery.GetRatingsAsync(driverIds, cancellationToken);

    var result = (
      from driver in drivers
      let distance = driver.LastLocation!.Distance(request.Location)
      let driverRating = userRatings.GetValueOrDefault(driver.Id, 0.0)
      let normalizedDistance = distance / request.DistanceThresholdInMeters
      let normalizedRating = (driverRating - weights.UserMinRating) / (weights.UserMaxRating - weights.UserMinRating)
      let score = weights.DistanceWeight * normalizedDistance + weights.RatingWeight * (1 - normalizedRating)
      select new SearchBestDriverResponse
      {
        DriverId = driver.Id,
        LastLocation = driver.LastLocation,
        LastUpdateDate = driver.LastUpdateDate,
        Distance = distance,
        Score = score
      }).ToList();

    return [.. result.OrderBy(f => f.Score)];
  }
}
