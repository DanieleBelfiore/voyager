using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Ride.Handlers.Interfaces
{
  public interface IRideContext
  {
    public DbSet<Models.Ride> Rides { get; set; }

    void Add<TEntity>(TEntity entity) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
  }
}
