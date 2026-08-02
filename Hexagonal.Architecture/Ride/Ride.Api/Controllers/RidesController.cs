using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ride.Core.Dtos;
using Ride.Core.Ports.Primary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Voyager.Shared.Extensions;

namespace Ride.Api.Controllers;

/// <summary>Primary adapter over HTTP — injects each use case's primary port directly.</summary>
[Authorize]
[EnableRateLimiting("ride_api")]
[Route("api/v1/rides")]
public class RidesController(
  IRequestRideUseCase requestRide,
  IGetRideDetailsUseCase getRideDetails,
  ICancelRideUseCase cancelRide,
  IGetRideCurrentLocationUseCase getRideCurrentLocation,
  IGetRideETAUseCase getRideEta,
  IRateRideUseCase rateRide,
  IRateDriverUseCase rateDriver,
  IGetRideHistoryUseCase getRideHistory,
  IGetActiveRideUseCase getActiveRide,
  IAcceptRideUseCase acceptRide,
  IStartRideUseCase startRide,
  ICompleteRideUseCase completeRide) : ControllerBase
{
  [EnableRateLimiting("ride_request")]
  [HttpPost]
  public async Task<ActionResult<RideDetailsResponse>> RequestRide([FromBody] RideRequest request, CancellationToken cancellationToken)
  {
    return Ok(await requestRide.Handle(new Ride.Core.Ports.Primary.RequestRide { UserId = this.GetUserId(), DriverId = request.DriverId, DropoffLocation = request.DropoffLocation, PickupLocation = request.PickupLocation }, cancellationToken));
  }

  [HttpGet("{rideId:guid}")]
  public async Task<ActionResult<RideDetailsResponse>> GetRideDetails(Guid rideId, CancellationToken cancellationToken)
  {
    return Ok(await getRideDetails.Handle(new GetRideDetails { Id = rideId, CallerId = this.GetUserId() }, cancellationToken));
  }

  [EnableRateLimiting("ride_cancellation")]
  [HttpPut("{rideId:guid}/cancel")]
  public async Task<ActionResult> CancelRide(Guid rideId, [FromBody] CancelRideRequest request, CancellationToken cancellationToken)
  {
    await cancelRide.Handle(new Ride.Core.Ports.Primary.CancelRide { Id = rideId, CancellationReason = request.CancellationReason, CallerId = this.GetUserId() }, cancellationToken);

    return Ok();
  }

  [EnableRateLimiting("ride_location")]
  [HttpGet("{rideId:guid}/location")]
  public async Task<ActionResult<RideCurrentLocationResponse>> GetRideCurrentLocation(Guid rideId, CancellationToken cancellationToken)
  {
    return Ok(await getRideCurrentLocation.Handle(new GetRideCurrentLocation { Id = rideId, CallerId = this.GetUserId() }, cancellationToken));
  }

  [HttpGet("{rideId:guid}/eta")]
  public async Task<ActionResult<ETAResponse>> GetRideETA(Guid rideId, CancellationToken cancellationToken)
  {
    return Ok(await getRideEta.Handle(new GetRideETA { Id = rideId, CallerId = this.GetUserId() }, cancellationToken));
  }

  [HttpPut("{rideId:guid}/rate")]
  public async Task<ActionResult> RateRide(Guid rideId, [FromBody] RateRideRequest request, CancellationToken cancellationToken)
  {
    await rateRide.Handle(new Ride.Core.Ports.Primary.RateRide { RideId = rideId, Rating = request.Rating, CallerId = this.GetUserId() }, cancellationToken);

    return Ok();
  }

  [HttpPut("{rideId:guid}/rate/driver")]
  public async Task<ActionResult> RateDriver(Guid rideId, [FromBody] RateRideRequest request, CancellationToken cancellationToken)
  {
    await rateDriver.Handle(new Ride.Core.Ports.Primary.RateDriver { RideId = rideId, Rating = request.Rating, CallerId = this.GetUserId() }, cancellationToken);

    return Ok();
  }

  [HttpGet("history")]
  public async Task<ActionResult<List<RideDetailsResponse>>> GetRideHistory(CancellationToken cancellationToken, int take = 25, int page = 0)
  {
    take = Math.Min(take, 100);

    return Ok(await getRideHistory.Handle(new GetRideHistory { UserId = this.GetUserId(), Take = take, Page = page }, cancellationToken));
  }

  [HttpGet("active")]
  public async Task<ActionResult<ActiveRideResponse>> GetActiveRide(CancellationToken cancellationToken)
  {
    return Ok(await getActiveRide.Handle(new GetActiveRide { UserId = this.GetUserId() }, cancellationToken));
  }

  [Authorize(Policy = "RequireDriver")]
  [HttpPut("{rideId:guid}/accept")]
  public async Task<ActionResult> AcceptRide(Guid rideId, CancellationToken cancellationToken)
  {
    await acceptRide.Handle(new AcceptRide { DriverId = this.GetUserId(), RideId = rideId }, cancellationToken);

    return Ok();
  }

  [HttpPut("{rideId:guid}/start")]
  public async Task<ActionResult> StartRide(Guid rideId, [FromBody] StartRideRequest request, CancellationToken cancellationToken)
  {
    await startRide.Handle(new Ride.Core.Ports.Primary.StartRide { Id = rideId, Location = request.Location, CallerId = this.GetUserId() }, cancellationToken);

    return Ok();
  }

  [HttpPut("{rideId:guid}/complete")]
  public async Task<ActionResult> CompleteRide(Guid rideId, [FromBody] CompleteRideRequest request, CancellationToken cancellationToken)
  {
    await completeRide.Handle(new Ride.Core.Ports.Primary.CompleteRide { Id = rideId, Location = request.Location, CallerId = this.GetUserId() }, cancellationToken);

    return Ok();
  }
}
