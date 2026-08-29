using BionicSquare.Models;

namespace BionicSquare.Business.Services;

public interface IApplicationUserService
{
    Task<ApplicationUser> GetUserByIdAsync(string userId);
}