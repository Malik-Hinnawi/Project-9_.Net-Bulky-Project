using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
public class ProductController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _webHostEnvironment;
    public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
    {
        _unitOfWork = unitOfWork;
        _webHostEnvironment = webHostEnvironment;
    }
    
    // GET
    public IActionResult Index()
    {
        var objProductList = _unitOfWork.Product.GetAll(includeProperties:"Category").ToList();
        return View(objProductList);
    }

    public IActionResult Upsert(int? id)
    {
        // ViewBag.CategoryList = CategoryList;
        // ViewData["Category List"] = CategoryList;
        ProductVM productVm = new ProductVM()
        {
            CategoryList = _unitOfWork.Category
                .GetAll().Select(u => new SelectListItem()
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }),
            Product = new Product()
        };
        if(id == null || id == 0)
            return View(productVm); // Create
        
        productVm.Product = _unitOfWork.Product.Get(u=>u.Id == id);
        return View(productVm); // Update

    }
    
    [HttpPost]
    public IActionResult Upsert(ProductVM obj, IFormFile? file)
    {
        if (ModelState.IsValid)
        {
            string wwwRootPath = _webHostEnvironment.WebRootPath;
            string productPath = "";
            if (file != null)
            {
                string fileName = Guid.NewGuid().ToString() +Path.GetExtension(file.FileName);
                productPath = Path.Combine(wwwRootPath, @"images/product");

                if (!string.IsNullOrEmpty(obj.Product.ImageUrl))
                {
                    // Delete old image
                    var oldImagePath = Path.Combine(wwwRootPath, obj.Product.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }
                using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                {
                    file.CopyTo(fileStream);
                }

                obj.Product.ImageUrl = @"/images/product/" + fileName;
            }

            if (obj.Product.Id == 0)
            {
                _unitOfWork.Product.Add(obj.Product);
                TempData["success"] = "Product created successfully";
            }
            else
            {
                _unitOfWork.Product.Update(obj.Product);
                TempData["success"] = "Product updated successfully";
            }
            _unitOfWork.Save();
            
            return RedirectToAction("Index", "Product");
        }

        obj.CategoryList = _unitOfWork.Category
            .GetAll().Select(u => new SelectListItem()
            {
                Text = u.Name,
                Value = u.Id.ToString()
            });
        return View(obj);
    }
    
    public IActionResult Delete(int? id)
    {
        if (id == null || id == 0)
        {
            return NotFound();
        }

        Product? ProductFromDb = _unitOfWork.Product.Get(u=>u.Id == id);
        if (ProductFromDb == null)
        {
            return NotFound();
        }
        
        return View(ProductFromDb);
    }
    
    [HttpPost, ActionName("Delete")]
    public IActionResult DeletePOST(int? id)
    {
        Product obj = _unitOfWork.Product.Get(u=>u.Id == id);
        if (obj == null)
        {
            return NotFound();
        }

        _unitOfWork.Product.Remove(obj);
        _unitOfWork.Save();
        TempData["success"] = "Product deleted successfully";
        return RedirectToAction("Index", "Product");
        
    }
    
}