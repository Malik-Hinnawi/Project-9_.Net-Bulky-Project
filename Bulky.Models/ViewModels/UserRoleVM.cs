using System.Collections;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bulky.Models.ViewModels;

public class UserRoleVM
{
    public ApplicationUser? User {
        get;
        set;
    }

    public IEnumerable<SelectListItem>? Companies { get; set; }
    public IEnumerable<SelectListItem>? Roles { get; set; }
}