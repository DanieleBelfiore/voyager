using System.Threading.Tasks;
using Identity.Handlers.Models;
using Microsoft.AspNetCore.Identity;

namespace Identity.Handlers.Interfaces
{
  public interface IUserManager
  {
    Task<IdentityResult> CreateAsync(VoyagerUser user, string password);
    Task<VoyagerUser> FindByIdAsync(string userId);
    Task<VoyagerUser> FindByUsernameAsync(string username);
    Task<bool> CheckPasswordAsync(VoyagerUser user, string password);
    Task<bool> IsInRoleAsync(VoyagerUser user, string role);
    Task<IdentityResult> AddToRoleAsync(VoyagerUser user, string role);
    Task<IdentityResult> RemoveFromRoleAsync(VoyagerUser user, string role);
  }
}
