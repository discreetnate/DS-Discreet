using System;

namespace WebExtension.ThirdParty.ZiplingoEngagement.Model
{
    public class ZiplingoEngagementSettingsRequest : CommandRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string ApiUrl { get; set; }
        public string LogoUrl { get; set; }
        public string CompanyName { get; set; }
        public bool AllowBirthday { get; set; }
        public bool AllowAnniversary { get; set; }
        public bool AllowRankAdvancement { get; set; }
    }
}
