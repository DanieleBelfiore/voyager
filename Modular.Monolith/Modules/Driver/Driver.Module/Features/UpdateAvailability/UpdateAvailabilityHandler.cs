using System;
using System.Threading;
using System.Threading.Tasks;
using Driver.Module.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Driver.Module.Features.UpdateAvailability;

internal class UpdateAvailabilityHandler(DriverDbContext db) : IRequestHandler<UpdateAvailability>
{
  public async Task Handle(UpdateAvailability request, CancellationToken cancellationToken)
  {
    var driver = await db.Drivers.FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
      ?? throw new Exception("driver_not_found");

    driver.UpdateAvailability(request.Status);

    await db.SaveChangesAsync(cancellationToken);
  }
}
