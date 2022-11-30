using Newtonsoft.Json;
using System;
using System.Linq;
using WebExtension.ThirdParty.Interfaces;
using Microsoft.Extensions.Logging;
using WebExtension.ThirdParty.ZiplingoEngagement.Interfaces;
using WebExtension.ThirdParty.ZiplingoEngagement.Model;
using DirectScale.Disco.Extension.Services;
using DirectScale.Disco.Extension;
using System.Net.Http;
using System.Text;
using System.Collections.Generic;
using DirectScale.Disco.Extension.Hooks.Commissions;
using WebExtension.Helper;
using WebExtension.Services;
using System.Text.RegularExpressions;

namespace WebExtension.ThirdParty
{
    public class ZiplingoEngagementService : IZiplingoEngagementService
    {
        private readonly IZiplingoEngagementRepository _ZiplingoEngagementRepository;
        private readonly ICompanyService _companyService;
        private static readonly string ClassName = typeof(ZiplingoEngagementService).FullName;
        private readonly IOrderService _orderService;
        private readonly IAssociateService _distributorService;
        private readonly ITreeService _treeService;
        private readonly IRankService _rankService;
        private readonly IHttpClientService _httpClientService;
        private readonly IPaymentProcessingService _paymentProcessDataService;
        private readonly ICustomLogService _customLogService;

