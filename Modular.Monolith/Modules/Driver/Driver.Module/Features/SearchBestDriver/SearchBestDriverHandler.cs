using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Driver.Module.Entities;
using Driver.Module.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Voyager.Contracts.Identity;
using Voyager.Shared.Cache;

namespace Driver.Module.Features.SearchBestDriver;

/// <summary>
/// Driver matching algorithm — same weighted scoring as every other variant. Ratings still come
/// from Identity's module via IMediator.Send(GetUsersRatings) — same call as in every other
/// variant, just resolved in-process instead of over RabbitMQ.
/// </summary>
internal class SearchBestDriverHandler(
  DriverDbContext db,
  ICacheService cache,
  IOptions<MatchingWeights> weights,
  IMediator mediator) : IRequestHandler<SearchBestDriver, List<SearchBestDriverResponse>>
{
  private const string CacheKeyPrefix = "drivers:available";
  private static readonly TimeSpan CacheExpiration = TimeSpan.FromSeconds(30);

  public async Task<List<SearchBestDriverResponse>> Handle(SearchBestDriver request, CancellationToken cancellationToken)
  {
    var availableDrivers = await cache.GetOrCreateAsync(CacheKeyPrefix, () => db.Drivers.AsNoTracking()
      .Where(d => d.Status == DriverStatus.Available && d.LastLocation != null)
      .ToListAsync(cancellationToken), CacheExpiration);

    var drivers = availableDrivers.Where(d => d.LastLocation!.Distance(request.Location) <= request.DistanceThresholdInMeters).ToList();
    if (drivers.Count == 0)
      return [];

    var driverIds = drivers.Select(d => d.Id).ToList();
    var userRatings = await mediator.Send(new GetUsersRatings { UserIds = driverIds }, cancellationToken);

    var w = weights.Value;

    var result = (
      from driver in drivers
      let distance = driver.LastLocation!.Distance(request.Location)
      let driverRating = userRatings.GetValueOrDefault(driver.Id, 0.0)
      let normalizedDistance = distance / request.DistanceThresholdInMeters
      let normalizedRating = (driverRating - w.UserMinRating) / (w.UserMaxRating - w.UserMinRating)
      let score = w.DistanceWeight * normalizedDistance + w.RatingWeight * (1 - normalizedRating)
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
