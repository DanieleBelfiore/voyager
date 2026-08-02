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

  /// <summary>
  /// One statement, computed from the row's own current values, so concurrent ratings can't read
  /// the same RatingsCount and lose one of the updates. The follow-up read is only for the
  /// caller's return value — the average itself is already committed.
  /// </summary>
  public async Task<double?> ApplyRatingAsync(Guid userId, int rating, CancellationToken cancellationToken)
  {
    // Captured rather than inlined so it is sent as a parameter, not a provider-specific
    // server-clock function.
    var now = DateTime.UtcNow;

    var affected = await db.Users
      .Where(f => f.Id == userId)
      .ExecuteUpdateAsync(setters => setters
        .SetProperty(f => f.Ratings, f => (f.Ratings * f.RatingsCount + rating) / (f.RatingsCount + 1))
        .SetProperty(f => f.RatingsCount, f => f.RatingsCount + 1)
        .SetProperty(f => f.Modified, _ => now), cancellationToken);

    if (affected == 0)
      return null;

    return await db.Users.AsNoTracking().Where(f => f.Id == userId)
      .Select(f => f.Ratings)
      .FirstOrDefaultAsync(cancellationToken);
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
