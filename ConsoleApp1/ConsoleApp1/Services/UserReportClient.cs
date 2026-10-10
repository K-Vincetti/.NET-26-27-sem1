using System.Net.Http.Json;
using ConsoleApp1.Common;
using ConsoleApp1.Models.Dto;
using Microsoft.Extensions.Logging;

namespace ConsoleApp1.Services;

public sealed class UserReportClient(HttpClient httpClient, ILogger<UserReportClient> logger)
{
    public async Task<OperationResult<string>> CreateReportAsync(IReadOnlyList<UserExportDto> users)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync("reports/users", users);
            response.EnsureSuccessStatusCode();
            var report = await response.Content.ReadAsStringAsync();
            logger.LogInformation("Отчёт получен от сервиса печати");
            return OperationResult<string>.Ok(report, "Отчёт получен");
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Истекло время ожидания сервиса печати");
            return OperationResult<string>.Fail("Сервис печати не ответил вовремя");
        }
        catch (HttpRequestException exception)
        {
            if (exception.StatusCode is { } status)
            {
                logger.LogError("Сервис печати вернул HTTP {StatusCode}", (int)status);
            }
            else
            {
                logger.LogWarning("Не удалось подключиться к сервису печати");
            }

            return OperationResult<string>.Fail("Сервис печати недоступен");
        }
    }
}
