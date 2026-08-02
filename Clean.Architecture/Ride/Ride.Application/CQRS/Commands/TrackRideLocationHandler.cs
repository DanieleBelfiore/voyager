using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Ride.Application.Ports;
using SharedTrackRideLocation = Voyager.Contracts.Ride.TrackRideLocation;

namespace Ride.Application.CQRS.Commands;

/// <summary>
/// Handles the shared Voyager.Contracts.Ride.TrackRideLocation contract directly — the remote
/// target Hub dispatches to on every driver position report, so the ride's own LastLocation
/// tracks the trip instead of standing still at the pickup point between Start and Complete.
/// </summary>
public class TrackRideLocationHandler(IRideRepository repository) : IRequestHandler<SharedTrackRideLocation>
{
  public async Task Handle(SharedTrackRideLocation request, CancellationToken cancellationToken)
  {
    // A position for a ride that vanished (or finished) is not an error worth failing the
    // driver's SignalR call over — the next report supersedes it.
    var ride = await repository.GetByIdAsync(request.RideId, cancellationToken);
    if (ride == null)
      return;

    ride.TrackLocation(request.Location);

    await repository.SaveChangesAsync(cancellationToken);
  }
}
