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
        static string[] Scopes = { SheetsService.Scope.Spreadsheets };
        // TODO: Make this configurable?
        private String _spreadsheetId = "1LVUp0_XvBC8aIqgEnbTlVxWASsoElngTizlbdoNQLRU"; 
        private static string _applicationName = "OvergrownBot"; 
        private GoogleCredential _credential;
        private SheetsService _service;
        private string _steamApiKey;
        private Dictionary<string, SteamUser> _cachedSteamUsers = new Dictionary<string, SteamUser>();
        
        private HttpClient _httpClient = new HttpClient()
        {
            BaseAddress = new Uri("http://api.steampowered.com")
        };
        
        public Requester()
        {
            using (var stream =
                new FileStream("service-key.json", FileMode.Open, FileAccess.Read))
            {
                _credential = GoogleCredential.FromStream(stream).CreateScoped(Scopes);
            }

            _service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = _credential,
                ApplicationName = _applicationName,
            });

            _steamApiKey = File.ReadAllText("steamapikey.txt").Trim();
        }

        public ValueRange Query(string range)
        {
            return _service.Spreadsheets.Values.Get(_spreadsheetId, range).Execute();
        }

        public object QuerySingle(string range)
        {
            var vs = Query(range);
            if (vs?.Values == null || vs.Values.Count != 1 || vs.Values[0]?.Count != 1)
                throw new InvalidOperationException("Got non-singleton value");
            return vs.Values[0][0];
        }

        public void WriteSingle(string range, object value)
        {
            var vr = new ValueRange();
            vr.Values = new List<IList<object>>() { new List<object>() { value } };
            vr.Range = range;
            var ur = _service.Spreadsheets.Values.Update(vr, _spreadsheetId, range);
            ur.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            var resp = ur.Execute();
        }

        public SteamUser GetSteamUser(string id)
        {
            SteamUser user;
            if (!_cachedSteamUsers.TryGetValue(id, out user))
            {
                user = ResolveSteamId(id);
                _cachedSteamUsers.Add(id, user);
            }
            return user;
        }

        public List<SteamUser> ResolveSteamIds(IEnumerable<string> steamids)
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

        public void CacheSteamIds(IEnumerable<string> steamIds)
        {
            var ids = steamIds.ToArray();
            var users = 
                steamIds.Where(id => id.ToUpper() != "NEED ID")
                    .Select((x, i) => (x, i))
                    .GroupBy(x => x.i / 100)
                    .SelectMany((x, _) => ResolveSteamIds(x.Select(x => x.x)))
                    .ToArray();
            
            foreach (var id in ids)
            {
                if (!_cachedSteamUsers.ContainsKey(id))
                    _cachedSteamUsers.Add(id, users.FirstOrDefault(user => user.steamid == id));
            }
        }
    }
}