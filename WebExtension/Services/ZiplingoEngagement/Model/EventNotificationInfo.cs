using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebExtension.Services.ZiplingoEngagement.Model
{
    public class EventNotificationInfo
    {
        public int AssociateId { get; set; }
        public string Event { get; set; }
        public DateTime EventDate { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MemberFirstName { get; set; }
        public string MemberLastName { get; set; }
        public string PrimaryPhone { get; set; }
        public string Email { get; set; }
    }
}
