using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore.Storage;

namespace LibreLancer
{

    public static class HttpAuthentication
    {
        record ServerInfo(string application, bool registerEnabled);
        record LoginResult(string token);

        record VerifyResult(Guid guid);

        static Uri Combine(string baseUrl, string relUrl) => new Uri(new Uri(baseUrl), relUrl);
        public static async Task<string> Login(this HttpClient client, string url, string username, string password)
        {
            try
            {
                FLLog.Info("Http", $"Logging in {username}");
                var result = await client.PostAsync(Combine(url,"/login"), JsonContent.Create(new
                {
                    username = username,
                    password = password
                }));
                if (!result.IsSuccessStatusCode)
                {
                    FLLog.Error("Http", $"Login failed for {username} {result.StatusCode} {await result.Content.ReadAsStringAsync()}");
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

        public static async Task<bool> Register(this HttpClient client, string url, string username, string password)
        {
            try
            {
                FLLog.Info("Http", $"Registering {username}");
                var result = await client.PostAsync(Combine(url,"/register"), JsonContent.Create(new
                {
                    username = username,
                    password = password
                }));
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
        
        public static async Task<bool> LoginServerInfo(this HttpClient client, string url)
        {
            try
            {
                FLLog.Info("Http", $"Contacting login server {url}");

                var result = await client.GetAsync(Combine(url, "/info"));
                if (!result.IsSuccessStatusCode) return false;
                var appInfo = await result.Content.ReadFromJsonAsync<ServerInfo>();
                if (appInfo.application == "authserver")
                {
                    FLLog.Info("Http", $"Found login server {url}");
                    return true;
                }
                FLLog.Error("Http", $"{appInfo.application} != authserver");
                return false;
            }
            catch (Exception e)
            {
                FLLog.Error("Http", e.ToString());
                return false;
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