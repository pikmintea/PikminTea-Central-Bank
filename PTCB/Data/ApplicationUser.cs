using Microsoft.AspNetCore.Identity;

namespace PTCB.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public decimal SumSumBalance { get; set; } = 0m;
    
}