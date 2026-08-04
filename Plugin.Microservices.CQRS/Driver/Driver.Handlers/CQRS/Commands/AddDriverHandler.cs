using System.Threading;
using System.Threading.Tasks;
using Driver.Core.CQRS.Commands;
using Driver.Handlers.Interfaces;
using Hikyaku;
using Microsoft.EntityFrameworkCore;

namespace Driver.Handlers.CQRS.Commands;

public class AddDriverHandler(IDriverContext db) : IRequestHandler<AddDriver>
{
  public async Task Handle(AddDriver request, CancellationToken cancellationToken)
  {
    var driver = await db.Drivers.FirstOrDefaultAsync(f => f.Id == request.DriverId, cancellationToken);
    if (driver != null)
      return;

    db.Drivers.Add(new Handlers.Models.Driver { Id = request.DriverId });

    await db.SaveChangesAsync(cancellationToken);
  }
}
