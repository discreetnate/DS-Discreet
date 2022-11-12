using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebExtension.Repositories;
using WebExtension.Services.ZiplingoEngagement;

namespace WebExtension.Hooks.Order
{
    public class FinalizeNonAcceptedOrderHook : IHook<FinalizeNonAcceptedOrderHookRequest, FinalizeNonAcceptedOrderHookResponse>
    {
        private readonly IZiplingoEngagementService _ziplingoEngagementService;
        private readonly ICustomLogRepository _customLogRepository;

        public FinalizeNonAcceptedOrderHook(IZiplingoEngagementService ziplingoEngagementService, ICustomLogRepository customLogRepository)
        {
            _ziplingoEngagementService = ziplingoEngagementService ?? throw new ArgumentNullException(nameof(ziplingoEngagementService));
            _customLogRepository = customLogRepository ?? throw new ArgumentNullException(nameof(customLogRepository));
        }
        public async Task<FinalizeNonAcceptedOrderHookResponse> Invoke(FinalizeNonAcceptedOrderHookRequest request, Func<FinalizeNonAcceptedOrderHookRequest, Task<FinalizeNonAcceptedOrderHookResponse>> func)
        {
            var result = await func(request);
            try
            {
                if (request.Order.OrderType == OrderType.Autoship)
                {
                    _ziplingoEngagementService.CallOrderZiplingoEngagementTrigger(request.Order, "AutoShipFailed", true);
                }
            }
            catch (Exception ex)
            {
                _customLogRepository.CustomErrorLog(request.Order.AssociateId, request.Order.OrderNumber, "", "Error : " + ex.Message);
            }
            return result;
        }
    }
}
