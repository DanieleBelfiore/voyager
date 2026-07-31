using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    // No cache here on purpose: driver location/availability changes every few seconds, so a
    // TTL-cached "all available drivers" list is either stale or constantly invalidated. A
    // fresh, bounding-box-narrowed query is both more correct and (thanks to the box) cheaper
    // than pulling the full table through a cache that adds staleness without saving much.
    var (minLat, maxLat, minLon, maxLon) = BoundingBox(request.Location, request.DistanceThresholdInMeters);
    var candidateDrivers = await db.Drivers.AsNoTracking()
      .Where(f => f.Status == DriverStatus.Available && f.LastLocation != null
        && f.LastLocation.Y >= minLat && f.LastLocation.Y <= maxLat
        && f.LastLocation.X >= minLon && f.LastLocation.X <= maxLon)
      .ToListAsync(cancellationToken);

    var drivers = candidateDrivers.Where(d => DistanceInMeters(d.LastLocation!, request.Location) <= request.DistanceThresholdInMeters).ToList();
    if (drivers.Count == 0)
      return [];

    var driverIds = drivers.Select(d => d.Id).ToList();
    var userRatings = await GetDriverRatings(driverIds, cancellationToken);

    var result = (
      from driver in drivers
      let distance = DistanceInMeters(driver.LastLocation!, request.Location)
      // A driver with no ratings yet defaults to the midpoint of the rating range, not the
      // floor — treating "no ratings" the same as "worst possible rating" would unfairly bury
      // brand-new drivers in the ranking.
      let driverRating = userRatings.GetValueOrDefault(driver.Id, (configuration.GetValue<double>("UserMinRating") + configuration.GetValue<double>("UserMaxRating")) / 2)
      let normalizedDistance = distance / request.DistanceThresholdInMeters
      let normalizedRating = (driverRating - configuration.GetValue<double>("UserMinRating")) / (configuration.GetValue<double>("UserMaxRating") - configuration.GetValue<double>("UserMinRating"))
      let score = configuration.GetValue<double>("DistanceWeight") * normalizedDistance + configuration.GetValue<double>("RatingWeight") * (1 - normalizedRating)
      select new SearchBestDriverResponse
      {
        DriverId = driver.Id,
        Distance = distance,
        Score = score
      }).ToList();

    return [.. result.OrderBy(f => f.Score)];
  }

  private async Task<Dictionary<Guid, double>> GetDriverRatings(List<Guid> driverIds, CancellationToken cancellationToken)
  {
    return await mediator.Send(new GetUsersRatings { UserIds = driverIds }, cancellationToken) ?? new Dictionary<Guid, double>();
  }

  // NetTopologySuite's Point.Distance() is planar/Cartesian on the raw coordinate values (SRID
  // is metadata only) — on lat/lon points that returns degrees, not meters. Haversine gives the
  // real great-circle distance so DistanceThresholdInMeters and the scoring are in the same unit.
  private static double DistanceInMeters(Point a, Point b)
  {
    const double earthRadiusMeters = 6371000;

    var lat1 = a.Y * Math.PI / 180;
    var lat2 = b.Y * Math.PI / 180;
    var deltaLat = (b.Y - a.Y) * Math.PI / 180;
    var deltaLon = (b.X - a.X) * Math.PI / 180;

    var h = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
            Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

    return 2 * earthRadiusMeters * Math.Asin(Math.Sqrt(h));
  }

  // Cheap prefilter for the SQL WHERE clause: a lat/lon rectangle around the search point, wide
  // enough to contain every point within radiusMeters. Deliberately not exact — the real cutoff
  // is DistanceInMeters above, applied after this narrows down what has to be fetched at all.
  private static (double MinLat, double MaxLat, double MinLon, double MaxLon) BoundingBox(Point center, double radiusMeters)
  {
    const double metersPerDegreeLatitude = 111320;

    var latDelta = radiusMeters / metersPerDegreeLatitude;
    var lonDelta = radiusMeters / (metersPerDegreeLatitude * Math.Max(Math.Cos(center.Y * Math.PI / 180), 0.01));

    return (center.Y - latDelta, center.Y + latDelta, center.X - lonDelta, center.X + lonDelta);
  }
}
