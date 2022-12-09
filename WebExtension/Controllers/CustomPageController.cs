using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Middleware;
using DirectScale.Disco.Extension.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebExtension.Services;
using WebExtension.Services.ZiplingoEngagement;
using WebExtension.Services.ZiplingoEngagement.Model;

namespace WebExtension.Controllers
{
    public class resObj {
        public ZiplingoEngagementSettings settings { get; set; }
        public List<ZiplingoEventSettings> eventSettings { get; set; }
    }

    public class CustomPageController : Controller
    {
        private readonly IZiplingoEngagementRepository _ziplingoEngagementRepository;
        public CustomPageController(IZiplingoEngagementRepository ziplingoEngagementRepository)
        {
            _ziplingoEngagementRepository = ziplingoEngagementRepository ?? throw new ArgumentNullException(nameof(ziplingoEngagementRepository));
        }
        public IActionResult EWalletSettings()
        {
            var ewalletSetting = _ziplingoEngagementRepository.GetEWalletSetting();
            ViewBag.Message = ewalletSetting;
            return View();
        }

      
        public IActionResult ZiplingoEngagementSetting()
        {
            ZiplingoEngagementSettings _settings = _ziplingoEngagementRepository.GetSettings();
            List<ZiplingoEventSettings> _eventSettings = _ziplingoEngagementRepository.GetEventSettingsList();
            resObj viewDataSend = new resObj() {  settings = _settings, eventSettings = _eventSettings};
            ViewBag.Message = viewDataSend;
            return View();
        }

        


    }
}
