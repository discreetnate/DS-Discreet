﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebExtension.ThirdParty.ZiplingoEngagement.Model
{
    public class CardInfo
    {
        public int AssociateId { get; set; }
        public int Last4DegitOfCard { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PrimaryPhone { get; set; }
        public string Email { get; set; }
    }
}
