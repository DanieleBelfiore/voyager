using System;
using System.Collections.Generic;
using Hikyaku;
using Voyager.Contracts.Identity;

namespace Identity.Core.Ports.Primary;

public interface IGetUsersRatingsUseCase : IRequestHandler<GetUsersRatings, Dictionary<Guid, double>>;
