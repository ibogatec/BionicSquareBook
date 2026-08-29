using BionicSquare.DataAccess;
using BionicSquare.Models;

namespace BionicSquare.Business.Services;

public class ApplicationUserService : IApplicationUserService
{
    private readonly ApplicationDbContext _context;
    
    public ApplicationUserService(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<ApplicationUser> GetUserByIdAsync(string userId)
    {
        return await _context.ApplicationUsers.FindAsync(userId) ?? throw new Exception($"User with id '{userId}' not found in database");
    }
}
