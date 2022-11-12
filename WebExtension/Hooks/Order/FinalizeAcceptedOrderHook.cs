using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Orders;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using DirectScale.Disco.Extension.Services;
using WebExtension.Services.ZiplingoEngagement;
using WebExtension.Repositories;

namespace WebExtension.Hooks.Order
{
    public class FinalizeAcceptedOrderHook : IHook<FinalizeAcceptedOrderHookRequest, FinalizeAcceptedOrderHookResponse>
    {
        private readonly IAutoshipService _autoShipService;
        private readonly IOrderService _orderService;
        private readonly IZiplingoEngagementService _ziplingoEngagementService;
        private readonly ICustomLogRepository _customLogRepository;
        private readonly IAssociateService _associateService;
        private readonly IWebsiteService _websiteService;
        private readonly IUserService _userService;

        public FinalizeAcceptedOrderHook(IAutoshipService autoShipService, IOrderService orderService, IZiplingoEngagementService ziplingoEngagementService, ICustomLogRepository customLogRepository,IAssociateService associateService, IWebsiteService websiteService, IUserService userService)
        {
            _autoShipService = autoShipService ?? throw new ArgumentNullException(nameof(autoShipService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _ziplingoEngagementService = ziplingoEngagementService ?? throw new ArgumentNullException(nameof(ziplingoEngagementService));
            _customLogRepository = customLogRepository ?? throw new ArgumentNullException(nameof(customLogRepository));
            _associateService = associateService ?? throw new ArgumentNullException(nameof(associateService));
            _websiteService = websiteService ?? throw new ArgumentNullException(nameof(websiteService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }
        public async Task<FinalizeAcceptedOrderHookResponse> Invoke(FinalizeAcceptedOrderHookRequest request, Func<FinalizeAcceptedOrderHookRequest, Task<FinalizeAcceptedOrderHookResponse>> func)
        {
            // All the subscriptions and autoships that were on the account have now been deleted if necessary.
            // Perform base functionality to create all autoships and subscriptions.
            var result = await func(request);
            try
            {
                DirectScale.Disco.Extension.Order order = await _orderService.GetOrderByOrderNumber(request.Order.OrderNumber);
                if (order.OrderType == OrderType.Enrollment)
                {
                    _ziplingoEngagementService.CreateEnrollContact(order);
                }
                if (order.Status == OrderStatus.Paid || order.IsPaid)
                {
                    var totalOrders = _orderService.GetOrdersByAssociateId(request.Order.AssociateId, "").Result;
                    if (totalOrders.Length == 1)
                    {
                        _ziplingoEngagementService.CallOrderZiplingoEngagementTrigger(order, "FirstOrderCreated", false);
                        _ziplingoEngagementService.CallOrderZiplingoEngagementTrigger(order, "OrderCreated", false);
                    }
                    else
                    {
                        _ziplingoEngagementService.CallOrderZiplingoEngagementTrigger(order, "OrderCreated", false);
                    }
                }
                if (order.OrderType == OrderType.Autoship && (order.Status == OrderStatus.Declined || order.Status == OrderStatus.FraudRejected))
                {
                    _ziplingoEngagementService.CallOrderZiplingoEngagementTrigger(order, "AutoShipFailed", true);
                }
            }
            catch (Exception ex)
            {
                _customLogRepository.CustomErrorLog(request.Order.AssociateId, request.Order.OrderNumber,"", "Error : " + ex.Message);
            }
            return result;
        }
    }
}
