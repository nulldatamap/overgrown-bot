using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;

namespace OvergownBot
{
    public class SteamUserResponse
    {
        public SteamUserResponseContent response { get; set; }
    }
    
    public class SteamUserResponseContent
    {
        public List<SteamUser> players { get; set; }
    }

    public class SteamUser
    {
        public string steamid { get; set; }
        public string personaname { get; set; }
    }
    
    public class Requester
    {
        static string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };
        // TODO: Make this configurable?
        private String _spreadsheetId = "1LVUp0_XvBC8aIqgEnbTlVxWASsoElngTizlbdoNQLRU"; 
        private static string _applicationName = "OvergrownBot"; 
        private UserCredential _credential;
        private SheetsService _service;
        private string _steamApiKey;
        
        private HttpClient _httpClient = new HttpClient()
        {
            BaseAddress = new Uri("http://api.steampowered.com")
        };
        
        public Requester()
        {
            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                _credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            _service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = _credential,
                ApplicationName = _applicationName,
            });

            _steamApiKey = File.ReadAllText("steamapikey.txt").Trim();
        }

        public ValueRange RawExecute(string range)
        {
            return _service.Spreadsheets.Values.Get(_spreadsheetId, range).Execute();
        }

        public List<SteamUser> ResolveSteamIds(string[] steamids)
        {
            var req = $"ISteamUser/GetPlayerSummaries/v0002/?key={_steamApiKey}&steamids={string.Join(',', steamids)}";
            var result =
                Task.Run(async () => await _httpClient.GetFromJsonAsync<SteamUserResponse>(req)).Result;
            return result?.response?.players;
        }
        
        public SteamUser ResolveSteamId(string steamid)
        {
            var req = $"ISteamUser/GetPlayerSummaries/v0002/?key={_steamApiKey}&steamids={steamid}";
            var result =
                Task.Run(async () => await _httpClient.GetFromJsonAsync<SteamUserResponse>(req)).Result;
            return result?.response?.players?.FirstOrDefault(x => true);
        }
    }
}