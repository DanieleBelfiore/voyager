using System;
using System.Linq;

namespace Common.Core.Extensions;

public static class LINQExtensions
{
  private const int MaxTake = 100;

  public static IQueryable<TResult> TakeIfPositive<TResult>(this IQueryable<TResult> source, int count)
  {
    return count < 0 ? source : source.Take(Math.Min(count, MaxTake));
  }
}
