using System.Threading.Tasks;
using Identity.Handlers.Interfaces;
using Identity.Handlers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.Handlers.Services
{
  public class UserManagerService : IUserManager
  {
    private readonly UserManager<VoyagerUser> _userManager;

    public UserManagerService(UserManager<VoyagerUser> userManager)
    {
      _userManager = userManager;
    }

    public async Task<IdentityResult> CreateAsync(VoyagerUser user, string password)
    {
      return await _userManager.CreateAsync(user, password);
    }

    public async Task<VoyagerUser> FindByIdAsync(string userId)
    {
      return await _userManager.FindByIdAsync(userId);
    }

    public async Task<VoyagerUser> FindByUsernameAsync(string username)
    {
      return await _userManager.Users.SingleOrDefaultAsync(u => u.UserName == username);
    }

    public async Task<bool> CheckPasswordAsync(VoyagerUser user, string password)
    {
      return await _userManager.CheckPasswordAsync(user, password);
    }

    public async Task<bool> IsInRoleAsync(VoyagerUser user, string role)
    {
      return await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<IdentityResult> AddToRoleAsync(VoyagerUser user, string role)
    {
      return await _userManager.AddToRoleAsync(user, role);
    }

    public async Task<IdentityResult> RemoveFromRoleAsync(VoyagerUser user, string role)
    {
      return await _userManager.RemoveFromRoleAsync(user, role);
    }

  }
}
