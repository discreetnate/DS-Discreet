using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WebExtension.Helper;
using WebExtension.Model;
using WebExtension.Models;

namespace WebExtension.Services
{
    public interface INomadEwalletService
    {
        Task<NomadEwalletAPIReturn> GetNomadEwalletAccountBalance(GetNomadEwalletAccountBalanceRequest request);
        Task<string> SingleSignOn(SingleSignOnRequest request);
    }
    public class NomadEwalletService : INomadEwalletService
    {
        private readonly ICustomLogService _customLogService;
        private readonly IHttpClientService _httpClientService;
        private readonly IAssociateService _associateService;
        private const string NomadEwalletBaseUrlSandbox = "https://api.nomadewallet.dev/";
        private const string NomadEwalletBaseUrlLive = "https://api.nomadewallet.com/";
        private const string NomadEwalletUrlLive = "https://ewallet.discreet.biz/";
        private const string NomadEwalletUrlStage = "https://ewallet.discreet.dev/";
        private const string SandboxUsername = "";
        private const string LiveUsername = "DiscreetAPI";
        private const string SandboxPassword = "";
        private const string LivePassword = "YYYAzTj;ce2Br0(";
        private readonly string tokenType = "authorization";
        private const string AuthMethod = "api/Authenticate/login";
        private const string BalanceMethod = "api/AccountData/DirectScaleAccountBalance";
        private static DirectScale.Disco.Extension.EnvironmentType _envType;
        public NomadEwalletService(ICustomLogService customLogService, IHttpClientService httpClientService, IAssociateService associateService)
        {
            _customLogService = customLogService ?? throw new ArgumentNullException(nameof(customLogService));
            _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
            _associateService = associateService ?? throw new ArgumentNullException(nameof(associateService));
        }


        public async Task<NomadEwalletAPIReturn> GetNomadEwalletAccountBalance(GetNomadEwalletAccountBalanceRequest request)
        {
            var associate = await _associateService.GetAssociate(request.associateId);
            string methodName = CommonMethod.GetCurrentMethodName(MethodBase.GetCurrentMethod());
            var apiUrl = GetBaseUrl() + BalanceMethod;
            var jsonReq = JsonConvert.SerializeObject(new { emailAddress = associate.EmailAddress });
            string token = await GetAuthToken();
            HttpResponseMessage output = POST_Nomand("0", methodName, apiUrl, jsonReq, $"Bearer {token}");
            var result = await output.Content.ReadAsStringAsync();
            NomadEwalletAPIReturn responseData = JsonConvert.DeserializeObject<NomadEwalletAPIReturn>(result);
            return responseData;
        }

        public async Task<string> SingleSignOn(SingleSignOnRequest request)
        {
            var associate = await _associateService.GetAssociate(request.associateId);
            string methodName = CommonMethod.GetCurrentMethodName(MethodBase.GetCurrentMethod());
            var apiUrl = GetBaseUrl() + BalanceMethod;
            var jsonReq = JsonConvert.SerializeObject(new { emailAddress = associate.EmailAddress });
            string token = await GetAuthToken();
            var a1 = NomadEwalletBaseUrlLive + "api/Admin/Subpateo?emailAddress=" + associate.EmailAddress;
            HttpResponseMessage output = POST_Nomand("0", methodName, a1, jsonReq, $"Bearer {token}");
            var result = await output.Content.ReadAsStringAsync();
            NomadEwalletAPIReturn responseData = JsonConvert.DeserializeObject<NomadEwalletAPIReturn>(result);
            var signonUrl = GetNomadEwalletUrl() + "Identity/Account/LoginA?emailAddress=" + associate.EmailAddress + "&LoginCode=" + responseData.returnID;
            return signonUrl;
        }


        internal async Task<string> GetAuthToken()
        {
            string methodName = CommonMethod.GetCurrentMethodName(MethodBase.GetCurrentMethod());
            var apiUrl = GetBaseUrl() + AuthMethod;
            var jsonReq = JsonConvert.SerializeObject(new { username = GetUsername(), password = GetPassword() });
            HttpResponseMessage output = POST_Nomand("0", methodName, apiUrl, jsonReq, "");
            if (output.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var result = await output.Content.ReadAsStringAsync();
                GetNomadEwalletTokenResponse responseData = JsonConvert.DeserializeObject<GetNomadEwalletTokenResponse>(result);
                return responseData.token;
            }
            else
            {
                throw new Exception(output.StatusCode.ToString());
            }
        }
        internal HttpResponseMessage POST_Nomand(string AssociateId, string title, string apiUrl, string jsonReq, string token)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("POST"), new Uri(apiUrl));
                request.Content = new StringContent(jsonReq, Encoding.UTF8, "application/json");
                HttpResponseMessage output = new HttpResponseMessage();
                if (string.IsNullOrEmpty(token))
                    output = _httpClientService.MakeRequest(request);
                else
                    output = _httpClientService.MakeRequestByToken(request, tokenType, token);
                _customLogService.SaveLog(int.Parse(AssociateId), 0, title, output.StatusCode == System.Net.HttpStatusCode.OK ? "Success" : "Not Success", "", apiUrl, $"{tokenType} : {token}", jsonReq, output.Content.ReadAsStringAsync().Result);
                return output;
            }
            catch (Exception ex)
            {
                _customLogService.SaveLog(int.Parse(AssociateId), 0, title, $"Error", ex.Message, apiUrl, $"{tokenType} : {token}", jsonReq, JsonConvert.SerializeObject(ex));
                throw;
            }
        }

        private static string GetBaseUrl()
        {
            if (_envType == EnvironmentType.Sandbox)
                return NomadEwalletBaseUrlSandbox;
            else
                return NomadEwalletBaseUrlLive;
        }
        private static string GetUsername()
        {
            if (_envType == EnvironmentType.Sandbox)
                return SandboxUsername;
            else
                return LiveUsername;
        }

        private static string GetPassword()
        {
            if (_envType == EnvironmentType.Sandbox)
                return SandboxPassword;
            else
                return LivePassword;
        }
        private static string GetNomadEwalletUrl()
        {
            if (_envType == EnvironmentType.Sandbox)
                return NomadEwalletUrlStage;
            else
                return NomadEwalletUrlLive;
        }
    }
}
