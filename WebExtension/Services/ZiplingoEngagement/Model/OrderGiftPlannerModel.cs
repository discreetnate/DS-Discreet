
using DirectScale.Disco.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebExtension.Services.ZiplingoEngagement.Model
{
    public class OrderGiftPlannerModel
    {
        public int EventId { get; set; }
        public string GiftNotes { get; set; }
        public Order Order { get; set; }
        public int MemberId { get; set; }
    }
}
