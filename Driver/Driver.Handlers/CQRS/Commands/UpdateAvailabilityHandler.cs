using System;
using System.Threading;
using System.Threading.Tasks;
using Driver.Core.CQRS.Commands;
using Driver.Handlers.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Driver.Handlers.CQRS.Commands;

public class UpdateAvailabilityHandler(IDriverContext db) : IRequestHandler<UpdateAvailability>
{
  public async Task Handle(UpdateAvailability request, CancellationToken cancellationToken)
  {
    var driver = await db.Drivers.FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken) ?? throw new Exception("driver_not_found");

    driver.Status = request.Status;
    driver.LastUpdateDate = DateTime.UtcNow;

    await db.SaveChangesAsync(cancellationToken);
  }
}