        public ZiplingoEngagementService(IZiplingoEngagementRepository repository, 
            ICompanyService companyService, 
            IOrderService orderService, 
            IAssociateService distributorService, 
            ITreeService treeService, 
            IRankService rankService,
            IHttpClientService httpClientService,
            IPaymentProcessingService paymentProcessDataService,
            ICustomLogService customLogService
            )
        {
            _ZiplingoEngagementRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            _companyService = companyService ?? throw new ArgumentNullException(nameof(companyService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _distributorService = distributorService ?? throw new ArgumentNullException(nameof(distributorService));
            _treeService = treeService ?? throw new ArgumentNullException(nameof(treeService));
            _rankService = rankService ?? throw new ArgumentNullException(nameof(rankService));
            _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
            _paymentProcessDataService = paymentProcessDataService ?? throw new ArgumentNullException(nameof(paymentProcessDataService));
            _customLogService = customLogService ?? throw new ArgumentNullException(nameof(customLogService));
        }

        #region private methods
        private Company GetCompany()
        {
            return _companyService.GetCompany().GetAwaiter().GetResult();
        }
        private (int, int) GetEnrollerAndSponsorID(int AssociateId)
        {
            int enrollerID = 0;
            int sponsorID = 0;
            if (_treeService.GetNodeDetail(new NodeId(AssociateId, 0), TreeType.Enrollment).GetAwaiter().GetResult().UplineId != null)
            {
                enrollerID = _treeService.GetNodeDetail(new NodeId(AssociateId, 0), TreeType.Enrollment).GetAwaiter().GetResult()?.UplineId.AssociateId ?? 0;
            }
            if (_treeService.GetNodeDetail(new NodeId(AssociateId, 0), TreeType.Unilevel).GetAwaiter().GetResult().UplineId != null)
            {
                sponsorID = _treeService.GetNodeDetail(new NodeId(AssociateId, 0), TreeType.Unilevel).GetAwaiter().GetResult()?.UplineId.AssociateId ?? 0;
            }
            return (enrollerID, sponsorID);
        }
        private (Associate, Associate) GetEnrollerAndSponsorSummary(int enrollerID, int sponsorID)
        {

            Associate sponsorSummary = new Associate();
            Associate enrollerSummary = new Associate();
            if (enrollerID <= 0)
            {
                enrollerSummary = new Associate();
            }
            else
            {
                enrollerSummary = _distributorService.GetAssociate(enrollerID).GetAwaiter().GetResult();
            }
            if (sponsorID > 0)
            {
                sponsorSummary = _distributorService.GetAssociate(sponsorID).GetAwaiter().GetResult();
            }
            else
            {
                sponsorSummary = enrollerSummary;
            }
            return (enrollerSummary, sponsorSummary);
        }
        #endregion

        public void CallOrderZiplingoEngagementTrigger(Order order, string eventKey, bool FailedAutoship)
        {
            try
            {
                var company = GetCompany();
                var settings = _ZiplingoEngagementRepository.GetSettings();
                (int enrollerID, int sponsorID) = GetEnrollerAndSponsorID(order.AssociateId);
                (Associate sponsorSummary, Associate enrollerSummary) = GetEnrollerAndSponsorSummary(enrollerID, sponsorID);

                var CardLastFourDegit = _ZiplingoEngagementRepository.GetLastFoutDegitByOrderNumber(order.OrderNumber);
                OrderData data = new OrderData
                {
                    ShipMethodId = order.Packages.Select(a => a.ShipMethodId).FirstOrDefault(),
                    AssociateId = order.AssociateId,
                    BackofficeId = order.BackofficeId,
                    Email = order.Email,
                    InvoiceDate = order.InvoiceDate,
                    IsPaid = order.IsPaid,
                    LocalInvoiceNumber = order.LocalInvoiceNumber,
                    Name = order.Name,
                    Phone=order.BillPhone,
                    OrderDate = order.OrderDate,
                    OrderNumber = order.OrderNumber,
                    OrderType = order.OrderType,
                    Tax = order.Totals.Select(m => m.Tax).FirstOrDefault(),
                    ShipCost = order.Totals.Select(m => m.Shipping).FirstOrDefault(),
                    Subtotal = order.Totals.Select(m => m.SubTotal).FirstOrDefault(),
                    Total = order.Totals.Select(m => m.Total).FirstOrDefault(),
                    Discount = order.Totals.Select(m => m.DiscountTotal).FirstOrDefault(),
                    CurrencySymbol = order.Totals.Select(m => m.CurrencySymbol).FirstOrDefault(),
                    PaymentMethod = CardLastFourDegit,
                    ProductInfo = order.LineItems,
                    ProductNames = string.Join(",", order.LineItems.Select(x => x.ProductName).ToArray()),
                    ErrorDetails = FailedAutoship ? order.Payments.FirstOrDefault().PaymentResponse.ToString() : "",
                    CompanyDomain = company.BackOfficeHomePageURL,
                    LogoUrl = settings.LogoUrl,
                    CompanyName = settings.CompanyName,
                    EnrollerId = enrollerSummary.AssociateId,
                    SponsorId = sponsorSummary.AssociateId,
                    EnrollerName = enrollerSummary.DisplayFirstName + ' ' + enrollerSummary.DisplayLastName,
                    EnrollerMobile = enrollerSummary.PrimaryPhone,
                    EnrollerEmail = enrollerSummary.EmailAddress,
                    SponsorName = sponsorSummary.DisplayFirstName + ' ' + sponsorSummary.DisplayLastName,
                    SponsorMobile = sponsorSummary.PrimaryPhone,
                    SponsorEmail = sponsorSummary.EmailAddress,
                    BillingAddress = order.BillAddress,
                    ShippingAddress = order.Packages?.FirstOrDefault()?.ShippingAddress
                };
                var strData = JsonConvert.SerializeObject(data);
                ZiplingoEngagementRequest request = new ZiplingoEngagementRequest { associateid = order.AssociateId, companyname = settings.CompanyName, eventKey = eventKey, data = strData };
                var jsonReq = JsonConvert.SerializeObject(request);
                CallZiplingoEngagementApi(jsonReq, "Campaign/ExecuteTrigger");
            }
            catch (Exception e)
            {
                _customLogService.SaveLog(order.AssociateId,order.OrderNumber, $"{ClassName}.CallOrderZiplingoEngagementTrigger", "Error",e.Message,"","",$"eventKey : {eventKey}, Order : {JsonConvert.SerializeObject(order)}, FailedAutoship : {FailedAutoship}",JsonConvert.SerializeObject(e));
            }
        }

        public void CallOrderZiplingoEngagementTriggerForShipped(OrderDetailModel order, string eventKey, bool FailedAutoship = false)
        {
            try
            {
                var company = _companyService.GetCompany().GetAwaiter().GetResult();
                var settings = _ZiplingoEngagementRepository.GetSettings();
                (int enrollerID, int sponsorID) = GetEnrollerAndSponsorID(order.Order.AssociateId);
                (Associate sponsorSummary, Associate enrollerSummary) = GetEnrollerAndSponsorSummary(enrollerID,sponsorID);

                var CardLastFourDegit = _ZiplingoEngagementRepository.GetLastFoutDegitByOrderNumber(order.Order.OrderNumber);


                // Track Shipping -----------------------------
                var TrackingUrl = "";
                var ShippingTrackingInfo = _ZiplingoEngagementRepository.GetShippingTrackingInfo();
                if (order.TrackingNumber != null)
                {
                    foreach (var shipInfo in ShippingTrackingInfo)
                    {
                        Match m1 = Regex.Match(order.TrackingNumber, shipInfo.TrackPattern, RegexOptions.IgnoreCase);
                        if (m1.Success)
                        {
                            TrackingUrl = shipInfo.ShippingUrl + order.TrackingNumber;
                            break;
                        }
                    }
                }

                // Track Shipping -----------------------------
                OrderData data = new OrderData
                {
                    ShipMethodId = order.ShipMethodId, //ShipMethodId added
                    AssociateId = order.Order.AssociateId,
                    BackofficeId = order.Order.BackofficeId,
                    Email = order.Order.Email,
                    InvoiceDate = order.Order.InvoiceDate,
                    IsPaid = order.Order.IsPaid,
                    LocalInvoiceNumber = order.Order.LocalInvoiceNumber,
                    Name = order.Order.Name,
                    Phone = order.Order.BillPhone,
                    OrderDate = order.Order.OrderDate,
                    OrderNumber = order.Order.OrderNumber,
                    OrderType = order.Order.OrderType,
                    Tax = order.Order.Totals.Select(m => m.Tax).FirstOrDefault(),
                    ShipCost = order.Order.Totals.Select(m => m.Shipping).FirstOrDefault(),
                    Subtotal = order.Order.Totals.Select(m => m.SubTotal).FirstOrDefault(),
                    Total = order.Order.Totals.Select(m => m.Total).FirstOrDefault(),
                    CurrencySymbol = order.Order.Totals.Select(m => m.CurrencySymbol).FirstOrDefault(),
                    PaymentMethod = CardLastFourDegit,
                    ProductInfo = order.Order.LineItems,
                    ProductNames = string.Join(",", order.Order.LineItems.Select(x => x.ProductName).ToArray()),
                    ErrorDetails = FailedAutoship ? order.Order.Payments.FirstOrDefault().PaymentResponse.ToString() : "",
                    CompanyDomain = company.BackOfficeHomePageURL,
                    LogoUrl = settings.LogoUrl,
                    TrackingNumber = order.TrackingNumber,
                    TrackingUrl = TrackingUrl,
                    Carrier = order.Carrier,
                    DateShipped = order.DateShipped,
                    CompanyName = settings.CompanyName,
                    EnrollerId = enrollerSummary.AssociateId,
                    SponsorId = sponsorSummary.AssociateId,
                    AutoshipId = order.AutoshipId,
                    EnrollerName = enrollerSummary.DisplayFirstName + ' ' + enrollerSummary.DisplayLastName,
                    EnrollerMobile = enrollerSummary.PrimaryPhone,
                    EnrollerEmail = enrollerSummary.EmailAddress,
                    SponsorName = sponsorSummary.DisplayFirstName + ' ' + sponsorSummary.DisplayLastName,
                    SponsorMobile = sponsorSummary.PrimaryPhone,
                    SponsorEmail = sponsorSummary.EmailAddress,
                    BillingAddress = order.Order.BillAddress,
                    ShippingAddress = order.Order.Packages?.FirstOrDefault()?.ShippingAddress
                };
                var strData = JsonConvert.SerializeObject(data);
                ZiplingoEngagementRequest request = new ZiplingoEngagementRequest { associateid = order.Order.AssociateId, companyname = settings.CompanyName, eventKey = eventKey, data = strData };
                var jsonReq = JsonConvert.SerializeObject(request);
                CallZiplingoEngagementApi(jsonReq, "Campaign/ExecuteTrigger");
            }
            catch (Exception e)
            {
                _customLogService.SaveLog(order.Order.AssociateId, order.Order.OrderNumber, $"{ClassName}.CallOrderZiplingoEngagementTriggerForShipped", "Error", e.Message, "", "", $"eventKey : {eventKey}, Order : {JsonConvert.SerializeObject(order)}, FailedAutoship : {FailedAutoship}", JsonConvert.SerializeObject(e));
            }
        }

        public void CallOrderZiplingoEngagementTriggerForBirthDayWishes(AssociateInfo assoInfo, string eventKey)
        {
            try
            {
                var company = _companyService.GetCompany().GetAwaiter().GetResult();
                var settings = _ZiplingoEngagementRepository.GetSettings();
                (int enrollerID, int sponsorID) = GetEnrollerAndSponsorID(assoInfo.AssociateId);
                (Associate sponsorSummary, Associate enrollerSummary) = GetEnrollerAndSponsorSummary(enrollerID, sponsorID);
                
                AssociateInfo data = new AssociateInfo
                {
                    AssociateId = assoInfo.AssociateId,
                    EmailAddress = assoInfo.EmailAddress,
                    Birthdate = assoInfo.Birthdate,
                    FirstName = assoInfo.FirstName,
                    LastName = assoInfo.LastName,
                    CompanyDomain = company.BackOfficeHomePageURL,
                    LogoUrl = settings.LogoUrl,
                    CompanyName = settings.CompanyName,
                    EnrollerId = enrollerSummary.AssociateId,
                    SponsorId = sponsorSummary.AssociateId,
                    CommissionActive = true,
                    EnrollerName = enrollerSummary.DisplayFirstName + ' ' + enrollerSummary.DisplayLastName,
                    EnrollerMobile = enrollerSummary.PrimaryPhone,
                    EnrollerEmail = enrollerSummary.EmailAddress,
                    SponsorName = sponsorSummary.DisplayFirstName + ' ' + sponsorSummary.DisplayLastName,
                    SponsorMobile = sponsorSummary.PrimaryPhone,
                    SponsorEmail = sponsorSummary.EmailAddress
                };
                var strData = JsonConvert.SerializeObject(data);
                ZiplingoEngagementRequest request = new ZiplingoEngagementRequest { associateid = assoInfo.AssociateId, companyname = settings.CompanyName, eventKey = eventKey, data = strData };
                var jsonReq = JsonConvert.SerializeObject(request);
                CallZiplingoEngagementApi(jsonReq, "Campaign/ExecuteTrigger");
            }
            catch (Exception e)
            {
                _customLogService.SaveLog(assoInfo.AssociateId, 0, $"{ClassName}.CallOrderZiplingoEngagementTriggerForBirthDayWishes", "Error", e.Message, "", "", $"eventKey : {eventKey}, assoInfo : {JsonConvert.SerializeObject(assoInfo)}", JsonConvert.SerializeObject(e));
            }
        }

        public void CallOrderZiplingoEngagementTriggerForWorkAnniversary(AssociateInfo assoInfo, string eventKey)
        {
            try
            {
                var company = GetCompany();
                var settings = _ZiplingoEngagementRepository.GetSettings();
                (int enrollerID, int sponsorID) = GetEnrollerAndSponsorID(assoInfo.AssociateId);
                (Associate sponsorSummary, Associate enrollerSummary) = GetEnrollerAndSponsorSummary(enrollerID, sponsorID);

                AssociateInfo data = new AssociateInfo
                {
                    AssociateId = assoInfo.AssociateId,
                    EmailAddress = assoInfo.EmailAddress,
                    SignupDate = assoInfo.SignupDate,
                    TotalWorkingYears = assoInfo.TotalWorkingYears,
                    FirstName = assoInfo.FirstName,
                    LastName = assoInfo.LastName,
                    CompanyDomain = company.BackOfficeHomePageURL,
                    LogoUrl = settings.LogoUrl,
                    CompanyName = settings.CompanyName,
                    EnrollerId = enrollerSummary.AssociateId,
                    SponsorId = sponsorSummary.AssociateId,
                    CommissionActive = true,
                    EnrollerName = enrollerSummary.DisplayFirstName + ' ' + enrollerSummary.DisplayLastName,
                    EnrollerMobile = enrollerSummary.PrimaryPhone,
                    EnrollerEmail = enrollerSummary.EmailAddress,
                    SponsorName = sponsorSummary.DisplayFirstName + ' ' + sponsorSummary.DisplayLastName,
                    SponsorMobile = sponsorSummary.PrimaryPhone,
                    SponsorEmail = sponsorSummary.EmailAddress
                };
                var strData = JsonConvert.SerializeObject(data);
                ZiplingoEngagementRequest request = new ZiplingoEngagementRequest { associateid = assoInfo.AssociateId, companyname = settings.CompanyName, eventKey = eventKey, data = strData };
                var jsonReq = JsonConvert.SerializeObject(request);
                CallZiplingoEngagementApi(jsonReq, "Campaign/ExecuteTrigger");
            }
            catch (Exception e)
            {
                _customLogService.SaveLog(assoInfo.AssociateId, 0, $"{ClassName}.CallOrderZiplingoEngagementTriggerForWorkAnniversary", "Error", e.Message, "", "", $"eventKey : {eventKey}, assoInfo : {JsonConvert.SerializeObject(assoInfo)}", JsonConvert.SerializeObject(e));
            }
        }

        public void CallOrderZiplingoEngagementTriggerForAssociateRankAdvancement(AssociateRankAdvancement assoRankAdvancementInfo, string eventKey)
        {
            try
            {
                var company = GetCompany();
                var settings = _ZiplingoEngagementRepository.GetSettings();
                (int enrollerID, int sponsorID) = GetEnrollerAndSponsorID(assoRankAdvancementInfo.AssociateId);
                (Associate sponsorSummary, Associate enrollerSummary) = GetEnrollerAndSponsorSummary(enrollerID, sponsorID);

                AssociateRankAdvancement data = new AssociateRankAdvancement
                {
                    Rank = assoRankAdvancementInfo.Rank,
                    AssociateId = assoRankAdvancementInfo.AssociateId,
                    FirstName = assoRankAdvancementInfo.FirstName,
                    LastName = assoRankAdvancementInfo.LastName,
                    CompanyDomain = company.BackOfficeHomePageURL,
                    LogoUrl = settings.LogoUrl,
                    CompanyName = settings.CompanyName,
                    EnrollerId = enrollerSummary.AssociateId,
                    SponsorId = sponsorSummary.AssociateId,
                    RankName = assoRankAdvancementInfo.RankName,
                    CommissionActive = true,
                    EnrollerName = enrollerSummary.DisplayFirstName + ' ' + enrollerSummary.DisplayLastName,
                    EnrollerMobile = enrollerSummary.PrimaryPhone,
                    EnrollerEmail = enrollerSummary.EmailAddress,
                    SponsorName = sponsorSummary.DisplayFirstName + ' ' + sponsorSummary.DisplayLastName,
                    SponsorMobile = sponsorSummary.PrimaryPhone,
                    SponsorEmail = sponsorSummary.EmailAddress
                };
                var strData = JsonConvert.SerializeObject(data);
                ZiplingoEngagementRequest request = new ZiplingoEngagementRequest { associateid = assoRankAdvancementInfo.AssociateId, companyname = settings.CompanyName, eventKey = eventKey, data = strData, rankid = assoRankAdvancementInfo.Rank };
                var jsonReq = JsonConvert.SerializeObject(request);
                CallZiplingoEngagementApi(jsonReq, "Campaign/RankAdvancement");
            }
            catch (Exception e)
            {
                _customLogService.SaveLog(assoRankAdvancementInfo.AssociateId, 0, $"{ClassName}.CallOrderZiplingoEngagementTriggerForAssociateRankAdvancement", "Error", e.Message, "", "", $"eventKey : {eventKey}, assoRankAdvancementInfo : {JsonConvert.SerializeObject(assoRankAdvancementInfo)}", JsonConvert.SerializeObject(e));
            }
        }

        public void CreateEnrollContact(Order order)
        {
            try
            {
                var company = GetCompany();
                var associateInfo = _distributorService.GetAssociate(order.AssociateId).GetAwaiter().GetResult();
                var settings = _ZiplingoEngagementRepository.GetSettings();
                var UserName = _ZiplingoEngagementRepository.GetUsernameById(Convert.ToString(order.AssociateId));
                (int enrollerID, int sponsorID) = GetEnrollerAndSponsorID(order.AssociateId);
                (Associate sponsorSummary, Associate enrollerSummary) = GetEnrollerAndSponsorSummary(enrollerID, sponsorID);

                var associateSummary = _distributorService.GetAssociate(order.AssociateId);
                var associateOrders = _orderService.GetOrdersByAssociateId(order.AssociateId, "");
                var ZiplingoEngagementRequest = new AssociateContactModel
                {
                    AssociateId = associateInfo.AssociateId,
                    AssociateType = associateInfo.AssociateBaseType,
                    BackOfficeId = associateInfo.BackOfficeId,
                    firstName = associateInfo.DisplayFirstName,
                    lastName = associateInfo.DisplayLastName,
                    address = associateInfo.Address.AddressLine1 + " " + associateInfo.Address.AddressLine2 + " " + associateInfo.Address.AddressLine3,
                    city = associateInfo.Address.City,
                    birthday = associateInfo.BirthDate,
                    OrderDate = order.OrderDate,
                    CountryCode = associateInfo.Address.CountryCode,
                    distributerId = associateInfo.BackOfficeId,
                    phoneNumber = associateInfo.TextNumber,
                    region = associateInfo.Address.CountryCode,
                    state = associateInfo.Address.State,
                    zip = associateInfo.Address.PostalCode,
                    UserName = UserName,
                    WebAlias = UserName,
                    CompanyUrl = company.BackOfficeHomePageURL,
                    CompanyDomain = company.BackOfficeHomePageURL,
                    LanguageCode = associateInfo.LanguageCode,
                    CommissionActive = true,
                    emailAddress = associateInfo.EmailAddress,
                    CompanyName = settings.CompanyName,
                    EnrollerId = enrollerSummary.AssociateId,
                    SponsorId = sponsorSummary.AssociateId,
                    EnrollerName = enrollerSummary.DisplayFirstName + ' ' + enrollerSummary.DisplayLastName,
                    EnrollerMobile = enrollerSummary.PrimaryPhone,
                    EnrollerEmail = enrollerSummary.EmailAddress,
                    SponsorName = sponsorSummary.DisplayFirstName + ' ' + sponsorSummary.DisplayLastName,
                    SponsorMobile = sponsorSummary.PrimaryPhone,
                    SponsorEmail = sponsorSummary.EmailAddress,
                    JoinDate = associateSummary.Result.SignupDate.ToUniversalTime(),
                    ActiveAutoship = associateOrders.Result.Where(o => o.OrderType == OrderType.Autoship).Any()
                };

                var jsonZiplingoEngagementRequest = JsonConvert.SerializeObject(ZiplingoEngagementRequest);
                CallZiplingoEngagementApi(jsonZiplingoEngagementRequest, "Contact/CreateContactV2");
            }
            catch (Exception e)
            {
                _customLogService.SaveLog(order.AssociateId, order.OrderNumber, $"{ClassName}.CreateContact", "Error", e.Message, "", "", $"order : {JsonConvert.SerializeObject(order)}", JsonConvert.SerializeObject(e));
            }
        }

        public void CreateContact(Application req, ApplicationResponse response)
        {
            try
            {
                if (req.AssociateId == 0)
                    req.AssociateId = response.AssociateId;

                if (string.IsNullOrEmpty(req.BackOfficeId))
                    req.BackOfficeId = response.BackOfficeId;

                var company = GetCompany();
                var settings = _ZiplingoEngagementRepository.GetSettings();
                (int enrollerID, int sponsorID) = GetEnrollerAndSponsorID(req.AssociateId);
                (Associate sponsorSummary, Associate enrollerSummary) = GetEnrollerAndSponsorSummary(enrollerID, sponsorID);

                var associateSummary = _distributorService.GetAssociate(req.AssociateId);
                var ZiplingoEngagementRequest = new AssociateContactModel
                {
                    AssociateId = req.AssociateId,
                    AssociateStatus = req.StatusId,
                    AssociateType = req.AssociateBaseType,
                    BackOfficeId = req.BackOfficeId,
                    birthday = req.BirthDate,
                    address = req.ApplicantAddress.AddressLine1 + " " + req.ApplicantAddress.AddressLine2 + " " + req.ApplicantAddress.AddressLine3,
                    city = req.ApplicantAddress.City,
                    CommissionActive = true,
                    CountryCode = req.ApplicantAddress.CountryCode,
                    distributerId = req.BackOfficeId,
                    emailAddress = req.EmailAddress,
                    firstName = req.FirstName,
                    lastName = req.LastName,
                    phoneNumber = req.TextNumber,
                    region = req.ApplicantAddress.CountryCode,
                    state = req.ApplicantAddress.State,
                    zip = req.ApplicantAddress.PostalCode,
                    UserName = req.Username,
                    WebAlias = req.Username,
                    CompanyUrl = company.BackOfficeHomePageURL,
                    CompanyDomain = company.BackOfficeHomePageURL,
                    LanguageCode = req.LanguageCode,
                    CompanyName = settings.CompanyName,
                    EnrollerId = enrollerSummary.AssociateId,
                    SponsorId = sponsorSummary.AssociateId,
                    EnrollerName = enrollerSummary.DisplayFirstName + ' ' + enrollerSummary.DisplayLastName,
                    EnrollerMobile = enrollerSummary.PrimaryPhone,
                    EnrollerEmail = enrollerSummary.EmailAddress,
                    SponsorName = sponsorSummary.DisplayFirstName + ' ' + sponsorSummary.DisplayLastName,
                    SponsorMobile = sponsorSummary.PrimaryPhone,
                    SponsorEmail = sponsorSummary.EmailAddress,
                    JoinDate = associateSummary.Result.SignupDate.ToUniversalTime(),
                    ActiveAutoship = false
                };

                var jsonZiplingoEngagementRequest = JsonConvert.SerializeObject(ZiplingoEngagementRequest);
                CallZiplingoEngagementApi(jsonZiplingoEngagementRequest, "Contact/CreateContactV2");
                ZiplingoEngagementRequest request = new ZiplingoEngagementRequest();
               
                request = new ZiplingoEngagementRequest { associateid = req.AssociateId, companyname = settings.CompanyName, eventKey = "Enrollment", data = jsonZiplingoEngagementRequest };
               
                var jsonReq = JsonConvert.SerializeObject(request);
                CallZiplingoEngagementApi(jsonReq, "Campaign/ExecuteTrigger");
            }
            catch (Exception e)
            {
                _customLogService.SaveLog(req.AssociateId, 0, $"{ClassName}.CreateContact", "Error", e.Message, "", "", $"req : {JsonConvert.SerializeObject(req)}, response : {JsonConvert.SerializeObject(response)}", JsonConvert.SerializeObject(e));
            }
        }

        public void UpdateContact(Associate req)
        {
            try
            {
                var settings = _ZiplingoEngagementRepository.GetSettings();
                var company = GetCompany();
                var UserName = _ZiplingoEngagementRepository.GetUsernameById(Convert.ToString(req.AssociateId));
                var AssociateInfo = _distributorService.GetAssociate(req.AssociateId).GetAwaiter().GetResult();
                (int enrollerID, int sponsorID) = GetEnrollerAndSponsorID(req.AssociateId);
                (Associate sponsorSummary, Associate enrollerSummary) = GetEnrollerAndSponsorSummary(enrollerID, sponsorID);

                var associateSummary = _distributorService.GetAssociate(req.AssociateId);
                var associateOrders = _orderService.GetOrdersByAssociateId(req.AssociateId, "");

                var ZiplingoEngagementRequest = new AssociateContactModel
                {
                    AssociateId = AssociateInfo.AssociateId,
                    AssociateType = AssociateInfo.AssociateBaseType,
                    AssociateStatus = AssociateInfo.StatusId,
                    BackOfficeId = AssociateInfo.BackOfficeId,
                    birthday = AssociateInfo.BirthDate,
                    address = AssociateInfo.Address.AddressLine1 + " " + AssociateInfo.Address.AddressLine2 + " " + AssociateInfo.Address.AddressLine3,
                    city = AssociateInfo.Address.City,
                    CommissionActive = true,
                    CountryCode = AssociateInfo.Address.CountryCode,
                    distributerId = AssociateInfo.BackOfficeId,
                    emailAddress = AssociateInfo.EmailAddress,
                    firstName = AssociateInfo.DisplayFirstName,
                    lastName = AssociateInfo.DisplayLastName,
                    phoneNumber = AssociateInfo.TextNumber,
                    region = AssociateInfo.Address.CountryCode,
                    state = AssociateInfo.Address.State,
                    zip = AssociateInfo.Address.PostalCode,
                    LanguageCode = AssociateInfo.LanguageCode,
                    UserName = UserName,
                    WebAlias = UserName,
                    CompanyUrl = company.BackOfficeHomePageURL,
                    CompanyDomain = company.BackOfficeHomePageURL,
                    CompanyName = settings.CompanyName,
                    EnrollerId = enrollerSummary.AssociateId,
                    SponsorId = sponsorSummary.AssociateId,
                    EnrollerName = enrollerSummary.DisplayFirstName + ' ' + enrollerSummary.DisplayLastName,
                    EnrollerMobile = enrollerSummary.PrimaryPhone,
                    EnrollerEmail = enrollerSummary.EmailAddress,
                    SponsorName = sponsorSummary.DisplayFirstName + ' ' + sponsorSummary.DisplayLastName,
                    SponsorMobile = sponsorSummary.PrimaryPhone,
                    SponsorEmail = sponsorSummary.EmailAddress,
                    JoinDate = associateSummary.Result.SignupDate.ToUniversalTime(),
                    ActiveAutoship = associateOrders.Result.Where(o => o.OrderType == OrderType.Autoship).Any()
                };

                var jsonReq = JsonConvert.SerializeObject(ZiplingoEngagementRequest);
                CallZiplingoEngagementApi(jsonReq, "Contact/CreateContactV2");
            }
            catch (Exception e)
            {
                _customLogService.SaveLog(req.AssociateId, 0, $"{ClassName}.UpdateContact", "Error", e.Message, "", "", $"req : {JsonConvert.SerializeObject(req)}", JsonConvert.SerializeObject(e));
            }
        }


        public HttpResponseMessage CallZiplingoEngagementApi(string jsonData, string apiMethod)
        {
            try
            {
                var settings = _ZiplingoEngagementRepository.GetSettings();

                //httpClient call
                var apiUrl = settings.ApiUrl + apiMethod;
                HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("POST"), new Uri(apiUrl));
                request.Content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var data = _httpClientService.PostRequestByUsername(request, settings.Username, settings.Password);

                return data;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void ResetSettings(CommandRequest commandRequest)
        {
            try
            {
                _ZiplingoEngagementRepository.ResetSettings();
            }
            catch (Exception ex)
            {
                _customLogService.SaveLog(0, 0, $"{ClassName}.ResetZiplingoSettings", "Error", ex.Message, "", "", $"commandRequest : {JsonConvert.SerializeObject(commandRequest)}", JsonConvert.SerializeObject(ex));
            }
        }

        public void SendOrderShippedEmail(int packageId, string trackingNumber)
        {
            var orderModel = new OrderDetailModel();
            var shipInfo = _ZiplingoEngagementRepository.GetOrderNumber(packageId);
            orderModel.TrackingNumber = trackingNumber;
            orderModel.Carrier = shipInfo.Carrier;
            orderModel.ShipMethodId = shipInfo.ShipMethodId;
            orderModel.DateShipped = shipInfo.DateShipped;
            orderModel.Order = _orderService.GetOrderByOrderNumber(shipInfo.OrderNumber).GetAwaiter().GetResult();
            if (orderModel.Order.OrderType == OrderType.Autoship)
            {
                var autoShipInfo = _ZiplingoEngagementRepository.GetAutoshipFromOrder(shipInfo.OrderNumber);
                orderModel.AutoshipId = autoShipInfo.AutoshipId;
                CallOrderZiplingoEngagementTriggerForShipped(orderModel, "AutoOrderShipped");
            }
            if (orderModel.Order.OrderType == OrderType.Standard)
            {
                CallOrderZiplingoEngagementTriggerForShipped(orderModel, "OrderShipped");
            }
        }

        public void AssociateBirthDateTrigger()
        {
            var settings = _ZiplingoEngagementRepository.GetSettings();
            if (settings.AllowBirthday)
            {
                var associateInfo = _ZiplingoEngagementRepository.AssociateBirthdayWishesInfo();
                if (associateInfo == null) return;

                foreach (var assoInfo in associateInfo)
                {
                    AssociateInfo asso = new AssociateInfo();
                    asso.AssociateId = assoInfo.AssociateId;
                    asso.Birthdate = assoInfo.Birthdate;
                    asso.EmailAddress = assoInfo.EmailAddress;
                    asso.FirstName = assoInfo.FirstName;
                    asso.LastName = assoInfo.LastName;
                    CallOrderZiplingoEngagementTriggerForBirthDayWishes(asso, "AssociateBirthdayWishes");
                }
            }
        }

        public void AssociateWorkAnniversaryTrigger()
        {
            var settings = _ZiplingoEngagementRepository.GetSettings();
            if (settings.AllowAnniversary)
            {
                var associateInfo = _ZiplingoEngagementRepository.AssociateWorkAnniversaryInfo();
                if (associateInfo == null) return;

                foreach (var assoInfo in associateInfo)
                {
                    AssociateInfo asso = new AssociateInfo();
                    asso.AssociateId = assoInfo.AssociateId;
                    asso.SignupDate = assoInfo.SignupDate;
                    asso.EmailAddress = assoInfo.EmailAddress;
                    asso.FirstName = assoInfo.FirstName;
                    asso.LastName = assoInfo.LastName;
                    asso.TotalWorkingYears = assoInfo.TotalWorkingYears;
                    CallOrderZiplingoEngagementTriggerForWorkAnniversary(asso, "AssociateWorkAnniversary");
                }
            }
        }

        public EmailOnNotificationEvent OnNotificationEvent(NotificationEvent notification)
        {
            if((int)notification.EventType == 1)
            {
               return CallRankAdvancementEvent(notification.EventValue);
            }
            return null;
        }
        public LogRealtimeRankAdvanceHookResponse LogRealtimeRankAdvanceEvent(LogRealtimeRankAdvanceHookRequest req)
        {
            return LogRankAdvancement(req);
        }

        public LogRealtimeRankAdvanceHookResponse LogRankAdvancement(LogRealtimeRankAdvanceHookRequest req)
        {
            try
            {
                AssociateRankAdvancement obj = new AssociateRankAdvancement();
                var settings = _ZiplingoEngagementRepository.GetSettings();
                var rankName = _rankService.GetRankName(req.NewRank).GetAwaiter().GetResult();
                var associateInfo = _distributorService.GetAssociate(req.AssociateId).GetAwaiter().GetResult();
                if (settings.AllowRankAdvancement)
                {
                    obj.Rank = req.NewRank;
                    obj.RankName = rankName;
                    obj.AssociateId = req.AssociateId;
                    obj.FirstName = associateInfo.DisplayFirstName;
                    obj.LastName = associateInfo.DisplayLastName;
                    CallOrderZiplingoEngagementTriggerForAssociateRankAdvancement(obj, "RankAdvancement");
                }
                return null;
            }
            catch (Exception ex)
            {
                _customLogService.SaveLog(0, 0, $"{ClassName}.LogRankAdvancement", "Error", ex.Message, "", "", $"req : {JsonConvert.SerializeObject(req)}", JsonConvert.SerializeObject(ex));
            }
            return null;
        }
        public EmailOnNotificationEvent CallRankAdvancementEvent(NotificationEvent notification)
        {
            try
            {
                var settings = _ZiplingoEngagementRepository.GetSettings();
                if (settings.AllowRankAdvancement)
                {
                    AssociateRankAdvancement obj = new AssociateRankAdvancement();
                    string str = string.Empty;
                    var rank = 0;
                    var rankName = string.Empty;
                    try
                    {
                        if (!String.IsNullOrEmpty(Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(notification.EventValue)))
                        {
                            str = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(notification.EventValue);
                            RankObj objRank = JsonConvert.DeserializeObject<RankObj>(str);
                             rank = objRank.Rank;
                             rankName = _rankService.GetRankName(rank).GetAwaiter().GetResult();
                        }
                    }
                    catch (Exception ex) {
                        str = ex.Message;
                    }
                    var distribuneuminfo = _distributorService.GetAssociate(notification.AssociateId).GetAwaiter().GetResult();
                    obj.Rank = rank;
                    obj.RankName = rankName;
                    obj.AssociateId = notification.AssociateId;
                    obj.FirstName = distribuneuminfo.DisplayFirstName;
                    obj.LastName = distribuneuminfo.DisplayLastName;
                    CallOrderZiplingoEngagementTriggerForAssociateRankAdvancement(obj, "RankAdvancement");
                }
                return null;
            }
            catch (Exception ex)
            {
                _customLogService.SaveLog(0, 0, $"{ClassName}.CallRankAdvancementEvent", "Error", ex.Message, "", "", $"req : {JsonConvert.SerializeObject(notification)}", JsonConvert.SerializeObject(ex));
            }
            return null;
        }

        private class RankObj
        {
            public int Rank { get; set; }
        }

        public void ExpirationCardTrigger(List<CardInfo> cardinfo)
        {
            try
            {
                var company = GetCompany();
                var settings = _ZiplingoEngagementRepository.GetSettings();

                foreach (CardInfo info in cardinfo)
                {
                    try
                    {
                        AssociateCardInfoModel assoObj = new AssociateCardInfoModel();
                        assoObj.FirstName = info.FirstName;
                        assoObj.LastName = info.LastName;
                        assoObj.PrimaryPhone = info.PrimaryPhone;
                        assoObj.Email = info.PrimaryPhone;
                        assoObj.CardDate = info.ExpirationDate;
                        assoObj.CardLast4Degit = info.Last4DegitOfCard;
                        assoObj.CompanyDomain = company.BackOfficeHomePageURL;
                        assoObj.LogoUrl = settings.LogoUrl;
                        assoObj.CompanyName = settings.CompanyName;

                        var strData = JsonConvert.SerializeObject(assoObj);
                        ZiplingoEngagementRequest request = new ZiplingoEngagementRequest { associateid = info.AssociateId, companyname = settings.CompanyName, eventKey = "UpcommingExpiryCard", data = strData };
                        var jsonReq = JsonConvert.SerializeObject(request);
                        CallZiplingoEngagementApi(jsonReq, "Campaign/ExecuteTrigger");
                    }
                    catch (Exception ex)
                    {
                        _customLogService.SaveLog(info.AssociateId, 0, $"{ClassName}.ExpirationCardTrigger", "Error", ex.Message, "", "", $"info : {info}", JsonConvert.SerializeObject(ex));
                    }
                }

            }
            catch (Exception e)
            {
                _customLogService.SaveLog(0, 0, $"{ClassName}.5DayTrigger", "Error", e.Message, "", "", $"cardinfo : {JsonConvert.SerializeObject(cardinfo)}", JsonConvert.SerializeObject(e));
            }
        }
        public void AssociateStatusChangeTrigger(int associateId, int oldStatusId, int newStatusId)
        {
            try
            {
                AssociateStatusChange obj = new AssociateStatusChange();
                var distributorInfo = _distributorService.GetAssociate(associateId).GetAwaiter().GetResult();
                obj.OldStatusId = oldStatusId;
                obj.OldStatus = _ZiplingoEngagementRepository.GetStatusById(oldStatusId);
                obj.NewStatusId = newStatusId;
                obj.NewStatus = _ZiplingoEngagementRepository.GetStatusById(newStatusId);
                obj.AssociateId = associateId;
                obj.FirstName = distributorInfo.DisplayFirstName;
                obj.LastName = distributorInfo.DisplayLastName;
                obj.EmailAddress = distributorInfo.EmailAddress;
                CallOrderZiplingoEngagementTriggerForAssociateChangeStatus(obj, "ChangeAssociateStatus");
            }
            catch (Exception ex)
            {
                _customLogService.SaveLog(0, 0, $"{ClassName}.AssociateStatusChangeTrigger", "Error", ex.Message, "", "", $"associateId : {associateId}, oldStatusId : {oldStatusId}, newStatusId : {newStatusId}", JsonConvert.SerializeObject(ex));
            }
        }
        public void CallOrderZiplingoEngagementTriggerForAssociateChangeStatus(AssociateStatusChange assoStatusChangeInfo, string eventKey)
        {
            try
            {
                var company = GetCompany();
                var settings = _ZiplingoEngagementRepository.GetSettings();
                var UserName = _ZiplingoEngagementRepository.GetUsernameById(Convert.ToString(assoStatusChangeInfo.AssociateId));
                (int enrollerID, int sponsorID) = GetEnrollerAndSponsorID(assoStatusChangeInfo.AssociateId);
                (Associate sponsorSummary, Associate enrollerSummary) = GetEnrollerAndSponsorSummary(enrollerID, sponsorID);

                AssociateStatusChange data = new AssociateStatusChange
                {
                    OldStatusId = assoStatusChangeInfo.OldStatusId,
                    OldStatus = assoStatusChangeInfo.OldStatus,
                    NewStatusId = assoStatusChangeInfo.NewStatusId,
                    NewStatus = assoStatusChangeInfo.NewStatus,
                    AssociateId = assoStatusChangeInfo.AssociateId,
                    FirstName = assoStatusChangeInfo.FirstName,
                    LastName = assoStatusChangeInfo.LastName,
                    CompanyDomain = company.BackOfficeHomePageURL,
                    LogoUrl = settings.LogoUrl,
                    CompanyName = settings.CompanyName,
                    EnrollerId = enrollerSummary.AssociateId,
                    SponsorId = sponsorSummary.AssociateId,
                    EnrollerName = enrollerSummary.DisplayFirstName + ' ' + enrollerSummary.DisplayLastName,
                    EnrollerMobile = enrollerSummary.PrimaryPhone,
                    EnrollerEmail = enrollerSummary.EmailAddress,
                    SponsorName = sponsorSummary.DisplayFirstName + ' ' + sponsorSummary.DisplayLastName,
                    SponsorMobile = sponsorSummary.PrimaryPhone,
                    SponsorEmail = sponsorSummary.EmailAddress,
                    EmailAddress = assoStatusChangeInfo.EmailAddress,
                    WebAlias = UserName
                };
                var strData = JsonConvert.SerializeObject(data);
                ZiplingoEngagementRequest request = new ZiplingoEngagementRequest { associateid = assoStatusChangeInfo.AssociateId, companyname = settings.CompanyName, eventKey = eventKey, data = strData, associateStatus = assoStatusChangeInfo.NewStatusId };
                var jsonReq = JsonConvert.SerializeObject(request);
                CallZiplingoEngagementApi(jsonReq, "Campaign/ChangeAssociateStatus");
            }
            catch (Exception e)
            {
                _customLogService.SaveLog(assoStatusChangeInfo.AssociateId, 0, $"{ClassName}.CallOrderZiplingoEngagementTriggerForAssociateChangeStatus", "Error", e.Message, "", "", $"eventKey : {eventKey}, assoStatusChangeInfo : {JsonConvert.SerializeObject(assoStatusChangeInfo)}", JsonConvert.SerializeObject(e));
            }
        }

        public void ExecuteCommissionEarned()
        {
            try
            {
                var settings = _ZiplingoEngagementRepository.GetSettings();

                var payments = _paymentProcessDataService.FindPaidPayments(DateTime.Now.Date.AddDays(-1), DateTime.Now.Date, "").GetAwaiter().GetResult();

                foreach (var payment in payments)
                {
                    var jsonZiplingoEngagementRequest = JsonConvert.SerializeObject(payment);

                    ZiplingoEngagementRequest request = new ZiplingoEngagementRequest();

                    request = new ZiplingoEngagementRequest { associateid = payment.AssociateId, companyname = settings.CompanyName, eventKey = "CommissionEarned", data = jsonZiplingoEngagementRequest };

                    var jsonReq = JsonConvert.SerializeObject(request);

                    CallZiplingoEngagementApi(jsonReq, "Campaign/ExecuteTrigger");
                }
            }
            catch (Exception e)
            {
                _customLogService.SaveLog(0, 0, $"{ClassName}.ExecuteCommissionEarned", "Error", e.Message, "", "", $"", JsonConvert.SerializeObject(e));
            }
        }

        public string GetExecuteCommissionEarned()
        {
            string responseJson = "";
            try
            {
                var settings = _ZiplingoEngagementRepository.GetSettings();
                var payments = _paymentProcessDataService.FindPaidPayments(DateTime.Now.Date.AddDays(-1), DateTime.Now.Date, "").GetAwaiter().GetResult();
                foreach (var payment in payments)
                {
                    var jsonZiplingoEngagementRequest = JsonConvert.SerializeObject(payment);

                    ZiplingoEngagementRequest request = new ZiplingoEngagementRequest();

                    request = new ZiplingoEngagementRequest { associateid = payment.AssociateId, companyname = settings.CompanyName, eventKey = "CommissionEarned", data = jsonZiplingoEngagementRequest };

                    var jsonReq = JsonConvert.SerializeObject(request);

                    CallZiplingoEngagementApi(jsonReq, "Campaign/ExecuteTrigger");

                    responseJson += jsonReq;
                }
            }
            catch (Exception e)
            {
                responseJson = e.Message;
                _customLogService.SaveLog(0, 0, $"{ClassName}.GetExecuteCommissionEarned", "Error", e.Message, "", "", $"", JsonConvert.SerializeObject(e));
            }
            return responseJson;
        }

        public void CreateAutoshipTrigger(Autoship autoshipInfo)
        {
            try
            {
                var company = GetCompany();
                var settings = _ZiplingoEngagementRepository.GetSettings();

                AutoshipInfoMap req = new AutoshipInfoMap();

                req.AssociateId = autoshipInfo.AssociateId;
                req.AutoshipId = autoshipInfo.AutoshipId;
                req.AutoshipType = autoshipInfo.AutoshipType.ToString();
                req.CurrencyCode = autoshipInfo.CurrencyCode;
                req.Custom = autoshipInfo.Custom;
                req.Frequency = autoshipInfo.Frequency.ToString();
                req.FrequencyString = autoshipInfo.FrequencyString;
                req.LastChargeAmount = autoshipInfo.LastChargeAmount;
                req.LastProcessDate = autoshipInfo.LastProcessDate;
                req.LineItems = autoshipInfo.LineItems;
                req.NextProcessDate = autoshipInfo.NextProcessDate;
                req.PaymentMerchantId = autoshipInfo.PaymentMerchantId;
                req.PaymentMethodId = autoshipInfo.PaymentMethodId;
                req.ShipAddress = autoshipInfo.ShipAddress;
                req.ShipMethodId = autoshipInfo.ShipMethodId;
                req.StartDate = autoshipInfo.StartDate;
                req.Status = autoshipInfo.Status;
                req.SubTotal = autoshipInfo.SubTotal;
                req.TotalCV = autoshipInfo.TotalCV;
                req.TotalQV = autoshipInfo.TotalQV;

                var strData = JsonConvert.SerializeObject(req);
                ZiplingoEngagementRequest request = new ZiplingoEngagementRequest { associateid = autoshipInfo.AssociateId, companyname = settings.CompanyName, eventKey = "CreateAutoship", data = strData };
                var jsonReq = JsonConvert.SerializeObject(request);
                CallZiplingoEngagementApi(jsonReq, "Campaign/ExecuteTrigger");
            }
            catch (Exception e)
            {
                _customLogService.SaveLog(autoshipInfo.AssociateId, 0, $"{ClassName}.CreateAutoshipTrigger", "Error", e.Message, "", "", $"{JsonConvert.SerializeObject(autoshipInfo)}", JsonConvert.SerializeObject(e));
            }
        }

        public void UpdateAssociateType(int associateId, string oldAssociateType, string newAssociateType, int newAssociateTypeId)
        {
            try
            {
                var company = GetCompany();
                var associateTypeModel = new AssociateTypeModel();
                var settings = _ZiplingoEngagementRepository.GetSettings();
                var associateSummary = _distributorService.GetAssociate(associateId).GetAwaiter().GetResult();
                associateTypeModel.AssociateId = associateId;
                associateTypeModel.FirstName = associateSummary.DisplayFirstName;
                associateTypeModel.LastName = associateSummary.DisplayLastName;
                associateTypeModel.Email = associateSummary.EmailAddress;
                associateTypeModel.Phone = (associateSummary.TextNumber == "" || associateSummary.TextNumber == null)
                    ? associateSummary.PrimaryPhone
                    : associateSummary.TextNumber;
                associateTypeModel.OldAssociateBaseType = oldAssociateType;
                associateTypeModel.NewAssociateBaseType = newAssociateType;
                associateTypeModel.CompanyDomain = company.BackOfficeHomePageURL;
                associateTypeModel.LogoUrl = settings.LogoUrl;
                associateTypeModel.CompanyName = settings.CompanyName;

                var strData = JsonConvert.SerializeObject(associateTypeModel);

                AssociateTypeChange request = new AssociateTypeChange
                {
                    associateTypeId = newAssociateTypeId,
                    associateid = associateId,
                    companyname = settings.CompanyName,
                    eventKey = "AssociateTypeChange",
                    data = strData
                };
                var jsonReq = JsonConvert.SerializeObject(request);
                CallZiplingoEngagementApi(jsonReq, "Campaign/ChangeAssociateType");

            }
            catch (Exception e)
            {
                _customLogService.SaveLog(associateId, 0, $"{ClassName}.UpdateAssociateType", "Error", e.Message, "", "", $"associateId : {associateId}, oldAssociateType : {oldAssociateType}, newAssociateType : {newAssociateType}, newAssociateTypeId : {newAssociateTypeId}", JsonConvert.SerializeObject(e));
            }
        }

    }
}
