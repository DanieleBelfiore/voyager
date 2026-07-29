using System.Linq;

namespace Voyager.Shared.Extensions;

public static class LINQExtensions
{
  public static IQueryable<TResult> TakeIfPositive<TResult>(this IQueryable<TResult> source, int count)
  {
    return count < 0 ? source : source.Take(count);
  }
}
