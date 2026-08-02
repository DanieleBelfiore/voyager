using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Identity.Domain.Entities;

namespace Identity.Application.Ports;

public interface IUserRepository
{
  Task<User> GetByEmailAsync(string email, CancellationToken cancellationToken);
  Task<User> GetByIdAsync(Guid id, CancellationToken cancellationToken);
  Task<Dictionary<Guid, double>> GetRatingsAsync(List<Guid> userIds, CancellationToken cancellationToken);
  /// <summary>
  /// Folds one new rating into the user's running average as a single atomic statement, and
  /// returns the resulting average (null when no such user exists).
  ///
  /// Not a read-modify-write on a tracked entity: two ratings landing on the same user at the
  /// same time both read the same RatingsCount and the second SaveChanges silently overwrote the
  /// first, so a rating simply vanished from the average that SearchBestDriver ranks on.
  /// </summary>
  Task<double?> ApplyRatingAsync(Guid userId, int rating, CancellationToken cancellationToken);
  void Add(User user);
  Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
