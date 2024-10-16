using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.RoleAdmin)]
public class CompanyController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _webHostEnvironment;
    public CompanyController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
    {
        _unitOfWork = unitOfWork;
        _webHostEnvironment = webHostEnvironment;
    }
    
    // GET
    public IActionResult Index()
    {
        var objCompanyList = _unitOfWork.Company.GetAll().ToList();
        return View(objCompanyList);
    }

    public IActionResult Upsert(int? id)
    {
        // ViewBag.CategoryList = CategoryList;
        // ViewData["Category List"] = CategoryList;
        
        if(id == null || id == 0)
            return View(new Company()); // Create
        
        Company company = _unitOfWork.Company.Get(u=>u.Id == id);
        return View(company); // Update

    }
    
    [HttpPost]
    public IActionResult Upsert(Company obj)
    {
        if (ModelState.IsValid)
        {
            
            if (obj.Id == 0)
            {
                _unitOfWork.Company.Add(obj);
                TempData["success"] = "Company created successfully";
            }
            else
            {
                _unitOfWork.Company.Update(obj);
                TempData["success"] = "Company updated successfully";
            }
            _unitOfWork.Save();
            
            return RedirectToAction("Index", "Company");
        }
        
        return View(obj);
    }

    #region API_CALLS

    [HttpGet]
    public IActionResult GetAll()
    {
        var objCompanyList = _unitOfWork.Company.GetAll().ToList();
        return Json(new { data = objCompanyList });
    }
    
    [HttpDelete]
    public IActionResult Delete(int? id)
    {
        var companyToBeDeleted = _unitOfWork.Company.Get(u => u.Id == id);
        if (companyToBeDeleted == null)
        {
            return Json(new { sucess = false, message = "Error while deleting" });
        }
        
        _unitOfWork.Company.Remove(companyToBeDeleted);
        _unitOfWork.Save();
        
        return Json(new { sucess = true, message = "Company deleted successfully" });
    }
    

    #endregion
}