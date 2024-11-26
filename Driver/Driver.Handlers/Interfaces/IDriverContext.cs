using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Driver.Handlers.Interfaces
{
  public interface IDriverContext
  {
    public DbSet<Models.Driver> Drivers { get; set; }

    void Add<TEntity>(TEntity entity) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
  }
}
