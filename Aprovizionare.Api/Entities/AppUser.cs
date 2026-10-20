using Microsoft.AspNetCore.Identity;

namespace Aprovizionare.Api.Entities;

public class AppUser :IdentityUser
{
    public string FullName { get; set; } = "";
}