using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Driver.Api.Entities;
using Driver.Api.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Voyager.Contracts.Identity;

namespace Driver.Api.Features.SearchBestDriver;

/// <summary>
/// Driver matching algorithm — same weighted scoring as the other variants:
/// score = (distanceWeight * normalizedDistance) + (ratingWeight * (1 - normalizedRating)).
/// Ratings are fetched from Identity via IMediator.Send — Arbitrer resolves it remotely since
/// no local handler for GetUsersRatings exists in this process. No dedicated ratings port here.
/// </summary>
public class SearchBestDriverHandler(
  DriverDbContext db,
  IOptions<MatchingWeights> weights,
  IMediator mediator) : IRequestHandler<SearchBestDriver, List<SearchBestDriverResponse>>
{
  public async Task<List<SearchBestDriverResponse>> Handle(SearchBestDriver request, CancellationToken cancellationToken)
  {
    // No cache here on purpose: driver location/availability changes every few seconds, so a
    // TTL-cached "all available drivers" list is either stale or constantly invalidated. A
    // fresh, spatially-indexed query is both more correct and cheaper than pulling the full
    // table through a cache that adds staleness without saving much.
    //
    // Point.Distance() against a geography-typed column translates to SQL Server's STDistance —
    // real geodetic meters, not the planar/Cartesian distance NTS computes in memory — so this
    // is both the exact cutoff and the prefilter in one query, pushed entirely into SQL. Backed
    // by a spatial index on LastLocation (see the AddDriverLastLocationSpatialIndex migration)
    // so the query optimizer isn't forced into a full table scan.
    var drivers = await db.Drivers.AsNoTracking()
      .Where(d => d.Status == DriverStatus.Available && d.LastLocation != null
        && d.LastLocation.Distance(request.Location) <= request.DistanceThresholdInMeters)
      .Select(d => new { d.Id, d.LastLocation, d.LastUpdateDate, Distance = d.LastLocation!.Distance(request.Location) })
      .ToListAsync(cancellationToken);

    if (drivers.Count == 0)
      return [];

    var driverIds = drivers.Select(d => d.Id).ToList();
    var userRatings = await GetRatingsAsync(driverIds, cancellationToken);

    var w = weights.Value;

    var result = (
      from driver in drivers
      // A driver with no ratings yet defaults to the midpoint of the rating range, not the
      // floor — treating "no ratings" the same as "worst possible rating" would unfairly bury
      // brand-new drivers in the ranking.
      let driverRating = userRatings.GetValueOrDefault(driver.Id, (w.UserMinRating + w.UserMaxRating) / 2)
      let normalizedDistance = driver.Distance / request.DistanceThresholdInMeters
      let normalizedRating = (driverRating - w.UserMinRating) / (w.UserMaxRating - w.UserMinRating)
      let score = w.DistanceWeight * normalizedDistance + w.RatingWeight * (1 - normalizedRating)
      select new SearchBestDriverResponse
      {
        DriverId = driver.Id,
        LastLocation = driver.LastLocation,
        LastUpdateDate = driver.LastUpdateDate,
        Distance = driver.Distance,
        Score = score
      }).ToList();

    return [.. result.OrderBy(f => f.Score)];
  }

  private async Task<Dictionary<Guid, double>> GetRatingsAsync(List<Guid> userIds, CancellationToken cancellationToken)
  {
    return await mediator.Send(new GetUsersRatings { UserIds = userIds }, cancellationToken) ?? [];
  }
}
