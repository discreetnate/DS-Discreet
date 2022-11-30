using System;
using System.Collections.Generic;
using WebExtension.ThirdParty.ZiplingoEngagement.Model;

namespace WebExtension.ThirdParty.Interfaces
{
    public interface IZiplingoEngagementRepository
    {
        ZiplingoEngagementSettings GetSettings();
        void UpdateSettings(ZiplingoEngagementSettingsRequest settings);
        void ResetSettings();
        ShipInfo GetOrderNumber(int packageId);
        List<AssociateInfo> AssociateBirthdayWishesInfo();
        List<AssociateInfo> AssociateWorkAnniversaryInfo();
        AutoshipFromOrderInfo GetAutoshipFromOrder(int OrderNumber);
        string GetUsernameById(string associateId);
        string GetLastFoutDegitByOrderNumber(int orderId);
        string GetStatusById(int statusId);

        List<ShippingTrackingInfo> GetShippingTrackingInfo();
    }
}
