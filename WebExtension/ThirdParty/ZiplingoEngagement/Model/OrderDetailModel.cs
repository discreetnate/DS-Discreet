using DirectScale.Disco.Extension;

namespace WebExtension.ThirdParty.ZiplingoEngagement.Model
{
    public class OrderDetailModel
    {
        public string TrackingNumber { get; set; }
        public int ShipMethodId { get; set; }
        public string Carrier { get; set; }
        public string DateShipped { get; set; }
        public Order Order { get; set; }
        public int AutoshipId { get; set; }
    }
}
