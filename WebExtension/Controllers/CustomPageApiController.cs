using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebExtension.Services.ZiplingoEngagement;
using WebExtension.Services.ZiplingoEngagement.Model;

namespace WebExtension.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomPageApiController : ControllerBase
    {
        private readonly IZiplingoEngagementRepository _ziplingoEngagementRepository;

        public CustomPageApiController(IZiplingoEngagementRepository ziplingoEngagementRepository)
        {
            _ziplingoEngagementRepository = ziplingoEngagementRepository ?? throw new ArgumentNullException(nameof(ziplingoEngagementRepository));
        }

        [HttpPost]
        [Route("UpdateEwalletSettings")]
        public IActionResult UpdateEWalletSettings()
        {
            try
            {

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPost]
        [Route("UpdateZiplingoEngagementSettings")]
        public IActionResult UpdateZiplingoEngagementSettings(ZiplingoEngagementSettingsRequest request)
        {
            try
            {
                _ziplingoEngagementRepository.UpdateSettings(request);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPost]
        [Route("UpdateZiplingoEventSettings")]
        public IActionResult UpdateZiplingoEventSettings(ZiplingoEventSettingRequest request)
        {
            try
            {
                _ziplingoEngagementRepository.UpdateEventSetting(request);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
    }
}
