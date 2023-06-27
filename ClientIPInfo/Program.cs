using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;

namespace ClientIPInfo
{
    public class IPQS
    {
        private string key = ConfigurationManager.AppSettings["APIKey"];

        public async Task<Dictionary<string, object>> ProxyVpnApi(string ip)
        {
            string url = $"https://www.ipqualityscore.com/api/json/ip/{key}/{ip}";

            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                string json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            }
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                IPQS IPInfo = new IPQS();
                DataTable IP = new DataTable();
                SqlConnection connB2B = new SqlConnection(ConfigurationManager.ConnectionStrings["B2BConnectionString"].ConnectionString);
                await connB2B.OpenAsync();

                SqlCommand command = new SqlCommand("SELECT DISTINCT [R_LG_IP] as IP FROM [nowe_b2b].[ldd].[Rptlogowanie] WITH (NOLOCK) where R_LG_IP not in (select IPAdress from [serwer-sql1].[CDNXL_TESTOWA_2014].[dbo].[GaskaClientIPInfo]) and R_LG_Data > Getdate() -60", connB2B);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    IP.Load(reader);
                }
                connB2B.Close();

                SqlConnection connGaska = new SqlConnection(ConfigurationManager.ConnectionStrings["GaskaConnectionString"].ConnectionString);
                await connGaska.OpenAsync();


                foreach (DataRow row in IP.Rows)
                {
                    Dictionary<string, object> response = await IPInfo.ProxyVpnApi(row.Field<string>("IP"));

                    string IPAdress = row.Field<string>("IP");
                    string fraudScore = response["fraud_score"].ToString();
                    string countryCode = response["country_code"].ToString();
                    string region = response["region"].ToString();
                    string city = response["city"].ToString();
                    string isp = response["ISP"].ToString();
                    string organization = response["organization"].ToString();
                    bool isCrawler = Convert.ToBoolean(response["is_crawler"]);
                    bool mobile = Convert.ToBoolean(response["mobile"]);
                    string host = response["host"].ToString();
                    bool proxy = Convert.ToBoolean(response["proxy"]);
                    bool vpn = Convert.ToBoolean(response["vpn"]);
                    bool tor = Convert.ToBoolean(response["tor"]);
                    bool activeVpn = Convert.ToBoolean(response["active_vpn"]);
                    bool activeTor = Convert.ToBoolean(response["active_tor"]);

                    string insertQuery = "INSERT INTO GaskaClientIPInfo(IPAdress, host, fraud_score, country_code, region, city, ISP, organization, is_crawler, mobile, proxy, vpn, tor, active_vpn, active_tor) " +
                                         "VALUES (@IPAdress, @Host, @FraudScore, @CountryCode, @Region, @City, @ISP, @Organization, @IsCrawler, @Mobile, @Proxy, @VPN, @Tor, @ActiveVPN, @ActiveTor)";

                    using (SqlCommand insertCommand = new SqlCommand(insertQuery, connGaska))
                    {
                        insertCommand.Parameters.AddWithValue("@IPAdress", IPAdress);
                        insertCommand.Parameters.AddWithValue("@FraudScore", fraudScore);
                        insertCommand.Parameters.AddWithValue("@CountryCode", countryCode);
                        insertCommand.Parameters.AddWithValue("@Region", region);
                        insertCommand.Parameters.AddWithValue("@City", city);
                        insertCommand.Parameters.AddWithValue("@ISP", isp);
                        insertCommand.Parameters.AddWithValue("@Organization", organization);
                        insertCommand.Parameters.AddWithValue("@IsCrawler", isCrawler);
                        insertCommand.Parameters.AddWithValue("@Mobile", mobile);
                        insertCommand.Parameters.AddWithValue("@Host", host);
                        insertCommand.Parameters.AddWithValue("@Proxy", proxy);
                        insertCommand.Parameters.AddWithValue("@VPN", vpn);
                        insertCommand.Parameters.AddWithValue("@Tor", tor);
                        insertCommand.Parameters.AddWithValue("@ActiveVPN", activeVpn);
                        insertCommand.Parameters.AddWithValue("@ActiveTor", activeTor);

                        await insertCommand.ExecuteNonQueryAsync();
                    }
                }
                connGaska.Close();


                Console.WriteLine("Zapisano do tabeli nowe wartości");
                Console.ReadLine();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
    }
}

