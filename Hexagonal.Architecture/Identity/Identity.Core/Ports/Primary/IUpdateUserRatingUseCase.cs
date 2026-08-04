using Hikyaku;
using Voyager.Contracts.Identity;

namespace Identity.Core.Ports.Primary;

/// <summary>Primary port — see Driver's IAddDriverUseCase for why this extends IRequestHandler&lt;T&gt;.</summary>
public interface IUpdateUserRatingUseCase : IRequestHandler<UpdateUserRating, double>;
