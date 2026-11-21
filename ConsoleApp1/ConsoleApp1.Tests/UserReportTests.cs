using System.Net;
using System.Net.Http.Json;
using ConsoleApp1.Models.Dto;
using ConsoleApp1.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging.Abstractions;

namespace ConsoleApp1.Tests;

public class UserReportTests(WebApplicationFactory<ReportServiceAssembly> factory)
    : IClassFixture<WebApplicationFactory<ReportServiceAssembly>>
{
    [Fact]
    public async Task ClientPostsUsersToRunningReportEndpoint()
    {
        using var http = factory.CreateClient();
        var client = new UserReportClient(http, NullLogger<UserReportClient>.Instance);
        var date = new DateTime(2026, 9, 17, 14, 20, 0, DateTimeKind.Utc);

        var result = await client.CreateReportAsync([new UserExportDto(12, "example", date, date)]);

        Assert.True(result.Success);
        Assert.Contains("12 | example | 2026-09-17T14:20:00.0000000Z", result.Data);
    }

    [Fact]
    public async Task EndpointRejectsInvalidUser()
    {
        using var http = factory.CreateClient();
        using var response = await http.PostAsJsonAsync("/reports/users",
            new[] { new UserExportDto(0, "example", DateTime.UtcNow, DateTime.UtcNow) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ClientReturnsFailureWhenServiceTimesOut()
    {
        using var http = new HttpClient(new WaitingHandler())
        {
            BaseAddress = new Uri("http://localhost"),
            Timeout = TimeSpan.FromMilliseconds(50)
        };
        var client = new UserReportClient(http, NullLogger<UserReportClient>.Instance);

        var result = await client.CreateReportAsync([]);

        Assert.False(result.Success);
        Assert.Contains("не ответил вовремя", result.Message);
    }

    private sealed class WaitingHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
