using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Core.Exceptions;
using Common.Core.Validation;
using Driver.Core.CQRS.Queries;
using Driver.Core.Dtos;
using Driver.Core.Enums;
using Driver.Handlers.Interfaces;
using Identity.Core.CQRS.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Geometries;

namespace Driver.Handlers.CQRS.Queries;

/// <summary>
/// Implements the driver matching algorithm.
/// Ranks available drivers based on:
/// - Geographic proximity to rider
/// - Driver rating
/// - Historical performance
///
/// Uses a weighted scoring system where:
/// - Distance weight: Higher priority for closer drivers
/// - Rating weight: Factors in driver quality
/// </summary>
public class SearchBestDriverHandler(IDriverContext db, IMediator mediator, IConfiguration configuration) : IRequestHandler<SearchBestDriver, List<SearchBestDriverResponse>>
{
  public async Task<List<SearchBestDriverResponse>> Handle(SearchBestDriver request, CancellationToken cancellationToken)
  {
    // A null centre made the spatial predicate below throw inside EF's translation; the
    // threshold is already clamped by the controller, but this handler is also reachable through
    // the mediator, so the divisor is guarded here rather than trusted from the caller.
    GeoGuard.Required(request.Location, "location");

    if (request.DistanceThresholdInMeters <= 0)
      throw new InvalidInputException("distance_threshold_out_of_range");

    var drivers = await db.Drivers.AsNoTracking()
      .Where(f => f.Status == DriverStatus.Available && f.LastLocation != null
        && f.LastLocation.Distance(request.Location) <= request.DistanceThresholdInMeters)
      .Select(f => new { f.Id, Distance = f.LastLocation!.Distance(request.Location) })
      // Nearest-first then capped: an unbounded threshold otherwise materialises every
      // available driver and feeds all their ids into GetUsersRatings as one IN (...).
      .OrderBy(f => f.Distance)
      .Take(configuration.GetValue<int?>("MaxCandidates") ?? 200)
      .ToListAsync(cancellationToken);

    if (drivers.Count == 0)
      return [];

    var driverIds = drivers.Select(d => d.Id).ToList();
    var userRatings = await GetDriverRatings(driverIds, cancellationToken);

    var result = (
      from driver in drivers
      // A driver with no ratings yet defaults to the midpoint of the rating range, not the
      // floor — treating "no ratings" the same as "worst possible rating" would unfairly bury
      // brand-new drivers in the ranking.
      let driverRating = userRatings.GetValueOrDefault(driver.Id, (configuration.GetValue<double>("UserMinRating") + configuration.GetValue<double>("UserMaxRating")) / 2)
      let normalizedDistance = driver.Distance / request.DistanceThresholdInMeters
      let normalizedRating = (driverRating - configuration.GetValue<double>("UserMinRating")) / (configuration.GetValue<double>("UserMaxRating") - configuration.GetValue<double>("UserMinRating"))
      let score = configuration.GetValue<double>("DistanceWeight") * normalizedDistance + configuration.GetValue<double>("RatingWeight") * (1 - normalizedRating)
      select new SearchBestDriverResponse
      {
        DriverId = driver.Id,
        Distance = driver.Distance,
        Score = score
      }).ToList();

    return [.. result.OrderBy(f => f.Score)];
  }

  private async Task<Dictionary<Guid, double>> GetDriverRatings(List<Guid> driverIds, CancellationToken cancellationToken)
  {
    return await mediator.Send(new GetUsersRatings { UserIds = driverIds }, cancellationToken) ?? new Dictionary<Guid, double>();
  }
}
