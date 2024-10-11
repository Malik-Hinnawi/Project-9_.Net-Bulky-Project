using System.Diagnostics;
using System.Security.Claims;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Customer.Controllers;

[Area("Customer")]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IUnitOfWork _unitOfWork;
    public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        var productList = _unitOfWork.Product.GetAll(includeProperties: "Category");
        return View(productList);
    }
    
    public IActionResult Details(int id)
    {
        var cart = new ShoppingCart()
        {
            Product = _unitOfWork.Product.Get(u=>u.Id == id, includeProperties: "Category"),
            Count = 1,
            ProductId = id
        };
        return View(cart);
    }
    
    [HttpPost]
    [Authorize]
    public IActionResult Details(ShoppingCart cart)
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
        cart.ApplicationUserId = userId;

        ShoppingCart? cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.ApplicationUserId == userId 
                                                                    && u.ProductId == cart.ProductId);

        if (cartFromDb != null)
        {
            cartFromDb.Count += cart.Count;
            _unitOfWork.ShoppingCart.Update(cartFromDb);
            TempData["success"] = "Item Updated in Cart Successfully";
        }
        else
        {
            _unitOfWork.ShoppingCart.Add(cart);
            TempData["success"] = "Item Added to Cart Successfully";
        }
        
        _unitOfWork.Save();

        
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}