using DirectScale.Disco.Extension;
using System.Collections.Generic;
using WebExtension.ThirdParty.ZiplingoEngagement.Model;
using DirectScale.Disco.Extension.Hooks.Commissions;

namespace WebExtension.ThirdParty.ZiplingoEngagement.Interfaces
{
    public interface IZiplingoEngagementService
    {
        void CallOrderZiplingoEngagementTrigger(Order order, string eventKey, bool FailedAutoship);
        void CreateEnrollContact(Order order);
        void CreateContact(Application req, ApplicationResponse response);
        void UpdateContact(Associate req);
        void ResetSettings(CommandRequest commandRequest);
        void SendOrderShippedEmail(int packageId, string trackingNumber);
        void AssociateBirthDateTrigger();
        void AssociateWorkAnniversaryTrigger();
        EmailOnNotificationEvent OnNotificationEvent(NotificationEvent notification);
        LogRealtimeRankAdvanceHookResponse LogRealtimeRankAdvanceEvent(LogRealtimeRankAdvanceHookRequest req);
        void ExpirationCardTrigger(List<CardInfo> cardinfo);
        void AssociateStatusChangeTrigger(int associateId, int oldStatusId, int newStatusId);
        void ExecuteCommissionEarned();

        string GetExecuteCommissionEarned();
        void CreateAutoshipTrigger(Autoship autoshipInfo);

        void UpdateAssociateType(int associateId, string oldAssociateType, string newAssociateType, int newAssociateTypeId);
    }
}
