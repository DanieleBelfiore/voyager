using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Driver.Core.Dtos;
using Driver.Core.Ports.Primary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Voyager.Contracts.Driver;
using Voyager.Shared.Extensions;

namespace Driver.Api.Controllers;

/// <summary>
/// Primary adapter over HTTP. Unlike the Clean.Architecture variant's controller,
/// this one injects each use case's primary port directly (IAddDriverUseCase, etc.) and calls
/// .Handle(...) — no IHikyaku.Send indirection for local calls. The same use case classes are
/// still reachable remotely via Kaido/Hikyaku (see Driver.Core/Ports/Primary), which is a
/// different primary adapter entirely; this controller doesn't need to know that exists.
/// </summary>
[Authorize]
[EnableRateLimiting("driver_api")]
[Route("api/v1/drivers")]
public class DriversController(
  IAddDriverUseCase addDriver,
  IUpdateAvailabilityUseCase updateAvailability,
  IUpdateLocationUseCase updateLocation,
  IGetDriverStatusUseCase getDriverStatus,
  ISearchBestDriverUseCase searchBestDriver) : ControllerBase
{
  // Clamped the same way RideController clamps `take`: DistanceThresholdInKm is caller-supplied
  // and drives a spatial scan, so an absurd radius is capped rather than trusted. MaxCandidates
  // bounds the result set on top of this.
  private const int MaxSearchRadiusKm = 50;

  // Gated on the is_driver claim, not just authentication: a Driver row is what puts someone into
  // SearchBestDriver's candidate pool, so an unrestricted endpoint let a rider account self-register,
  // publish a location, and be matched to real ride requests it can never accept. The claim marks
  // the account type chosen at registration, not a privilege granted by anyone — registering as a
  // driver is self-service, so this separates the two flows rather than keeping anyone out.
  [Authorize(Policy = "RequireDriver")]
  [EnableRateLimiting("driver_registration")]
  [HttpPost]
  public async Task<ActionResult> AddDriver(CancellationToken cancellationToken)
  {
    await addDriver.Handle(new AddDriver { DriverId = this.GetUserId() }, cancellationToken);

    return Ok();
  }

  [EnableRateLimiting("driver_status_update")]
  [HttpPut("availability")]
  public async Task<ActionResult> UpdateAvailability([FromBody] UpdateAvailabilityRequest request, CancellationToken cancellationToken)
  {
    await updateAvailability.Handle(new UpdateAvailability { Id = this.GetUserId(), Status = request.Status }, cancellationToken);

    return Ok();
  }

  [EnableRateLimiting("driver_location_update")]
  [HttpPut("location")]
  public async Task<ActionResult> UpdateLocation([FromBody] UpdateLocationRequest request, CancellationToken cancellationToken)
  {
    await updateLocation.Handle(new UpdateLocation { Id = this.GetUserId(), Location = request.Location }, cancellationToken);

    return Ok();
  }

  [EnableRateLimiting("driver_status")]
  [HttpGet("{driverId:guid}")]
  public async Task<ActionResult<DriverStatusResponse>> GetDriverStatus(Guid driverId, CancellationToken cancellationToken)
  {
    return Ok(await getDriverStatus.Handle(new GetDriverStatus { Id = driverId, CallerId = this.GetUserId() }, cancellationToken));
  }

  [EnableRateLimiting("driver_search")]
  [HttpPost("search")]
  public async Task<ActionResult<List<SearchBestDriverResponse>>> SearchBestDriver([FromBody] SearchBestDriverRequest request, CancellationToken cancellationToken)
  {
    return Ok(await searchBestDriver.Handle(new SearchBestDriver { UserId = this.GetUserId(), Location = request.Location, DistanceThresholdInMeters = Math.Clamp(request.DistanceThresholdInKm, 1, MaxSearchRadiusKm) * 1000 }, cancellationToken));
  }
}
