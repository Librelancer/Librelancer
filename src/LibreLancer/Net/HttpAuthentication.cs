using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LibreLancer.Net
{
    public class AuthInfo
    {
        [JsonIgnore]
        public string Url { get; set; }
        [JsonPropertyName("application")]
        public string Application { get; set; }
        [JsonPropertyName("registerEnabled")]
        public bool RegisterEnabled { get; set; }
        [JsonPropertyName("loginDifficulty")]
        public int LoginDifficulty { get; set; }
        [JsonPropertyName("registerDifficuty")]
        public int RegisterDifficulty { get; set; }
    }

    public static class HttpAuthentication
    {
        record LoginResult(string token);

        record VerifyResult(Guid guid);

        static string Combine(string baseUrl, string relUrl)
        {
            return $"{baseUrl.TrimEnd('/')}/{relUrl.TrimStart('/')}";
        }

        abstract class ProtectedRequest
        {
            static string ComputeSHA256(string rawData)  
            {  
                // Create a SHA256   
                using (SHA256 sha256Hash = SHA256.Create())  
                {  
                    // ComputeHash - returns byte array  
                    byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));  
  
                    // Convert byte array to a string   
                    StringBuilder builder = new StringBuilder();  
                    for (int i = 0; i < bytes.Length; i++)  
                    {  
                        builder.Append(bytes[i].ToString("x2"));  
                    }  
                    return builder.ToString();  
                }  
            }
            
            public string utctime { get; set; }
            public string nonce { get; set; }
            public string hash { get; set; }

            protected abstract string GetFields();
            public async Task Validate(int difficulty)
            {
                utctime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                int nonceInt = 0;
                nonce = nonceInt.ToString();
                var data = GetFields() + utctime;
                if (difficulty == 0)
                {
                    hash = ComputeSHA256(data + nonce);
                }
                else
                {
                    await Task.Run(() =>
                    {
                        var zeros = new string('0', difficulty);
                        while (!(hash = ComputeSHA256(data + nonce)).StartsWith(zeros))
                        {
                            nonceInt++;
                            nonce = nonceInt.ToString();
                        }
                    });
                }
            }
        }
        class UsernamePasswordRequest : ProtectedRequest
        {
            public string username { get; set; }
            public string password { get; set; }
            public UsernamePasswordRequest(string username, string password)
            {
                this.username = username;
                this.password = password;
            }
            protected override string GetFields()
            {
                return username + password;
            }
        }

        public static async Task<string> Login(this HttpClient client, AuthInfo info, string username, string password)
        {
            try
            {
                FLLog.Info("Http", $"Logging in {username}");
                var request = new UsernamePasswordRequest(username, password);
                await request.Validate(info.LoginDifficulty);
                var result = await client.PostAsync(Combine(info.Url,"/login"), JsonContent.Create(request));
                if (!result.IsSuccessStatusCode)
                {
                    FLLog.Error("Http", $"Login failed for {username} {result.StatusCode}. Response:\n {await result.Content.ReadAsStringAsync()}");
                    return null;
                }
                var login = await result.Content.ReadFromJsonAsync<LoginResult>();
                FLLog.Info("Http", $"Login success for {username}");
                return login.token;
            }
            catch (Exception e)
            {
                FLLog.Error("Http", e.ToString());
                return null;
            }
        }

        public static async Task<bool> Register(this HttpClient client, AuthInfo info, string username, string password)
        {
            try
            {
                FLLog.Info("Http", $"Registering {username}");
                var request = new UsernamePasswordRequest(username, password);
                await request.Validate(info.RegisterDifficulty);
                var result = await client.PostAsync(Combine(info.Url, "/register"), JsonContent.Create(request));
                if (!result.IsSuccessStatusCode)
                {
                    FLLog.Error("Http", $"Register failed for {username}");
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                FLLog.Error("Http", e.ToString());
                return false;
            }
        }
        
        public static async Task<AuthInfo> LoginServerInfo(this HttpClient client, string url)
        {
            try
            {
                FLLog.Info("Http", $"Contacting login server {url}");

                var requestUrl = Combine(url, "/info");
                var result = await client.GetAsync(requestUrl);
                if (!result.IsSuccessStatusCode)
                {
                    FLLog.Error("Http", $"Login server contact failed, {result.StatusCode}: {requestUrl}");
                    FLLog.Error("Http", "Response:\n" + await result.Content.ReadAsStringAsync());
                    return null;
                }
                var appInfo = await result.Content.ReadFromJsonAsync<AuthInfo>();
                if (appInfo.Application == "authserver")
                {
                    FLLog.Info("Http", $"Found login server {url}");
                    appInfo.Url = url;
                    return appInfo;
                }
                FLLog.Error("Http", $"info.application != authserver");
                return null;
            }
            catch (Exception e)
            {
                FLLog.Error("Http", e.ToString());
                return null;
            }
        }
        
        public static async Task<Guid> VerifyToken(this HttpClient client, string url, string token)
        {
            try
            {
                var result = await client.PostAsync(Combine(url,"/verifytoken"), JsonContent.Create(new
                {
                    token = token
                }));
                if (result.IsSuccessStatusCode)
                {
                    var verifyResult = await result.Content.ReadFromJsonAsync<VerifyResult>();
                    return verifyResult.guid;
                }
                var response = await result.Content.ReadAsStringAsync();
                FLLog.Info("Http", $"verifytoken failed. {result.StatusCode}: {response}");
                return Guid.Empty;
            }
            catch (Exception e)
            {
                FLLog.Error("Http", e.ToString());
                return Guid.Empty;
            }
        }
    }
}