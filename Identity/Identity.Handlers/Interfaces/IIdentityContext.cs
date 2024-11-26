using System.Threading;
using System.Threading.Tasks;
using Identity.Handlers.Models;
using Microsoft.EntityFrameworkCore;

namespace Identity.Handlers.Interfaces
{
  public interface IIdentityContext
  {
    DbSet<VoyagerUser> Users { get; set; }
    void Add<TEntity>(TEntity entity) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
  }
}
