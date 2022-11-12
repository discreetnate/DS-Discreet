using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DirectScale.Disco.Extension.Services;
using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Autoships;
using WebExtension.Services.ZiplingoEngagement;

namespace WebExtension.Hooks.Autoships
{
    public class UpdateAutoshipHook : IHook<UpdateAutoshipHookRequest, UpdateAutoshipHookResponse>
    {
        private IZiplingoEngagementService _ziplingoEngagementService;
        private IAssociateService _associateService;
        private IAutoshipService _autoshipService;

        public UpdateAutoshipHook(IZiplingoEngagementService ziplingoEngagementService, IAssociateService associateService, IAutoshipService autoshipService)
        {
            _ziplingoEngagementService = ziplingoEngagementService ?? throw new ArgumentNullException(nameof(ziplingoEngagementService));
            _associateService = associateService ?? throw new ArgumentNullException(nameof(associateService));
            _autoshipService = autoshipService ?? throw new ArgumentNullException(nameof(autoshipService));
        }

        public async Task<UpdateAutoshipHookResponse> Invoke(UpdateAutoshipHookRequest request, Func<UpdateAutoshipHookRequest, Task<UpdateAutoshipHookResponse>> func)
        {
            var response = await func(request);

            var updatedAutoshipInfo = await _autoshipService.GetAutoship(request.AutoshipInfo.AutoshipId);
            _ziplingoEngagementService.UpdateAutoshipTrigger(updatedAutoshipInfo);
            var associateInfo = await _associateService.GetAssociate(request.AutoshipInfo.AssociateId);
            _ziplingoEngagementService.UpdateContact(associateInfo);

            return response;

        }
    }
}