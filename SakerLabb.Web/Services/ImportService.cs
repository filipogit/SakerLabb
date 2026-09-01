using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json;

namespace SakerLabb.Web.Services;

public class ImportService
{
    private readonly ILogger<ImportService> _logger;
    private readonly HttpClient _http;

    public ImportService(ILogger<ImportService> logger, HttpClient http)
    {
        _logger = logger;
        _http = http;
    }

    public string ImportXml(string xml)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Parse,
            XmlResolver = new XmlUrlResolver()
        };

        var document = new XmlDocument { XmlResolver = new XmlUrlResolver() };
        using var reader = XmlReader.Create(new StringReader(xml), settings);
        document.Load(reader);

        return document.DocumentElement?.InnerText ?? "";
    }

    public object? ImportJson(string json)
    {
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        return JsonConvert.DeserializeObject(json, settings);
    }

    public async Task<string> FetchRemote(string url)
    {
        _logger.LogInformation("Hämtar fjärresurs {Url}", url);
        var response = await _http.GetAsync(url);
        return await response.Content.ReadAsStringAsync();
    }

    private static readonly Regex ValidHostPattern = new(@"^[a-zA-Z0-9._\-]+$");

    public string Ping(string host)
    {
        if (string.IsNullOrWhiteSpace(host) || !ValidHostPattern.IsMatch(host))
        {
            return "Ogiltigt hostnamn.";
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ping",
                ArgumentList = { "-n", "2", host },
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit(5000);
        return output;
    }
}
