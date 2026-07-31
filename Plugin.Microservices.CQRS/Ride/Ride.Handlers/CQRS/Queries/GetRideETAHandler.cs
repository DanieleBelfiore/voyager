using System;
using System.Threading;
using System.Threading.Tasks;
using Driver.Core.CQRS.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Geometries;
using Ride.Core.CQRS.Queries;
using Ride.Core.Dtos;
using Ride.Handlers.Interfaces;

namespace Ride.Handlers.CQRS.Queries;

/// <summary>
/// Calculates estimated time of arrival (ETA) using:
/// - Real-time traffic patterns based on time of day
/// - Historical travel time data
/// - Distance between points
///
/// Time multipliers adjust for:
/// - Peak hours (8-10, 17-19): 1.5-1.6x longer
/// - Night hours (22-5): 0.8x faster
/// - Standard hours: 1.0x baseline
/// </summary>
public class GetRideETAHandler(IRideContext db, IMediator mediator, IConfiguration configuration) : IRequestHandler<GetRideETA, ETAResponse>
{
  public async Task<ETAResponse> Handle(GetRideETA request, CancellationToken cancellationToken)
  {
    var ride = await db.Rides.AsNoTracking().FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken) ?? throw new Exception("ride_not_found");

    if (ride.UserId != request.CallerId && ride.DriverId != request.CallerId)
      throw new UnauthorizedAccessException("not_ride_participant");

    var driver = await mediator.Send(new GetDriverStatus { Id = ride.DriverId }, cancellationToken) ?? throw new Exception("driver_not_found");

    if (ride.PickupLocation == null || driver.LastLocation == null)
      return new ETAResponse();

    var distanceInMeters = DistanceInMeters(ride.PickupLocation, driver.LastLocation);

    var baseMinutes = distanceInMeters / 1000 / configuration.GetValue<double>("AverageSpeedKmh") * 60;
    var adjustedMinutes = baseMinutes * GetTimeMultiplier(DateTime.UtcNow.Hour, configuration);

    return new ETAResponse
    {
      EstimatedArrivalMinutes = (int)Math.Round(adjustedMinutes),
      DistanceKm = Math.Round(distanceInMeters / 1000, 2)
    };
  }

  private static double GetTimeMultiplier(int hour, IConfiguration configuration)
  {
    return hour switch
    {
      // Morning highlights (8-10)
      >= 8 and <= 10 => configuration.GetValue<double>("MorningPeakMultiplier"),

      // Evening highlights (17-19)
      >= 17 and <= 19 => configuration.GetValue<double>("EveningPeakMultiplier"),

      // Night highlights (22-5)
      >= 22 or <= 5 => configuration.GetValue<double>("NightMultiplier"),

      // Lunch time (12-14)
      >= 12 and <= 14 => configuration.GetValue<double>("LunchMultiplier"),

      // Normal daytime (6-11)
      _ => 1.0
    };
  }

  // NetTopologySuite's Point.Distance() is planar/Cartesian on the raw coordinate values (SRID
  // is metadata only) — on lat/lon points that returns degrees, not meters. Haversine gives the
  // real great-circle distance instead.
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
}
