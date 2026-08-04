using System;
using System.Collections.Generic;
using Hikyaku;

namespace Identity.Core.CQRS.Queries
{
  public class GetUsersRatings : IRequest<Dictionary<Guid, double>>
  {
    public List<Guid> UserIds { get; set; }
  }
}
