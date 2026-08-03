using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Ride.Core.Ports.Secondary;
using SharedTrackRideLocation = Voyager.Contracts.Ride.TrackRideLocation;
using Ride.Core.Validation;

namespace Ride.Core.UseCases;

/// <summary>
/// Handles the shared Voyager.Contracts.Ride.TrackRideLocation contract directly — the remote
/// target Hub dispatches to on every driver position report, so the ride's own LastLocation
/// tracks the trip instead of standing still at the pickup point between Start and Complete.
///
/// No primary port: nothing local injects this, it is reachable only via Arbitrer — same shape
/// as GetActiveRideForHubUseCase.
/// </summary>
public class TrackRideLocationUseCase(IRideRepository repository) : IRequestHandler<SharedTrackRideLocation>
{
  public async Task Handle(SharedTrackRideLocation request, CancellationToken cancellationToken)
  {
    GeoGuard.Required(request.Location, "location");

    // A position for a ride that vanished (or finished) is not an error worth failing the
    // driver's SignalR call over — the next report supersedes it.
    var ride = await repository.GetByIdAsync(request.RideId, cancellationToken);
    if (ride == null)
      return;

    ride.TrackLocation(request.Location);

    await repository.SaveChangesAsync(cancellationToken);
  }
}
