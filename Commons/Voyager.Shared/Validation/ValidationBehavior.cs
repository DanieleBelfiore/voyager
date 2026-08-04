using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Hikyaku;

namespace Voyager.Shared.Validation;

/// <summary>
/// Hikyaku pipeline behavior that runs every registered FluentValidation validator for
/// the request before the handler executes. Used by the Vertical Slice Architecture and
/// Modular Monolith variants, where each feature slice owns its own validator instead of a
/// shared validation layer. Plugin/Clean/Hexagonal validate with inline guard clauses instead.
/// </summary>
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
  : IPipelineBehavior<TRequest, TResponse>
  where TRequest : IRequest<TResponse>
{
  public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
  {
    if (validators.Any())
    {
      var context = new ValidationContext<TRequest>(request);
      var failures = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken))))
        .SelectMany(result => result.Errors)
        .Where(failure => failure != null)
        .ToList();

      if (failures.Count != 0)
        throw new ValidationException(failures);
    }

    return await next();
  }
}
