using System.Net.Http.Headers;
using Microsoft.AspNetCore.WebUtilities;

class Program
{
    static void Main(string[] args)
    {
        string appId = "----";
        string apiKey = "----";
        string testName = "testNameInInsights";

        DateTimeOffset startTime = DateTimeOffset.UtcNow.AddDays(-1);
        DateTimeOffset endTime = DateTimeOffset.UtcNow;

        var checker = new AvailabilityChecker(appId, apiKey);
        double availabilityPercentage = checker.GetAvailabilityPercentage(startTime, endTime, testName).GetAwaiter().GetResult();

        Console.WriteLine($"Availability percentage between {startTime} and {endTime}: {availabilityPercentage}%");

    }

}

public class AvailabilityChecker
{
    private const string ApiEndpoint = "https://api.applicationinsights.io/v1/apps/";
    private readonly string _appId;
    private readonly string _apiKey;

    public AvailabilityChecker(string appId, string apiKey)
    {
        _appId = appId;
        _apiKey = apiKey;
    }

    public async Task<double> GetAvailabilityPercentage(DateTimeOffset startTime, DateTimeOffset endTime, string testName)
    {
        string query = $"availabilityResults " +
            $"| where timestamp between(todatetime('{startTime:O}') .. todatetime('{endTime:O}')) and name == \"{testName}\"" +
            $"| project success " +
            $"| extend percentage = toint(success) * 100 " +
            $"| summarize avg(percentage)";

        string requestUrl = $"{ApiEndpoint}{_appId}/query";

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);

        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.RequestUri = new Uri(QueryHelpers.AddQueryString(requestUrl, "query", query));

        HttpResponseMessage response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        dynamic result = await response.Content.ReadAsAsync<dynamic>();

        if (result.tables[0].rows.Count > 0)
        {
            double availabilityPercentage = result.tables[0].rows[0][0];
            return availabilityPercentage;
        }

        return 0;
    }
}