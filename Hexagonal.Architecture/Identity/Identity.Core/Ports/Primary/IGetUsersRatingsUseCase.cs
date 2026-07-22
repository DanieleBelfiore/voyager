using System;
using System.Collections.Generic;
using MediatR;
using Voyager.Contracts.Identity;

namespace Identity.Core.Ports.Primary;

public interface IGetUsersRatingsUseCase : IRequestHandler<GetUsersRatings, Dictionary<Guid, double>>;
