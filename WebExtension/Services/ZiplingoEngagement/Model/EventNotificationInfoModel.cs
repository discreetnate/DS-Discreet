using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebExtension.Services.ZiplingoEngagement.Model
{
    public class EventNotificationInfoModel
    {
        public string Event { get; set; }
        public DateTime EventDate { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MemberFirstName { get; set; }
        public string MemberLastName { get; set; }
        public string EmailAddress { get; set; }
        public string PrimaryPhone { get; set; }
        public string Email { get; set; }
        public string LogoUrl { get; set; }
        public string CompanyName { get; set; }
        public string CompanyDomain { get; set; }
        public string ErrorDetails { get; set; }
    }
}
