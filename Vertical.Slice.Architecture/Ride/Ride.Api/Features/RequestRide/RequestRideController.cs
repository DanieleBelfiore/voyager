using System.Threading;
using System.Threading.Tasks;
using Hikyaku;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Ride.Api.Shared;
using Voyager.Shared.Extensions;

namespace Ride.Api.Features.RequestRide;

[Authorize]
[EnableRateLimiting("ride_api")]
[Route("api/v1/rides")]
public class RequestRideController(IHikyaku mediator) : ControllerBase
{
  [EnableRateLimiting("ride_request")]
  [HttpPost]
  public async Task<ActionResult<RideDetailsResponse>> RequestRide([FromBody] RequestRideRequest request, CancellationToken cancellationToken)
  {
    return Ok(await mediator.Send(new RequestRide { UserId = this.GetUserId(), DriverId = request.DriverId, PickupLocation = request.PickupLocation, DropoffLocation = request.DropoffLocation }, cancellationToken));
  }
}
