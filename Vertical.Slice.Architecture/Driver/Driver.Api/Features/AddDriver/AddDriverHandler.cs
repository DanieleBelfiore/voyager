using System.Threading;
using System.Threading.Tasks;
using Driver.Api.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Voyager.Contracts.Driver;
using DriverEntity = Driver.Api.Entities.Driver;

namespace Driver.Api.Features.AddDriver;

/// <summary>
/// Handles the shared <see cref="Voyager.Contracts.Driver.AddDriver"/> contract directly —
/// same unification pattern as the Clean/Hexagonal variants: one type for both local dispatch
/// (this service's own controller) and Arbitrer's remote dispatch (e.g. from Identity after
/// registration). No repository — talks to DriverDbContext directly, no port to satisfy.
/// </summary>
public class AddDriverHandler(DriverDbContext db) : IRequestHandler<Voyager.Contracts.Driver.AddDriver>
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
