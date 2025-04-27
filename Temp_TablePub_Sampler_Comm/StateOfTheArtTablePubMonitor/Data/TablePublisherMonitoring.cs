using System.Collections;
using System.Net.Http.Json;

namespace StateOfTheArtTablePubMonitor.Data
{
    public class ServicesInfo
    {
        public Dictionary<string, ServiceInfo> Services = new Dictionary<string, ServiceInfo>();

        public ServicesInfo()
        {
            var envVars = Environment.GetEnvironmentVariables();

            foreach (DictionaryEntry envVar in envVars)
            {
                var key = (string)envVar.Key;
                var value = (string?)envVar.Value;

                if (key.StartsWith("Services:"))
                {
                    var splitKey = key.Split(":");
                    var serviceUniqueName = splitKey[1];
                    var serviceInfoType = splitKey[2];

                    if (!Services.ContainsKey(serviceUniqueName))
                        Services.Add(serviceUniqueName, new ServiceInfo());

                    switch (serviceInfoType)
                    {
                        case "Ip":
                            Services[serviceUniqueName].Ip = string.IsNullOrEmpty(value) ? string.Empty : value;
                            break;
                        case "RestApiPort":
                            Services[serviceUniqueName].RestApiPort = string.IsNullOrEmpty(value) ? string.Empty : value;
                            break;
                        case "SupportPublisherMonitor":
                            Services[serviceUniqueName].SupportPublisherCacheMonitors = string.IsNullOrEmpty(value) ? false : bool.Parse(value);
                            break;
                    }
                }
            }
        }
    }

    public class ServiceInfo
    {
        public string UniqueName { get; set; }
        public string Ip { get; set; }
        public string RestApiPort { get; set; }
        public bool SupportPublisherCacheMonitors { get; set; }
    }

    // TODO: Move to common.
    public class TableInfo
    {
        public string Name { get; set; }
        public Guid Guid { get; set; }
    }

    public class ExtendedTableInfo : TableInfo
    {
        public bool Available { get; set; }
    }

    public class Table
    {
        public TableInfo TableInfo { get; set; }

        public List<List<string>> TableData { get; set; }
    }

    public class ProberCacheMonitoringService
    {
        private Dictionary<string, HttpClient> appsThatSupportMonitoring = new Dictionary<string, HttpClient>();

        public ProberCacheMonitoringService()
        {
            Environment.SetEnvironmentVariable("Services:RanBlazor:Ip", "127.0.0.1");
            Environment.SetEnvironmentVariable("Services:RanBlazor:RestApiPort", "7287");
            Environment.SetEnvironmentVariable("Services:RanBlazor:SupportPublisherMonitor", "true");

            var servicesInfo = new ServicesInfo();
            foreach (var service in servicesInfo.Services)
            {
                if (service.Value.SupportPublisherCacheMonitors)
                {
                    var handler = new HttpClientHandler()
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    };

                    appsThatSupportMonitoring.Add(service.Key, new HttpClient(handler) { BaseAddress = new Uri($"https://{service.Value.Ip}:{service.Value.RestApiPort}") });
                }
            }
        }

        //private Dictionary<Guid, Tuple<string, string>> requieredTables = new Dictionary<Guid, Tuple<string, string>>();
        //
        //public void AddRemoveRequieredTables(string app, string name, Guid guid, bool trueToAddOrFlaseToRemove)
        //{
        //    lock (requieredTables)
        //    {
        //        if (trueToAddOrFlaseToRemove)
        //        {
        //            if (!requieredTables.ContainsKey(guid))
        //                requieredTables.Add(guid, Tuple.Create(app, name));
        //        }
        //        else
        //        {
        //            if (requieredTables.ContainsKey(guid))
        //                requieredTables.Remove(guid);
        //        }
        //    }
        //}


        //public async Task<List<List<ExtendedTableInfo>>> GetAllAppsTableInfoAsync()
        //{
        //    return await Task.Run(() =>
        //    {
        //        var result = new List<List<ExtendedTableInfo>>();

        //        foreach (var app in appsThatSupportMonitoring.Values)
        //        {
        //            result.Add(GetAppTableInfo(app));
        //        }

        //        return result;
        //    });
        //}

        public async Task<List<Tuple<string, List<ExtendedTableInfo>>>> GetAllAppsTableInfoAsync()
        {
            return await Task.Run(() =>
            {
                var result = new List<Tuple<string, List<ExtendedTableInfo>>>();

                foreach (var app in appsThatSupportMonitoring)
                {
                    result.Add(Tuple.Create(app.Key, GetAppTableInfo(app.Value)));
                }

                return result;
            });
        }

        //public List<Tuple<string, List<ExtendedTableInfo>>> GetAllAppsTableInfo()
        //{
        //    var result = new List<Tuple<string, List<ExtendedTableInfo>>>();
        //
        //    foreach (var app in appsThatSupportMonitoring)
        //    {
        //        result.Add(Tuple.Create(app.Key,GetAppTableInfo(app.Value)));
        //    }
        //
        //    return result;
        //}

        public List<ExtendedTableInfo> GetAppTableInfo(HttpClient http)
        {
            List<ExtendedTableInfo>? result = http.GetFromJsonAsync<List<ExtendedTableInfo>>($"api/ProberCacheMonitoring/CachedTablesNames").GetAwaiter().GetResult();

            return result ?? new List<ExtendedTableInfo>();
        }

        public async Task<List<Table>> GetAppTableDataAsync(string appName, List<Guid> guids)
        {
            return await Task.Run(() => GetAppTableData(appsThatSupportMonitoring[appName], guids));
        }

        public List<Table> GetAppTableData(HttpClient client, List<Guid> guids)
        {
            var response = client.PostAsJsonAsync($"api/ProberCacheMonitoring/CachedTables", guids).GetAwaiter().GetResult();

            if (response.IsSuccessStatusCode)
                return response.Content.ReadFromJsonAsync<List<Table>>().Result ?? new List<Table>();

            return new List<Table>();
        }
    }
}
