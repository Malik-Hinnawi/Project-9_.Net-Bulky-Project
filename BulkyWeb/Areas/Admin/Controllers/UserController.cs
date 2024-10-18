using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.RoleAdmin)]
public class UserController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<UserController> _logger;
    
    public UserController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager, ILogger<UserController> logger, RoleManager<IdentityRole> roleManager)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _logger = logger;
        _roleManager = roleManager;
    }
    
    // GET
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult RoleManagement(string id)
    {
        var user = _unitOfWork.ApplicationUser.Get(u => u.Id == id, includeProperties:"Company");
        UserRoleVM roleVm = new()
        {
            User = user,
            Roles = _roleManager.Roles.Select(i=> new SelectListItem()
            {
                Text = i.Name,
                Value = i.Name
            }),
            Companies = _unitOfWork.Company.GetAll().Select(i=>new SelectListItem()
            {
                Text = i.Name,
                Value = i.Id.ToString()
            })
        };

        roleVm.User.Role = _userManager.GetRolesAsync(user).GetAwaiter().GetResult().FirstOrDefault();
        return View(roleVm);
    }
    
    [HttpPost]
    public IActionResult UpdateRole(UserRoleVM roleVm)
    {
        _logger.Log(LogLevel.Information,"Reached");
        var user = _unitOfWork.ApplicationUser.Get(u => u.Id == roleVm.User.Id);
        var oldRole = _userManager.GetRolesAsync(user).GetAwaiter().GetResult().FirstOrDefault();

        if (!(roleVm.User.Role == oldRole))
        {
            if (roleVm.User.Role == SD.RoleCompany)
            {
                user.CompanyId = roleVm.User.CompanyId;
            }

            if (oldRole == SD.RoleCompany)
            {
                user.CompanyId = null;
            }
            _unitOfWork.ApplicationUser.Update(user);
            _unitOfWork.Save();
            _userManager.RemoveFromRoleAsync(user, oldRole).GetAwaiter().GetResult();
            _userManager.AddToRoleAsync(user, roleVm.User.Role).GetAwaiter().GetResult();
        }
        else
        {
            if (oldRole == SD.RoleCompany && user.CompanyId != roleVm.User.CompanyId)
            {
                user.CompanyId = roleVm.User.CompanyId;
                _unitOfWork.ApplicationUser.Update(user);
                _unitOfWork.Save();
            }
        }
        
        return RedirectToAction(nameof(Index));
    }

    #region API_CALLS
    

    [HttpGet]
    public IActionResult GetAll()
    {
        var objUsersList = _unitOfWork.ApplicationUser.GetAll(includeProperties:"Company").ToList();
        foreach (var user in objUsersList)
        {
            user.Role = _userManager.GetRolesAsync(user).GetAwaiter().GetResult().FirstOrDefault();
            
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
        var objFromDb = _unitOfWork.ApplicationUser.Get(u => u.Id == id);
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
        _unitOfWork.ApplicationUser.Update(objFromDb);
        _unitOfWork.Save();
        return Json(new { success = true, message = "Operation successful" });
    }
    

    #endregion
}