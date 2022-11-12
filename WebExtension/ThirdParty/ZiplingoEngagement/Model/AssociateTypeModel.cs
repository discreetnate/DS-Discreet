using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebExtension.ThirdParty.ZiplingoEngagement.Model
{
    public class AssociateTypeModel
    {
        public int AssociateId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string OldAssociateBaseType { get; set; }
        public string NewAssociateBaseType { get; set; }
        public string LogoUrl { get; set; }
        public string CompanyName { get; set; }
        public string CompanyDomain { get; set; }
    }
}
