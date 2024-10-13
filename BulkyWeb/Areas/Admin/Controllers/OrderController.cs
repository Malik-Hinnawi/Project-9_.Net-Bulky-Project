using System.Security.Claims;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class OrderController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _webHostEnvironment;
    
    [BindProperty]
    public OrderVM OrderVm { get; set; }
    
    public OrderController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
    {
        _unitOfWork = unitOfWork;
        _webHostEnvironment = webHostEnvironment;
    }
    // GET
    public IActionResult Index()
    {
        return View();
    }
    
    public IActionResult Details(int orderId)
    {
        OrderVm = new()
        {
            OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId, includeProperties: "ApplicationUser"),
            OrderDetails = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderId, includeProperties:"Product")
        };
        return View(OrderVm);
    }
    
    [Authorize(Roles = SD.RoleAdmin + "," + SD.RoleEmployee)]
    [HttpPost]
    public IActionResult UpdateOrderDetail()
    {
        var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVm.OrderHeader.Id);
        orderHeaderFromDb.Name = OrderVm.OrderHeader.Name;
        orderHeaderFromDb.PhoneNumber = OrderVm.OrderHeader.PhoneNumber;
        orderHeaderFromDb.StreetAddress = OrderVm.OrderHeader.StreetAddress;
        orderHeaderFromDb.City = OrderVm.OrderHeader.City;
        orderHeaderFromDb.State = OrderVm.OrderHeader.State;
        orderHeaderFromDb.PostalCode = OrderVm.OrderHeader.PostalCode;
        if (!string.IsNullOrEmpty(OrderVm.OrderHeader.Carrier))
        {
            orderHeaderFromDb.Carrier = OrderVm.OrderHeader.Carrier;
        }
        if (!string.IsNullOrEmpty(OrderVm.OrderHeader.TrackingNumber))
        {
            orderHeaderFromDb.TrackingNumber = OrderVm.OrderHeader.TrackingNumber;
        }
        _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
        _unitOfWork.Save();

        TempData["success"] = "Order Details updated Successfully.";
        return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });
    }
    
    [Authorize(Roles = SD.RoleAdmin + "," + SD.RoleEmployee)]
    [HttpPost]
    public IActionResult StartProcessing()
    {
    
        
        _unitOfWork.OrderHeader.UpdateStatus(OrderVm.OrderHeader.Id, SD.StatusInProcess);
        _unitOfWork.Save();
        
        TempData["success"] = "Order Status updated Successfully.";
        return RedirectToAction(nameof(Details), new { orderId = OrderVm.OrderHeader.Id });
    }
    
    [Authorize(Roles = SD.RoleAdmin + "," + SD.RoleEmployee)]
    [HttpPost]
    public IActionResult ShipOrder()
    {
        var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVm.OrderHeader.Id);
        orderHeaderFromDb.TrackingNumber = OrderVm.OrderHeader.TrackingNumber;
        orderHeaderFromDb.Carrier = OrderVm.OrderHeader.Carrier;
        orderHeaderFromDb.OrderStatus = SD.StatusShipped;
        orderHeaderFromDb.ShippingDate = DateTime.UtcNow;
        if (orderHeaderFromDb.PaymentStatus == SD.PaymentStatusDelayedPayment)
        {
            orderHeaderFromDb.PaymentDueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        }
        
        _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
        _unitOfWork.Save();
        
        TempData["success"] = "Order Shipped Successfully.";
        return RedirectToAction(nameof(Details), new { orderId = OrderVm.OrderHeader.Id });
    }
    
    [Authorize(Roles = SD.RoleAdmin + "," + SD.RoleEmployee)]
    [HttpPost]
    public IActionResult CancelOrder()
    {
        var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVm.OrderHeader.Id);
        if (orderHeaderFromDb.PaymentStatus == SD.PaymentStatusApproved)
        {
            _unitOfWork.OrderHeader.UpdateStatus(orderHeaderFromDb.Id, SD.StatusCancelled, SD.StatusRefunded);
        }
        else
        {
            _unitOfWork.OrderHeader.UpdateStatus(orderHeaderFromDb.Id, SD.StatusCancelled, SD.StatusCancelled);
        }
        
        _unitOfWork.Save();
        
        TempData["success"] = "Order Cancelled Successfully.";
        return RedirectToAction(nameof(Details), new { orderId = OrderVm.OrderHeader.Id });
    }
    
    [Authorize(Roles = SD.RoleAdmin + "," + SD.RoleEmployee)]
    [HttpPost]
    public IActionResult PayNow()
    {
        if (OrderVm.OrderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
        {
            _unitOfWork.OrderHeader.UpdateStatus(OrderVm.OrderHeader.Id, OrderVm.OrderHeader.OrderStatus, SD.StatusApproved);
            _unitOfWork.Save();
        }
        
        TempData["success"] = "Order Paid Successfully.";
        return RedirectToAction(nameof(Details), new { orderId = OrderVm.OrderHeader.Id });
    }
    
    #region API_CALLS

    [HttpGet]
    public IActionResult GetAll(string? status)
    {
        var objOrderHeadersList = _unitOfWork.OrderHeader.GetAll(includeProperties:"ApplicationUser");
        if (!(User.IsInRole(SD.RoleAdmin) || User.IsInRole(SD.RoleEmployee)))
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            objOrderHeadersList = objOrderHeadersList.Where(u => u.ApplicationUserId == userId);
        }
        switch (status)
        {
            case "pending":
                objOrderHeadersList =
                    objOrderHeadersList.Where(u => u.PaymentStatus.Equals(SD.PaymentStatusDelayedPayment));
                break;
            case "inprocess":
                objOrderHeadersList =
                    objOrderHeadersList.Where(u => u.OrderStatus.Equals(SD.StatusInProcess));
                break;
            case "completed":
                objOrderHeadersList =
                    objOrderHeadersList.Where(u => u.OrderStatus.Equals(SD.StatusCompleted));
                break;
            case "approved":
                objOrderHeadersList =
                    objOrderHeadersList.Where(u => u.OrderStatus.Equals(SD.StatusApproved));
                break;
        }
        return Json(new { data = objOrderHeadersList });
    }
    

    #endregion
}