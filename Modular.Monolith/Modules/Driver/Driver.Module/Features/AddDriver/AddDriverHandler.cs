using System.Threading;
using System.Threading.Tasks;
using Driver.Module.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Voyager.Contracts.Driver;
using DriverEntity = Driver.Module.Entities.Driver;

namespace Driver.Module.Features.AddDriver;

/// <summary>
/// Handles the shared Voyager.Contracts.Driver.AddDriver contract directly. In every other
/// variant this is also the seam Arbitrer routes a remote call through; here there's no remote
/// — Identity's module just calls IMediator.Send(new AddDriver{...}) in the same process, and
/// this internal handler (registered by AddDriverModule) is the one MediatR resolves.
/// </summary>
internal class AddDriverHandler(DriverDbContext db) : IRequestHandler<Voyager.Contracts.Driver.AddDriver>
{
  public async Task Handle(Voyager.Contracts.Driver.AddDriver request, CancellationToken cancellationToken)
  {
    var exists = await db.Drivers.AnyAsync(d => d.Id == request.DriverId, cancellationToken);
    if (exists)
      return;

    db.Drivers.Add(new DriverEntity(request.DriverId));

    await db.SaveChangesAsync(cancellationToken);
  }
}
