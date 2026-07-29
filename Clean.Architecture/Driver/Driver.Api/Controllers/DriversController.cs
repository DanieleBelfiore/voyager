using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Driver.Application.CQRS.Commands;
using Driver.Application.CQRS.Queries;
using Driver.Application.Dtos;
using MediatR;
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
public class DriversController(IMediator mediator) : ControllerBase
{
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
    return Ok(await mediator.Send(new SearchBestDriver { UserId = this.GetUserId(), Location = request.Location, DistanceThresholdInMeters = request.DistanceThresholdInKm }));
  }
}
