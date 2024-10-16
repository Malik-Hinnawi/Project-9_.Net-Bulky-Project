using Bulky.DataAccess.Data;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.EntityFrameworkCore;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.RoleAdmin)]
public class UserController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<UserController> _logger;
    
    public UserController(ApplicationDbContext db, UserManager<IdentityUser> userManager, ILogger<UserController> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }
    
    // GET
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult RoleManagement(string id)
    {
        var user = _db.ApplicationUsers.Where(u => u.Id == id).Include(u => u.Company).FirstOrDefault();
        var roleId = _db.UserRoles.FirstOrDefault(u => u.UserId == id)?.RoleId;
        var role = _db.Roles.FirstOrDefault(u => u.Id == roleId)?.Name;
        UserRoleVM roleVm = new()
        {
            User = user,
            Roles = _db.Roles.Select(i=> new SelectListItem()
            {
                Text = i.Name,
                Value = i.Name
            }),
            Companies = _db.Companies.Select(i=>new SelectListItem()
            {
                Text = i.Name,
                Value = i.Id.ToString()
            })
        };

        roleVm.User.Role = role;
        return View(roleVm);
    }
    
    [HttpPost]
    public IActionResult UpdateRole(UserRoleVM roleVm)
    {
        _logger.Log(LogLevel.Information,"Reached");
        var roleId = _db.UserRoles.FirstOrDefault(u=>u.UserId == roleVm.User.Id).RoleId;
        var oldRole = _db.Roles.FirstOrDefault(u => u.Id == roleId).Name;

        if (!(roleVm.User.Role == oldRole))
        {
            var user = _db.ApplicationUsers.FirstOrDefault(u => u.Id == roleVm.User.Id);
            if (roleVm.User.Role == SD.RoleCompany)
            {
                user.CompanyId = roleVm.User.CompanyId;
            }

            if (oldRole == SD.RoleCompany)
            {
                user.CompanyId = null;
            }

            _db.SaveChanges();
            _userManager.RemoveFromRoleAsync(user, oldRole).GetAwaiter().GetResult();
            _userManager.AddToRoleAsync(user, roleVm.User.Role).GetAwaiter().GetResult();
        }
        
        return RedirectToAction(nameof(Index));
    }

    #region API_CALLS
    
    [HttpGet]
    public IActionResult RoleManagementTest(string id)
    {
        var user = _db.ApplicationUsers.Where(u => u.Id == id).Include(u => u.Company).FirstOrDefault();
        var roleId = _db.UserRoles.FirstOrDefault(u => u.UserId == id)?.RoleId;
        var role = _db.Roles.FirstOrDefault(u => u.Id == roleId)?.Name;
        UserRoleVM roleVm = new()
        {
            User = user,
            Roles = _db.Roles.Select(i=> new SelectListItem()
            {
                Text = i.Name,
                Value = i.Id.ToString(),
                Selected = i.Id == roleId
            }),
            Companies = _db.Companies.Select(i=>new SelectListItem()
            {
                Text = i.Name,
                Value = i.Id.ToString()
            })
        };
        roleVm.User.Role = role;
        
        return Json(new {data=roleVm});
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var objUsersList = _db.ApplicationUsers.Include(u=>u.Company).ToList();
        var userRoles = _db.UserRoles.ToList();
        var roles = _db.Roles.ToList();
        foreach (var user in objUsersList)
        {
            var roleId = userRoles.FirstOrDefault(u => u.UserId == user.Id)?.RoleId;
            user.Role = roles.FirstOrDefault(u => u.Id == roleId)?.Name;
            
            if (user.Company == null)
            {
                user.Company = new() { Name = "" };
            }
        }
        return Json(new { data = objUsersList });
    }
    
    
    
    [HttpPost]
    public IActionResult LockUnlock([FromBody] string id)
    {
        var objFromDb = _db.ApplicationUsers.FirstOrDefault(u => u.Id == id);
        if (objFromDb == null)
        {
            return Json(new { sucess = false, message = $"Error while locking/unlocking for user with id {id}." });
        }

        if (objFromDb.LockoutEnd != null && objFromDb.LockoutEnd > DateTime.UtcNow)
        {
            objFromDb.LockoutEnd = DateTime.UtcNow;
        }
        else
        {
            objFromDb.LockoutEnd = DateTime.UtcNow.AddYears(1000);
        }

        _db.SaveChanges();
        return Json(new { success = true, message = "Operation successful" });
    }
    

    #endregion
}