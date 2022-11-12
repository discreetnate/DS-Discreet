using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Orders;
using DirectScale.Disco.Extension.Services;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using DirectScale.Disco.Extension.Hooks.Associates;
using WebExtension.Repositories;
using WebExtension.Services.ZiplingoEngagement;

namespace WebExtension.Hooks.Associate
{
    public class UpdateAssociateHook : IHook<UpdateAssociateHookRequest, UpdateAssociateHookResponse>
    {
        private readonly IZiplingoEngagementService _ziplingoEngagementService;
        private readonly IAssociateService _associateService;
        private readonly ICustomLogRepository _customLogRepository;


        public UpdateAssociateHook(IAssociateService associateService, IZiplingoEngagementService ziplingoEngagementService, ICustomLogRepository customLogRepository)
        {
            _ziplingoEngagementService = ziplingoEngagementService ?? throw new ArgumentNullException(nameof(ziplingoEngagementService));
            _associateService = associateService ?? throw new ArgumentNullException(nameof(associateService));
            _customLogRepository = customLogRepository ?? throw new ArgumentNullException(nameof(customLogRepository));
        }

        public async Task<UpdateAssociateHookResponse> Invoke(UpdateAssociateHookRequest request, Func<UpdateAssociateHookRequest, Task<UpdateAssociateHookResponse>> func)
        {

            var result = await func(request);

            try
            {
                if (request.OldAssociateInfo.AssociateBaseType != request.UpdatedAssociateInfo.AssociateBaseType)
                {
                    // Call AssociateTypeChange Trigger
                    var OldAssociateType = await _associateService.GetAssociateTypeName(request.OldAssociateInfo.AssociateBaseType);
                    var UpdatedAssociateType = await _associateService.GetAssociateTypeName(request.UpdatedAssociateInfo.AssociateBaseType);
                    _ziplingoEngagementService.UpdateAssociateType(request.UpdatedAssociateInfo.AssociateId, OldAssociateType, UpdatedAssociateType, request.UpdatedAssociateInfo.AssociateBaseType);
                }

                if (request.OldAssociateInfo.StatusId != request.UpdatedAssociateInfo.StatusId)
                {
                    _ziplingoEngagementService.AssociateStatusChangeTrigger(request.UpdatedAssociateInfo.AssociateId, request.OldAssociateInfo.StatusId, request.UpdatedAssociateInfo.StatusId);
                }
                
                var associate = await _associateService.GetAssociate(request.UpdatedAssociateInfo.AssociateId);
                _ziplingoEngagementService.UpdateContact(associate);
            }
            catch (Exception ex)
            {
                _customLogRepository.CustomErrorLog(request.OldAssociateInfo.AssociateBaseType, request.UpdatedAssociateInfo.AssociateBaseType, "Error while calling update associate hook", "Error : " + ex.Message);
            }

            return result;
        }
    }
}