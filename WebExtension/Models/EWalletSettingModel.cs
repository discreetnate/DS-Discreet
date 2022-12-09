using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebExtension.Models
{
    public class EWalletSettingModel
    {
        public string CompanyId {get; set; }
        public string PointAccountId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int BackupMerchantId { get; set; }
        public sbyte SplitPayment { get; set; }
        public string ApiUrl { get; set; }
    }
}
