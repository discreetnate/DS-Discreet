using System;

namespace WebExtension.Services.ZiplingoEngagement.Model
{
    public class UpcomingServiceExpiryModel
    {
        public int associateid { get; set; }
        public int serviceid { get; set; }
        public string servicename { get; set; }
        public string firstname { get; set; }
        public string lastname { get; set; }
        public DateTime expirationdate { get; set; }
    }
}

