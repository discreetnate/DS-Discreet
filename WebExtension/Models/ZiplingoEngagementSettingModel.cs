using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebExtension.Models
{
    public class ZiplingoEngagementSettingModel
    {
        public string CompanyName { get; set; }
        public string ApiUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string LogoUrl { get; set; }
        public sbyte AllowBirthday { get; set; }
        public sbyte AllowAnniversary { get; set; }
        public sbyte AllowRankAdvancement { get; set; }
    }
}
