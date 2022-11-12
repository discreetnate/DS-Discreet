using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebExtension.Services.ZiplingoEngagement.Model
{
    public class GetSheduleEventResponse
    {
        public int MemberId { get; set; }
        public int AssociateID { get; set; }
        public string FirstName  { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public string Event { get; set; }
        public DateTime EventDate { get; set; }
        public DateTime NextEventDate { get; set; }
        public string MemberFirstName { get; set; }
        public string MemberLastName { get; set; }
        public string MemberEmailAddress { get; set; }
    }
}
