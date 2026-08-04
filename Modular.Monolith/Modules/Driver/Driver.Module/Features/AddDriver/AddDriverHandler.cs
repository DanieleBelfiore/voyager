using System.Threading;
using System.Threading.Tasks;
using Driver.Module.Persistence;
using Hikyaku;
using Microsoft.EntityFrameworkCore;
using Voyager.Contracts.Driver;
using DriverEntity = Driver.Module.Entities.Driver;

namespace Driver.Module.Features.AddDriver;

/// <summary>
/// Handles the shared Voyager.Contracts.Driver.AddDriver contract directly. In every other
/// variant this is also the seam Kaido routes a remote call through; here there's no remote
/// — Identity's module just calls IHikyaku.Send(new AddDriver{...}) in the same process, and
/// this internal handler (registered by AddDriverModule) is the one Hikyaku resolves.
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
