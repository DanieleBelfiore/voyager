using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Driver.Application.CQRS.Commands;
using Driver.Application.CQRS.Queries;
using Driver.Application.Dtos;
using Hikyaku;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Voyager.Contracts.Driver;
using Voyager.Shared.Extensions;

namespace Driver.Api.Controllers;

/// <summary>
/// Driver-bounded-context endpoints only. Unlike the Plugin.Microservices.CQRS variant, this
/// controller does not also expose Ride's GetActiveRide/GetRideDriverHistory as a passthrough —
/// those already have a proper home on Ride's own controller, and duplicating them here would
/// leak a foreign bounded context into what's supposed to be a clean layering example.
/// </summary>
[Authorize]
[EnableRateLimiting("driver_api")]
[Route("api/v1/drivers")]
public class DriversController(IHikyaku mediator) : ControllerBase
{
  // Clamped the same way RideController clamps `take`: DistanceThresholdInKm is caller-supplied
  // and drives a spatial scan, so an absurd radius is capped rather than trusted. MaxCandidates
  // bounds the result set on top of this.
  private const int MaxSearchRadiusKm = 50;

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

  [EnableRateLimiting("driver_status_update")]
  [HttpPut("availability")]
  public async Task<ActionResult> UpdateAvailability([FromBody] UpdateAvailabilityRequest request)
  {
    await mediator.Send(new UpdateAvailability { Id = this.GetUserId(), Status = request.Status });

    return Ok();
  }

  [EnableRateLimiting("driver_location_update")]
  [HttpPut("location")]
  public async Task<ActionResult> UpdateLocation([FromBody] UpdateLocationRequest request)
  {
    await mediator.Send(new UpdateLocation { Id = this.GetUserId(), Location = request.Location });

    return Ok();
  }

  [EnableRateLimiting("driver_status")]
  [HttpGet("{driverId:guid}")]
  public async Task<ActionResult<DriverStatusResponse>> GetDriverStatus(Guid driverId)
  {
    return Ok(await mediator.Send(new GetDriverStatus { Id = driverId }));
  }

  [EnableRateLimiting("driver_search")]
  [HttpPost("search")]
  public async Task<ActionResult<List<SearchBestDriverResponse>>> SearchBestDriver([FromBody] SearchBestDriverRequest request)
  {
    // The request is in kilometres, the query is in metres: SearchBestDriverHandler compares
    // against a geography Distance(), which SQL Server evaluates as STDistance in metres.
    return Ok(await mediator.Send(new SearchBestDriver { UserId = this.GetUserId(), Location = request.Location, DistanceThresholdInMeters = Math.Clamp(request.DistanceThresholdInKm, 1, MaxSearchRadiusKm) * 1000 }));
  }
}
