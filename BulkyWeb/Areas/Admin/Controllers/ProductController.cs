using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.RoleAdmin)]
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
        
        productVm.Product = _unitOfWork.Product.Get(u=>u.Id == id, includeProperties:"ProductImages");
        return View(productVm); // Update

    }

    public IActionResult DeleteImage(int imageId)
    {
        var imageToBeDeleted = _unitOfWork.ProductImage.Get(u => u.Id == imageId);
        int productId = imageToBeDeleted.ProductId;
        if (imageToBeDeleted != null)
        {
            if (!string.IsNullOrEmpty(imageToBeDeleted.ImageUrl))
            {
                var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, imageToBeDeleted.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }
            _unitOfWork.ProductImage.Remove(imageToBeDeleted);
            _unitOfWork.Save();

            TempData["success"] = "Deleted Successfully.";
        }

        return RedirectToAction(nameof(Upsert), new { id = productId });
    }
    
    [HttpPost]
    public IActionResult Upsert(ProductVM obj, List<IFormFile>? files)
    {
        
        if (ModelState.IsValid)
        {
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
            
            string wwwRootPath = _webHostEnvironment.WebRootPath;
            string finalPath = "";
            if (files != null)
            {
                foreach (var file in files)
                {
                    string fileName = Guid.NewGuid().ToString() +Path.GetExtension(file.FileName);
                    string productPath = @"images/products/product-" + obj.Product.Id;
                    finalPath = Path.Combine(wwwRootPath, productPath);

                    if (!Directory.Exists(finalPath))
                        Directory.CreateDirectory(finalPath);
                    
                    using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }

                    ProductImage productImage = new()
                    {
                        ImageUrl = @"/" + productPath + @"/" + fileName,
                        ProductId = obj.Product.Id
                    };

                    obj.Product.ProductImages ??= new();
                    
                    obj.Product.ProductImages.Add(productImage);
                }
                
                _unitOfWork.Product.Update(obj.Product);
                _unitOfWork.Save();
            }

          
            
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

    #region API_CALLS

    [HttpGet]
    public IActionResult GetAll()
    {
        var objProductList = _unitOfWork.Product.GetAll(includeProperties:"Category").ToList();
        return Json(new { data = objProductList });
    }
    
    [HttpDelete]
    public IActionResult Delete(int? id)
    {
        var productToBeDeleted = _unitOfWork.Product.Get(u => u.Id == id);
        if (productToBeDeleted == null)
        {
            return Json(new { sucess = false, message = "Error while deleting" });
        }
        
        string productPath = @"images/products/product-" + id;
        string finalPath = Path.Combine(_webHostEnvironment.WebRootPath, productPath);

        if (Directory.Exists(finalPath))
        {
            string[] filePaths = Directory.GetFiles(finalPath);
            foreach (var filePath in filePaths)
                System.IO.File.Delete(filePath);
            
            Directory.Delete(finalPath);
        }
            
        
        _unitOfWork.Product.Remove(productToBeDeleted);
        _unitOfWork.Save();
        
        return Json(new { sucess = true, message = "Product deleted successfully" });
    }
    

    #endregion
}