using System.Threading.Tasks;
using Identity.Handlers.Interfaces;
using Identity.Handlers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.Handlers.Services;

public class UserManagerService(UserManager<VoyagerUser> userManager) : IUserManager
{
  public async Task<IdentityResult> CreateAsync(VoyagerUser user, string password)
  {
    return await userManager.CreateAsync(user, password);
  }

  public async Task<VoyagerUser> FindByIdAsync(string userId)
  {
    return await userManager.FindByIdAsync(userId);
  }

  public async Task<VoyagerUser> FindByUsernameAsync(string username)
  {
    return await userManager.Users.SingleOrDefaultAsync(u => u.UserName == username);
  }

  public async Task<bool> CheckPasswordAsync(VoyagerUser user, string password)
  {
    return await userManager.CheckPasswordAsync(user, password);
  }

  public async Task<bool> IsInRoleAsync(VoyagerUser user, string role)
  {
    return await userManager.IsInRoleAsync(user, role);
  }

  public async Task<IdentityResult> AddToRoleAsync(VoyagerUser user, string role)
  {
    return await userManager.AddToRoleAsync(user, role);
  }

  public async Task<IdentityResult> RemoveFromRoleAsync(VoyagerUser user, string role)
  {
    return await userManager.RemoveFromRoleAsync(user, role);
  }

}
