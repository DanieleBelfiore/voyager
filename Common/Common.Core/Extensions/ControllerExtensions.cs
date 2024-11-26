using System;
using Microsoft.AspNetCore.Mvc;

namespace Common.Core
{
  public static class ControllerExtensions
  {
    public static Guid GetUserId(this ControllerBase controller)
    {
      var userId = controller.User.FindFirst("sub")?.Value;

      return userId == null ? throw new ArgumentException("missing_user_id") : Guid.Parse(userId);
    }
  }
}
