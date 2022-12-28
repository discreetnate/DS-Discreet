using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebExtension.Models
{
    public class NomadEwalletAPIReturn
    {
        public string statusCode { get; set; }
        public List<ErrorMessage> errorMessages { get; set; }
        public string returnID { get; set; }
        public object returnIDDescription { get; set; }
        public Data data { get; set; }
    }
}


public class ErrorMessage
{
    public string field { get; set; }
    public string errorMessage { get; set; }
}
public class Data
{
    public string currencyName { get; set; }
    public int numericCode { get; set; }
    public int minorunit { get; set; }
    public string walletName { get; set; }
    public double balance { get; set; }
    public double availableBalance { get; set; }
    public double blockedBalance { get; set; }
    public bool suspended { get; set; }
    public bool isDefault { get; set; }
    public string accountBackground { get; set; }
    public string symbol { get; set; }
}