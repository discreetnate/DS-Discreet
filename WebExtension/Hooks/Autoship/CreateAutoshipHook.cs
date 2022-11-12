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
    public class CreateAutoshipHook : IHook<CreateAutoshipHookRequest, CreateAutoshipHookResponse>
    {

        private IZiplingoEngagementService _ziplingoEngagementService;
        private IAssociateService _associateService;

        public CreateAutoshipHook(IZiplingoEngagementService ziplingoEngagementService, IAssociateService associateService)
        {
            _ziplingoEngagementService = ziplingoEngagementService ?? throw new ArgumentNullException(nameof(ziplingoEngagementService));
            _associateService = associateService ?? throw new ArgumentNullException(nameof(associateService));
        }

        public async Task<CreateAutoshipHookResponse> Invoke(CreateAutoshipHookRequest request, Func<CreateAutoshipHookRequest, Task<CreateAutoshipHookResponse>> func)
        {
            var response = await func(request);

            _ziplingoEngagementService.CreateAutoshipTrigger(request.AutoshipInfo);
            var associateInfo = await _associateService.GetAssociate(request.AutoshipInfo.AssociateId);
            _ziplingoEngagementService.UpdateContact(associateInfo);

            return response;
        }
    }
}