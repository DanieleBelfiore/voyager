using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Core.Extensions;
using Driver.Core.CQRS.Commands;
using Driver.Core.CQRS.Queries;
using Driver.Core.Dtos;
using Hikyaku;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Driver.API.Controllers;

/// <summary>
/// Controller for managing driver-related operations.
/// </summary>
[Authorize]
[EnableRateLimiting("driver_api")]
[Route("api/v1/drivers")]
public class DriverController(IHikyaku mediator) : ControllerBase
{
  // Clamped the same way RideController clamps `take`: DistanceThresholdInKm is caller-supplied
  // and drives a spatial scan, so an absurd radius is capped rather than trusted. MaxCandidates
  // bounds the result set on top of this.
  private const int MaxSearchRadiusKm = 50;

  /// <summary>
  /// Registers a new driver.
  /// </summary>
  // Gated on the is_driver claim, not just authentication: a Driver row is what puts someone into
  // SearchBestDriver's candidate pool, so an unrestricted endpoint let any rider self-register,
  // publish a location, and be matched to real ride requests they can never accept.
  [Authorize(Policy = "RequireDriver")]
  [EnableRateLimiting("driver_registration")]
  [HttpPost]
  public async Task<ActionResult> AddDriver()
  {
    await mediator.Send(new AddDriver { DriverId = this.GetUserId() });

    return Ok();
  }

  /// <summary>
  /// Updates the driver's availability status.
  /// </summary>
  /// <param name="request">The request containing the new availability status.</param>
  [EnableRateLimiting("driver_status_update")]
  [HttpPut("availability")]
  public async Task<ActionResult> UpdateAvailability([FromBody] UpdateAvailabilityRequest request)
  {
    await mediator.Send(new UpdateAvailability { Id = this.GetUserId(), Status = request.Status });

    return Ok();
  }

  /// <summary>
  /// Updates the driver's location.
  /// </summary>
  /// <param name="request">The request containing the new location.</param>
  [EnableRateLimiting("driver_location_update")]
  [HttpPut("location")]
  public async Task<ActionResult> UpdateLocation([FromBody] UpdateLocationRequest request)
  {
    await mediator.Send(new UpdateLocation { Id = this.GetUserId(), Location = request.Location });

    return Ok();
  }

  /// <summary>
  /// Gets the status of a specific driver.
  /// </summary>
  /// <param name="driverId">The ID of the driver.</param>
  [EnableRateLimiting("driver_status")]
  [HttpGet("{driverId:guid}")]
  public async Task<ActionResult<DriverStatusResponse>> GetDriverStatus(Guid driverId)
  {
    return Ok(await mediator.Send(new GetDriverStatus { Id = driverId }));
  }

  /// <summary>
  /// Searches for the best driver based on the provided criteria.
  /// </summary>
  /// <param name="request">The search criteria.</param>
  [EnableRateLimiting("driver_search")]
  [HttpPost("search")]
  public async Task<ActionResult<List<SearchBestDriverResponse>>> SearchBestDriver([FromBody] SearchBestDriverRequest request)
  {
    return Ok(await mediator.Send(new SearchBestDriver { UserId = this.GetUserId(), Location = request.Location, DistanceThresholdInMeters = Math.Clamp(request.DistanceThresholdInKm, 1, MaxSearchRadiusKm) * 1000 }));
  }

  // GET rides/active and GET rides/history used to live here, serving Ride data (GetActiveRide,
  // GetRideDriverHistory) out of the Driver service. That is the bounded-context leak
  // Commons/README.md rules out: an endpoint whose data another service owns. Removed — the
  // Ride service is where ride reads belong, and every other variant in the portfolio already
  // does not expose them from Driver. GetRideDriverHistory keeps its handler in Ride.Handlers,
  // reachable over Kaido, same as in Clean/Hexagonal/Vertical.Slice/Modular.Monolith.
}
