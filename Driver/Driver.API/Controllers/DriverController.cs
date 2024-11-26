using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Core;
using Driver.Core.CQRS.Commands;
using Driver.Core.CQRS.Queries;
using Driver.Core.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Ride.Core.CQRS.Queries;
using Ride.Core.Dtos;

namespace Driver.API
{
  /// <summary>
  /// Controller for managing driver-related operations.
  /// </summary>
  [Authorize]
  [EnableRateLimiting("driver_api")]
  [Route("api/v1/drivers")]
  public class DriverController(IMediator mediator) : ControllerBase
  {
    /// <summary>
    /// Registers a new driver.
    /// </summary>
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
      return Ok(await mediator.Send(new SearchBestDriver { UserId = this.GetUserId(), Location = request.Location, DistanceThresholdInMeters = request.DistanceThresholdInKm }));
    }

    /// <summary>
    /// Gets the active ride of the current driver.
    /// </summary>
    [HttpGet("rides/active")]
    public async Task<ActionResult<ActiveRideResponse>> GetActiveRide()
    {
      return Ok(await mediator.Send(new GetActiveRide { DriverId = this.GetUserId() }));
    }

    /// <summary>
    /// Gets the ride history of the current driver.
    /// </summary>
    /// <param name="take">The number of records to take.</param>
    /// <param name="page">The page number.</param>
    [HttpGet("rides/history")]
    public async Task<ActionResult<List<RideDetailsResponse>>> GetRideDriverHistory(int take = 25, int page = 0)
    {
      return Ok(await mediator.Send(new GetRideDriverHistory { DriverId = this.GetUserId(), Take = take, Page = page }));
    }
  }
}
