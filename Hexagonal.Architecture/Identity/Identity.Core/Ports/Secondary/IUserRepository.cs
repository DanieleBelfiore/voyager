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
  /// <summary>
  /// Undoes an Add that has already been committed. Registering a driver spans two services with
  /// no transaction between them, so this is what lets the caller roll the user back when the
  /// second half fails — see RegisterUserUseCase.
  /// </summary>
  void Remove(User user);
  Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
