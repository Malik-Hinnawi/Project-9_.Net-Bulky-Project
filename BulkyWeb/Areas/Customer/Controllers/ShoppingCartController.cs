using System.Security.Claims;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize]
public class ShoppingCartController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    [BindProperty]
    public ShoppingCartVM ShoppingCartVm { get; set; }
    public ShoppingCartController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

        ShoppingCartVM ShoppingCartVM = new()
        {
            ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                includeProperties: "Product"),
            OrderHeader = new()
        };

        foreach (var cart in ShoppingCartVM.ShoppingCartList)
        {
            cart.Price = GetPriceBasedOnQuantity(cart);
            ShoppingCartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
        }
        return View(ShoppingCartVM);
    }
    
    public IActionResult Plus(int cartId)
    {
        var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
        cartFromDb.Count++;
        _unitOfWork.ShoppingCart.Update(cartFromDb);
        _unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }
    
    public IActionResult Minus(int cartId)
    {
        var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId, tracked:true);
        cartFromDb.Count--;
        if (cartFromDb.Count < 1)
        {
            HttpContext.Session.SetInt32(SD.SessionCart, 
                _unitOfWork.ShoppingCart.GetAll(u=>
                        u.ApplicationUserId == cartFromDb.ApplicationUserId)
                    .Count()-1);
            _unitOfWork.ShoppingCart.Remove(cartFromDb);
        }
        else
        {
            _unitOfWork.ShoppingCart.Update(cartFromDb);
        }
       
        _unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Remove(int cartId)
    {
        var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId, tracked:true);
        HttpContext.Session.SetInt32(SD.SessionCart, 
            _unitOfWork.ShoppingCart.GetAll(u=>
                    u.ApplicationUserId == cartFromDb.ApplicationUserId)
                .Count()-1);
        _unitOfWork.ShoppingCart.Remove(cartFromDb);
        _unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Summary()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

        ShoppingCartVM ShoppingCartVM = new()
        {
            ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
                includeProperties: "Product"),
            OrderHeader = new()
        };
        
        ApplicationUser currentUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

        ShoppingCartVM.OrderHeader.ApplicationUser = currentUser;
        ShoppingCartVM.OrderHeader.Name = currentUser.Name;
        ShoppingCartVM.OrderHeader.PhoneNumber = currentUser.PhoneNumber ?? "";
        ShoppingCartVM.OrderHeader.StreetAddress = currentUser.StreetAddress ?? "";
        ShoppingCartVM.OrderHeader.City = currentUser.City ?? "";
        ShoppingCartVM.OrderHeader.State = currentUser.State ?? "";
        ShoppingCartVM.OrderHeader.PostalCode = currentUser.PostalCode ?? "";
        
        foreach (var cart in ShoppingCartVM.ShoppingCartList)
        {
            cart.Price = GetPriceBasedOnQuantity(cart);
            ShoppingCartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
        }
        
        return View(ShoppingCartVM);
    }
    
    [HttpPost]
    [ActionName("Summary")]
    public IActionResult SummaryPost()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

        ShoppingCartVm.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
            includeProperties: "Product");

        ShoppingCartVm.OrderHeader.OrderDate = System.DateTime.UtcNow;
        ShoppingCartVm.OrderHeader.ApplicationUserId = userId;
        
        ApplicationUser user = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

        
        
        foreach (var cart in ShoppingCartVm.ShoppingCartList)
        {
            cart.Price = GetPriceBasedOnQuantity(cart);
            ShoppingCartVm.OrderHeader.OrderTotal += cart.Price * cart.Count;
        }

        if (user.CompanyId.GetValueOrDefault() == 0)
        {
            ShoppingCartVm.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            ShoppingCartVm.OrderHeader.OrderStatus = SD.StatusPending;
        }
        else
        { 
            // Company User:
            ShoppingCartVm.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
            ShoppingCartVm.OrderHeader.OrderStatus = SD.StatusApproved;
        }
        _unitOfWork.OrderHeader.Add(ShoppingCartVm.OrderHeader);
        _unitOfWork.Save();

        foreach (var cart in ShoppingCartVm.ShoppingCartList)
        {
            OrderDetail orderDetail = new ()
            {
                ProductId = cart.ProductId,
                OrderHeaderId = ShoppingCartVm.OrderHeader.Id,
                Price = cart.Price,
                Count = cart.Count
            };
            _unitOfWork.OrderDetail.Add(orderDetail);
            _unitOfWork.Save();
        }
        if (user.CompanyId.GetValueOrDefault() == 0)
        {
           
        }
        
        return RedirectToAction(nameof(OrderConfirmation), new
        {
            id= ShoppingCartVm.OrderHeader.Id
        });
    }

    public IActionResult OrderConfirmation(int id)
    {
        OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id, 
            includeProperties: "ApplicationUser");
        if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
        {
            _unitOfWork.OrderHeader.UpdateStatus(id,SD.StatusApproved, SD.PaymentStatusApproved );
            _unitOfWork.Save();
        }
        HttpContext.Session.Clear();

        List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart
            .GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList(); ;
        
        _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
        _unitOfWork.Save();
        return View(id);
    }
    
    private double GetPriceBasedOnQuantity(ShoppingCart cart)
    {
        if (cart.Count <= 50)
        {
            return cart.Product.Price;
        }

        if (cart.Count <= 100)
        {
            return cart.Product.Price50;
        }

        return cart.Product.Price100;
    }

}