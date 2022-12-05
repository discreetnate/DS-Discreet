using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebExtension.Helper;
using WebExtension.Helper.Interface;
using WebExtension.Helper.Models;
using WebExtension.Model;
using WebExtension.Models;
using WebExtension.Services;

namespace WebExtension.Controllers
{
    [Route("api/[controller]")]
    public class NomadEwalletController : Controller
    {
        
        private readonly INomadEwalletService _nomadEwalletService;
        public NomadEwalletController(
           
            INomadEwalletService nomadEwalletService
        )
        {
          
            _nomadEwalletService = nomadEwalletService ?? throw new ArgumentNullException(nameof(nomadEwalletService));
        }


        [HttpPost]
        [Route("GetNomadEwalletAccountBalance")]
        public async Task<IActionResult> GetNomadEwalletAccountBalance([FromBody] GetNomadEwalletAccountBalanceRequest request)
        {
            try
            {
                return new Responses().OkResult(await _nomadEwalletService.GetNomadEwalletAccountBalance(request));
            }
            catch (Exception ex)
            {
                return new Responses().BadRequestResult(ex.Message);
            }
        }

        [HttpPost]
        [Route("GetSingleSignON")]
        public async Task<IActionResult> GetSingleSignON([FromBody] SingleSignOnRequest request)
        {
            try
            {
                return new Responses().OkResult(await _nomadEwalletService.SingleSignOn(request));
            }
            catch (Exception ex)
            {
                return new Responses().BadRequestResult(ex.Message);
            }
        }

    }
}
