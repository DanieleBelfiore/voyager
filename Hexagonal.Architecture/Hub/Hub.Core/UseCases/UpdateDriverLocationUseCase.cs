using System.Threading;
using System.Threading.Tasks;
using Hub.Core.Ports.Secondary;
using Hub.Core.Ports.Primary;

namespace Hub.Core.UseCases;

/// <summary>
/// Update the driver's location, find their active ride, and push a location + ETA update to
/// the rider — plus an arrival notification once within 500m of pickup.
/// </summary>
public class UpdateDriverLocationUseCase(
  IDriverLocationUpdater locationUpdater,
  IActiveRideQuery activeRideQuery,
  IRideEtaQuery etaQuery,
  IHubRelay relay) : IUpdateDriverLocationUseCase
{
  private const double ArrivalThresholdMeters = 500;

  public async Task Handle(UpdateDriverLocation request, CancellationToken cancellationToken)
  {
    await locationUpdater.UpdateLocationAsync(request.DriverId, request.Location, cancellationToken);

    var ride = await activeRideQuery.GetActiveRideForDriverAsync(request.DriverId, cancellationToken);
    if (ride == null)
      return;

    await relay.SendToRiderNewDriverLocation(ride.Id, request.Location, cancellationToken);

    var eta = await etaQuery.GetEtaAsync(ride.Id, cancellationToken);

    await relay.SendToRiderNewETA(ride.Id, eta.EstimatedArrivalMinutes, eta.DistanceKm, cancellationToken);

    var distance = ride.PickupLocation.Distance(request.Location);
    if (distance < ArrivalThresholdMeters)
      await relay.SendToRiderDriverArrival(ride.Id, cancellationToken);
  }
}
