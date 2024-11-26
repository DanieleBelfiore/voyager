using System.Threading;
using System.Threading.Tasks;
using Driver.Handlers.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Driver.Core.CQRS.Commands
{
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
}
