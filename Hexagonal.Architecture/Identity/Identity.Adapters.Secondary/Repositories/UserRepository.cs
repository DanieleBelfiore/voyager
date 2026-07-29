using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Identity.Adapters.Secondary.Persistence;
using Identity.Core.Ports.Secondary;
using Microsoft.EntityFrameworkCore;
using UserEntity = Identity.Core.Domain.User;

namespace Identity.Adapters.Secondary.Repositories;

public class UserRepository(IdentityDbContext db) : IUserRepository
{
  public async Task<UserEntity> GetByEmailAsync(string email, CancellationToken cancellationToken)
  {
    return await db.Users.FirstOrDefaultAsync(f => f.Email == email, cancellationToken);
  }

  public async Task<UserEntity> GetByIdAsync(Guid id, CancellationToken cancellationToken)
  {
    return await db.Users.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
  }

  public async Task<Dictionary<Guid, double>> GetRatingsAsync(List<Guid> userIds, CancellationToken cancellationToken)
  {
    return await db.Users.AsNoTracking().Where(f => userIds.Contains(f.Id))
      .ToDictionaryAsync(k => k.Id, v => v.Ratings, cancellationToken);
  }

  public void Add(UserEntity user)
  {
    db.Users.Add(user);
  }

  public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
  {
    return await db.SaveChangesAsync(cancellationToken);
  }
}
