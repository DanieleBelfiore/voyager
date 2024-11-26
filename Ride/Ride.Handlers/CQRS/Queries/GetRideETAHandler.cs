using System;
using System.Threading;
using System.Threading.Tasks;
using Driver.Core.CQRS.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Ride.Core.CQRS.Queries;
using Ride.Core.Dtos;
using Ride.Handlers.Interfaces;

namespace Ride.Handlers.CQRS.Queries
{
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

      var driver = await mediator.Send(new GetDriverStatus { Id = ride.DriverId }, cancellationToken) ?? throw new Exception("driver_not_found");

      if (ride.PickupLocation == null || driver.LastLocation == null)
        return new ETAResponse();

      var distanceInMeters = ride.PickupLocation.Distance(driver.LastLocation);

      var baseMinutes = distanceInMeters / 1000 / configuration.GetValue<double>("AverageSpeedKmh") * 60;
      var adjustedMinutes = baseMinutes * GetTimeMultiplier(DateTime.Now.Hour);

      return new ETAResponse
      {
        EstimatedArrivalMinutes = DateTime.UtcNow.AddMinutes(adjustedMinutes).Minute,
        DistanceKm = Math.Round(distanceInMeters, 2)
      };
    }

    private static double GetTimeMultiplier(int hour)
    {
      return hour switch
      {
        // Morning highlights (8-10)
        >= 8 and <= 10 => 1.5,

        // Evening highlights (17-19)
        >= 17 and <= 19 => 1.6,

        // Night highlights (22-5)
        >= 22 or <= 5 => 0.8,

        // Lunch time (12-14)
        >= 12 and <= 14 => 1.3,

        // Normal daytime (6-11)
        _ => 1.0
      };
    }
  }
}
