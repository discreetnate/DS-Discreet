using System;
using WebExtension.ThirdParty.Interfaces;
using WebExtension.ThirdParty.ZiplingoEngagement.Model;
using System.Collections.Generic;
using Dapper;
using DirectScale.Disco.Extension.Services;
using System.Linq;

namespace WebExtension.ThirdParty
{
    public class ZiplingoEngagementRepository : IZiplingoEngagementRepository
    {
        private readonly IDataService _dataService;

        public ZiplingoEngagementRepository(
            IDataService dataService
            )
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        }

        public ZiplingoEngagementSettings GetSettings()
        {
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().GetAwaiter().GetResult()))
            {                
                var settingsQuery = "SELECT * FROM Client.ZiplingoEngagementSettings";

                return dbConnection.QueryFirstOrDefault<ZiplingoEngagementSettings>(settingsQuery);
            }
        }

        public void UpdateSettings(ZiplingoEngagementSettingsRequest settings)
        {
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().GetAwaiter().GetResult()))
            {
                var parameters = new
                {
                    settings.ApiUrl,
                    settings.Username,
                    settings.Password,
                    settings.LogoUrl,
                    settings.CompanyName,
                    settings.AllowBirthday,
                    settings.AllowAnniversary,
                    settings.AllowRankAdvancement
                };

                var updateStatement = @"UPDATE Client.ZiplingoEngagementSettings SET ApiUrl = @ApiUrl,  Username = @Username, Password = @Password, LogoUrl = @LogoUrl, CompanyName = @CompanyName, AllowBirthday = @AllowBirthday, AllowAnniversary = @AllowAnniversary, AllowRankAdvancement = @AllowRankAdvancement";
                dbConnection.Execute(updateStatement, parameters);
            }
            
        }

        public void ResetSettings()
        {
            try
            {
                using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().GetAwaiter().GetResult()))
                {
                    var settings = GetSettings();
                    var parameters = new
                    {
                        Username = "NeumiAPI",
                        Password = "da7c4c2a-bf87-441e-b69e-6afd1bea261d",
                        ApiUrl = "http://unifiedbetaapi.ziplingo.com/api/",
                        LogoUrl = "https://az708237.vo.msecnd.net/neumi/images/376843ac-31be-42e7-9f7e-a072056b572e",
                        settings.CompanyName
                    };

                    var query = @"
                        MERGE INTO Client.ZiplingoEngagementSettings WITH (HOLDLOCK) AS TARGET 
                        USING 
                            (SELECT @Username AS 'Username', @Password AS 'Password', @ApiUrl AS 'ApiUrl', @LogoUrl AS 'LogoUrl', @CompanyName AS 'CompanyName'
                        ) AS SOURCE 
                            ON SOURCE.CompanyName = TARGET.CompanyName
                        WHEN MATCHED THEN 
                            UPDATE SET TARGET.Username = SOURCE.Username, TARGET.Password = SOURCE.Password, TARGET.ApiUrl = SOURCE.ApiUrl, TARGET.LogoUrl = SOURCE.LogoUrl
                        WHEN NOT MATCHED BY TARGET THEN 
                            INSERT (Username, [Password], ApiUrl, LogoUrl,CompanyName) 
					        VALUES (SOURCE.Username, SOURCE.Password, SOURCE.ApiUrl, SOURCE.LogoUrl, SOURCE.CompanyName);
                    ";

                    dbConnection.Execute(query, parameters);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public ShipInfo GetOrderNumber(int packageId)
        {
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().GetAwaiter().GetResult()))
            {
                ShipInfo info = new ShipInfo();
                var query = $"SELECT o.recordnumber as OrderNumber,p.ShipMethodId,p.Carrier,p.DateShipped FROM ORD_OrderPackages p JOIN ORD_Order o ON p.OrderNumber = o.recordnumber WHERE p.recordnumber ='{packageId}'";
                using (var reader = dbConnection.ExecuteReader(query))
                {
                    if (reader.Read())
                    {
                        info.OrderNumber = (Int32)reader["OrderNumber"];
                        info.ShipMethodId = (Int32)reader["ShipMethodId"];
                        info.Carrier = Convert.ToString(reader["Carrier"]);
                        info.DateShipped = Convert.ToString(reader["DateShipped"]);
                    }
                }
                return info;
            }
        }
        public List<AssociateInfo> AssociateBirthdayWishesInfo()
        {
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().GetAwaiter().GetResult()))
            {
                List<AssociateInfo> objAsso = new List<AssociateInfo>();
                AssociateInfo info = new AssociateInfo();
                var query = $"SELECT Birthdate, recordnumber,EmailAddress,FirstName,LastName FROM crm_distributors WHERE datepart(dd, Birthdate) = datepart(dd, GETDATE()) AND datepart(mm, Birthdate) = datepart(mm, GETDATE()) AND Void = 0 AND StatusId<>5";

                using (var reader = dbConnection.ExecuteReader(query))
                {
                    while (reader.Read())
                    {
                        info.AssociateId = (Int32)reader["recordnumber"];
                        info.FirstName = Convert.ToString(reader["FirstName"]);
                        info.LastName = Convert.ToString(reader["LastName"]);
                        info.Birthdate = Convert.ToString(reader["Birthdate"]);
                        info.EmailAddress = Convert.ToString(reader["EmailAddress"]);
                        objAsso.Add(info);
                    }
                }
                return objAsso;
            }
        }

        public List<AssociateInfo> AssociateWorkAnniversaryInfo()
        {
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().GetAwaiter().GetResult()))
            {
                List<AssociateInfo> objAsso = new List<AssociateInfo>();
                AssociateInfo info = new AssociateInfo();
                var query = $"SELECT SignupDate, recordnumber,EmailAddress,FirstName,LastName,DATEDIFF (yy, SignupDate, GETDATE()) AS 'TotalWorkingYears' FROM crm_distributors WHERE datepart(dd, SignupDate) = datepart(dd, GETDATE()) AND datepart(mm, SignupDate) = datepart(mm, GETDATE()) AND Void = 0 AND StatusId<>5";
                using (var reader = dbConnection.ExecuteReader(query))
                {
                    while (reader.Read())
                    {
                        info.AssociateId = (Int32)reader["recordnumber"];
                        info.FirstName = Convert.ToString(reader["FirstName"]);
                        info.LastName = Convert.ToString(reader["LastName"]);
                        info.SignupDate = Convert.ToString(reader["SignupDate"]);
                        info.TotalWorkingYears = (Int32)reader["TotalWorkingYears"];
                        info.EmailAddress = Convert.ToString(reader["EmailAddress"]);
                        objAsso.Add(info);
                    }
                }
                return objAsso;
            }
        }

        public AutoshipFromOrderInfo GetAutoshipFromOrder(int orderNumber)
        {
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().GetAwaiter().GetResult()))
            {
                var qry = @"SELECT al.AutoShipId, a.LastProcessDate, a.NextProcessDate, a.Frequency
                FROM CRM_AutoShipLog al
                JOIN CRM_AutoShip a
                    ON al.AutoShipId = a.recordnumber
                WHERE al.OrderId = @orderNumber";

                return dbConnection.QueryFirstOrDefault<AutoshipFromOrderInfo>(qry, new { orderNumber });
            }
        }

        public string GetUsernameById(string associateId)
        {
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().GetAwaiter().GetResult()))
            {
                var parameters = new
                {
                    BackOfficeId = new Dapper.DbString { IsAnsi = true, Length = 25, Value = associateId }
                };
                var query = @"SELECT Username FROM Users WHERE BackOfficeId = @BackofficeId";

                return dbConnection.QueryFirstOrDefault<string>(query, parameters);
            }
        }
        public string GetLastFoutDegitByOrderNumber(int orderId)
        {
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().GetAwaiter().GetResult()))
            {
                var parameter = new { orderId };
                var query = @"SELECT Number FROM ORD_PaymentGatewayTransactions WHERE OrderNumber = @orderId";

                var result = dbConnection.QueryFirstOrDefault<string>(query, parameter);
                return result;
            }
        }
        public string GetStatusById(int statusId)
        {
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().GetAwaiter().GetResult()))
            {
                var parameter = new { statusId };
                var query = @"SELECT StatusName FROM CRM_AssociateStatuses WHERE recordnumber = @statusId";

                var result = dbConnection.QueryFirstOrDefault<string>(query, parameter);
                return result;
            }
        }

        public List<ShippingTrackingInfo> GetShippingTrackingInfo()
        {
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().GetAwaiter().GetResult()))
            {
                var query = $"SELECT ShippingUrl,TrackPattern FROM client.ShippingTrackingUrl";
                var result = dbConnection.Query<ShippingTrackingInfo>(query).ToList();
                return result;
            }
        }
    }
}
