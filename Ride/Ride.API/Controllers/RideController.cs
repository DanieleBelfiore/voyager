using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Core;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Ride.Core.CQRS.Commands;
using Ride.Core.CQRS.Queries;
using Ride.Core.Dtos;

namespace Ride.API
{
  /// <summary>
  /// Handles ride-related operations.
  /// </summary>
  [Authorize]
  [EnableRateLimiting("ride_api")]
  [Route("api/v1/rides")]
  public class RidesController(IMediator mediator) : ControllerBase
  {
    /// <summary>
    /// Requests a ride.
    /// </summary>
    /// <param name="request">The ride request details.</param>
    /// <returns>The details of the requested ride.</returns>
    [EnableRateLimiting("ride_request")]
    [HttpPost]
    public async Task<ActionResult<RideDetailsResponse>> RequestRide([FromBody] RideRequest request)
    {
      return Ok(await mediator.Send(new RequestRide { UserId = this.GetUserId(), DriverId = request.DriverId, DropoffLocation = request.DropoffLocation, PickupLocation = request.PickupLocation }));
    }

    /// <summary>
    /// Gets the details of a specific ride.
    /// </summary>
    /// <param name="rideId">The ID of the ride.</param>
    /// <returns>The details of the ride.</returns>
    [HttpGet("{rideId:guid}")]
    public async Task<ActionResult<RideDetailsResponse>> GetRideDetails(Guid rideId)
    {
      return Ok(await mediator.Send(new GetRideDetails { Id = rideId }));
    }

    /// <summary>
    /// Cancels a ride.
    /// </summary>
    /// <param name="rideId">The ID of the ride to cancel.</param>
    /// <param name="request">The cancellation request details.</param>
    /// <returns>An HTTP status indicating the result.</returns>
    [EnableRateLimiting("ride_cancellation")]
    [HttpPut("{rideId:guid}/cancel")]
    public async Task<ActionResult> CancelRide(Guid rideId, [FromBody] CancelRideRequest request)
    {
      await mediator.Send(new CancelRide { Id = rideId, CancellationReason = request.CancellationReason });

      return Ok();
    }

    /// <summary>
    /// Gets the current location of a specific ride.
    /// </summary>
    /// <param name="rideId">The ID of the ride.</param>
    /// <returns>The current location of the ride.</returns>
    [EnableRateLimiting("ride_location")]
    [HttpGet("{rideId:guid}/location")]
    public async Task<ActionResult<RideCurrentLocationResponse>> GetRideCurrentLocation(Guid rideId)
    {
      return Ok(await mediator.Send(new GetRideCurrentLocation { Id = rideId }));
    }

    /// <summary>
    /// Gets the estimated time of arrival (ETA) for a specific ride.
    /// </summary>
    /// <param name="rideId">The ID of the ride.</param>
    /// <returns>The ETA of the ride.</returns>
    [HttpGet("{rideId:guid}/eta")]
    public async Task<ActionResult<ETAResponse>> GetRideETA(Guid rideId)
    {
      return Ok(await mediator.Send(new GetRideETA { Id = rideId }));
    }

    /// <summary>
    /// Rates a ride.
    /// </summary>
    /// <param name="rideId">The ID of the ride to rate.</param>
    /// <param name="request">The rating request details.</param>
    /// <returns>An HTTP status indicating the result.</returns>
    [HttpPut("{rideId:guid}/rate")]
    public async Task<ActionResult> RateRide(Guid rideId, [FromBody] RateRideRequest request)
    {
      await mediator.Send(new RateRide { RideId = rideId, Rating = request.Rating });

      return Ok();
    }

    /// <summary>
    /// Rates a specific driver.
    /// </summary>
    /// <param name="rideId">The ID of the ride to rate.</param>
    /// <param name="request">The request containing the rating.</param>
    [HttpPut("{rideId:guid}/rate/driver")]
    public async Task<ActionResult> RateDriver(Guid rideId, [FromBody] RateRideRequest request)
    {
      await mediator.Send(new RateDriver { RideId = rideId, Rating = request.Rating });

      return Ok();
    }

    /// <summary>
    /// Gets the ride history for the current user.
    /// </summary>
    /// <param name="take">The number of records to take.</param>
    /// <param name="page">The page number to retrieve.</param>
    /// <returns>A list of ride details.</returns>
    [HttpGet("history")]
    public async Task<ActionResult<List<RideDetailsResponse>>> GetRideHistory(int take = 25, int page = 0)
    {
      return Ok(await mediator.Send(new GetRideHistory { UserId = this.GetUserId(), Take = take, Page = page }));
    }

    /// <summary>
    /// Gets the current active ride for the user.
    /// </summary>
    /// <returns>The details of the active ride.</returns>
    [HttpGet("active")]
    public async Task<ActionResult<ActiveRideResponse>> GetActiveRide()
    {
      return Ok(await mediator.Send(new GetActiveRide { UserId = this.GetUserId() }));
    }

    /// <summary>
    /// Accepts a specific ride.
    /// </summary>
    /// <param name="rideId">The ID of the ride.</param>
    [HttpPut("{rideId:guid}/accept")]
    public async Task<ActionResult> AcceptRide(Guid rideId)
    {
      await mediator.Send(new AcceptRide { DriverId = this.GetUserId(), RideId = rideId });

      return Ok();
    }

    /// <summary>
    /// Starts a specific ride.
    /// </summary>
    /// <param name="rideId">The ID of the ride.</param>
    /// <param name="request">The request containing the starting location.</param>
    [HttpPut("{rideId:guid}/start")]
    public async Task<ActionResult> StartRide(Guid rideId, [FromBody] StartRideRequest request)
    {
      await mediator.Send(new StartRide { Id = rideId, Location = request.Location });

      return Ok();
    }

    /// <summary>
    /// Completes a specific ride.
    /// </summary>
    /// <param name="rideId">The ID of the ride.</param>
    /// <param name="request">The request containing the completion details.</param>
    [HttpPut("{rideId:guid}/complete")]
    public async Task<ActionResult> CompleteRide(Guid rideId, [FromBody] CompleteRideRequest request)
    {
      await mediator.Send(new CompleteRide { Id = rideId, Location = request.Location, Price = request.Price });

      return Ok();
    }
  }
}
