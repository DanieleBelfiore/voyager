using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ride.Application.CQRS.Commands;
using Ride.Application.CQRS.Queries;
using Ride.Application.Dtos;
using Hikyaku;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Voyager.Shared.Extensions;

namespace Ride.Api.Controllers;

[Authorize]
[EnableRateLimiting("ride_api")]
[Route("api/v1/rides")]
public class RidesController(IHikyaku mediator) : ControllerBase
{
  [EnableRateLimiting("ride_request")]
  [HttpPost]
  public async Task<ActionResult<RideDetailsResponse>> RequestRide([FromBody] RideRequest request)
  {
    return Ok(await mediator.Send(new RequestRide { UserId = this.GetUserId(), DriverId = request.DriverId, DropoffLocation = request.DropoffLocation, PickupLocation = request.PickupLocation }));
  }

  [HttpGet("{rideId:guid}")]
  public async Task<ActionResult<RideDetailsResponse>> GetRideDetails(Guid rideId)
  {
    return Ok(await mediator.Send(new GetRideDetails { Id = rideId, CallerId = this.GetUserId() }));
  }

  [EnableRateLimiting("ride_cancellation")]
  [HttpPut("{rideId:guid}/cancel")]
  public async Task<ActionResult> CancelRide(Guid rideId, [FromBody] CancelRideRequest request)
  {
    await mediator.Send(new CancelRide { Id = rideId, CancellationReason = request.CancellationReason, CallerId = this.GetUserId() });

    return Ok();
  }

  [EnableRateLimiting("ride_location")]
  [HttpGet("{rideId:guid}/location")]
  public async Task<ActionResult<RideCurrentLocationResponse>> GetRideCurrentLocation(Guid rideId)
  {
    return Ok(await mediator.Send(new GetRideCurrentLocation { Id = rideId, CallerId = this.GetUserId() }));
  }

  [HttpGet("{rideId:guid}/eta")]
  public async Task<ActionResult<ETAResponse>> GetRideETA(Guid rideId)
  {
    return Ok(await mediator.Send(new GetRideETA { Id = rideId, CallerId = this.GetUserId() }));
  }

  [HttpPut("{rideId:guid}/rate")]
  public async Task<ActionResult> RateRide(Guid rideId, [FromBody] RateRideRequest request)
  {
    await mediator.Send(new RateRide { RideId = rideId, Rating = request.Rating, CallerId = this.GetUserId() });

    return Ok();
  }

  [HttpPut("{rideId:guid}/rate/driver")]
  public async Task<ActionResult> RateDriver(Guid rideId, [FromBody] RateRideRequest request)
  {
    await mediator.Send(new RateDriver { RideId = rideId, Rating = request.Rating, CallerId = this.GetUserId() });

    return Ok();
  }

  [HttpGet("history")]
  public async Task<ActionResult<List<RideDetailsResponse>>> GetRideHistory(int take = 25, int page = 0)
  {
    take = Math.Min(take, 100);

    return Ok(await mediator.Send(new GetRideHistory { UserId = this.GetUserId(), Take = take, Page = page }));
  }

  [HttpGet("active")]
  public async Task<ActionResult<ActiveRideResponse>> GetActiveRide()
  {
    return Ok(await mediator.Send(new GetActiveRide { UserId = this.GetUserId() }));
  }

  [Authorize(Policy = "RequireDriver")]
  [HttpPut("{rideId:guid}/accept")]
  public async Task<ActionResult> AcceptRide(Guid rideId)
  {
    await mediator.Send(new AcceptRide { DriverId = this.GetUserId(), RideId = rideId });

    return Ok();
  }

  [HttpPut("{rideId:guid}/start")]
  public async Task<ActionResult> StartRide(Guid rideId, [FromBody] StartRideRequest request)
  {
    await mediator.Send(new StartRide { Id = rideId, Location = request.Location, CallerId = this.GetUserId() });

    return Ok();
  }

  [HttpPut("{rideId:guid}/complete")]
  public async Task<ActionResult> CompleteRide(Guid rideId, [FromBody] CompleteRideRequest request)
  {
    await mediator.Send(new CompleteRide { Id = rideId, Location = request.Location, CallerId = this.GetUserId() });

    return Ok();
  }
}
