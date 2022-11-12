using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Orders.Packages;
using System;
using System.Threading.Tasks;
using WebExtension.Repositories;
using WebExtension.Services.ZiplingoEngagement;

namespace WebExtension.Hooks
{
    public class MarkPackageShippedHook : IHook<MarkPackagesShippedHookRequest, MarkPackagesShippedHookResponse>
    {
        private readonly IZiplingoEngagementService _ziplingoEngagementService;
        private readonly ICustomLogRepository _customLogRepository;

        public MarkPackageShippedHook(IZiplingoEngagementService ziplingoEngagementService, ICustomLogRepository customLogRepository)
        {
            _ziplingoEngagementService = ziplingoEngagementService ?? throw new ArgumentNullException(nameof(ziplingoEngagementService));
            _customLogRepository = customLogRepository ?? throw new ArgumentNullException(nameof(customLogRepository));

        }
        public async Task<MarkPackagesShippedHookResponse> Invoke(MarkPackagesShippedHookRequest request, Func<MarkPackagesShippedHookRequest, Task<MarkPackagesShippedHookResponse>> func)
        {
            var result = await func(request);
            try
            {
                foreach (var shipInfo in request.PackageStatusUpdates)
                {
                    _ziplingoEngagementService.SendOrderShippedEmail(shipInfo.PackageId, shipInfo.TrackingNumber);
                }
            }
            catch (Exception ex)
            {
                _customLogRepository.CustomErrorLog(request.PackageStatusUpdates[0].PackageId, request.PackageStatusUpdates[0].PackageId, "", "Error : " + ex.Message);
            }
            return result;
        }
    }
}