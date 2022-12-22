using DirectScale.Disco.Extension.Middleware;
using DirectScale.Disco.Extension.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using WebExtension.Repositories;
using WebExtension.Services.ZiplingoEngagement;
using WebExtension.Services.DailyRun;

namespace WebExtension.Controllers
{
    [Route("api/webhooks")]
    [ApiController]
    public class DailyEventController : ControllerBase
    {
        private readonly ISettingsService _settingsService;
        private readonly IZiplingoEngagementService _ziplingoEngagementService;
        private readonly IDailyRunService _dailyRunService;
        private readonly ICustomLogRepository _customLogRepository;

        public DailyEventController(ISettingsService settingsService, IZiplingoEngagementService ziplingoEngagementService, IDailyRunService dailyRunService, ICustomLogRepository customLogRepository)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _ziplingoEngagementService = ziplingoEngagementService ?? throw new ArgumentNullException(nameof(ziplingoEngagementService));
            _dailyRunService = dailyRunService ?? throw new ArgumentNullException(nameof(dailyRunService));
            _customLogRepository = customLogRepository ?? throw new ArgumentNullException(nameof(customLogRepository));
        }

        [ExtensionAuthorize]
        [HttpPost("DailyEvent")]
        public async Task<IActionResult> DailyEvent()
        {
            try
            {
                var extensionContext = await _settingsService.ExtensionContext();

                if (extensionContext.EnvironmentType == DirectScale.Disco.Extension.EnvironmentType.Live)
                {
                    try
                    {
                        _ziplingoEngagementService.AssociateBirthDateTrigger();
                        _ziplingoEngagementService.AssociateWorkAnniversaryTrigger();
                        _dailyRunService.FiveDayRun();
                        _dailyRunService.SentNotificationOnCardExpiryBefore30Days();
                        _dailyRunService.ExecuteCommissionEarned();
                    }
                    catch (Exception ex)
                    {
                        _customLogRepository.CustomErrorLog(0, 0, "Error with in daily run hook", ex.Message);
                    }
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ex.Message });
            }
        }
    }
}