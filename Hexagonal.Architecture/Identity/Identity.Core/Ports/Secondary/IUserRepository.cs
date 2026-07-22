using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Identity.Core.Domain;

namespace Identity.Core.Ports.Secondary;

public interface IUserRepository
{
  Task<User> GetByEmailAsync(string email, CancellationToken cancellationToken);
  Task<User> GetByIdAsync(Guid id, CancellationToken cancellationToken);
  Task<Dictionary<Guid, double>> GetRatingsAsync(List<Guid> userIds, CancellationToken cancellationToken);
  void Add(User user);
  Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
