using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebExtension.Models
{
    public class GetNomadEwalletTokenResponse
    {
        public string token { get; set; }
        public DateTime expiration { get; set; }
    }
}
