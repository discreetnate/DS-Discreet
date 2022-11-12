using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebExtension.Services.ZiplingoEngagement.Model
{
    public class AssociateWorkAnniversaryInfoList
    {
        public int AssociateId { get; set; }
        public string Birthdate { get; set; }
        public string EmailAddress { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string SignupDate { get; set; }
        public int TotalWorkingYears { get; set; }
    }
}
