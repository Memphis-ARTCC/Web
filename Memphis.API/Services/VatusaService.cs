using Memphis.Shared.Dtos.Vatusa;
using Newtonsoft.Json;

namespace Memphis.API.Services;

public class VatusaService
{
    private readonly ILogger<VatusaService> _logger;

    public VatusaService(ILogger<VatusaService> logger)
    {
        _logger = logger;
    }

    public async Task<IList<TransferRequestBody>?> GetTransferRequests()
    {
        try
        {
            using var client = new HttpClient();
            var facility = Environment.GetEnvironmentVariable("VATUSA_FACILITY") ??
                throw new ArgumentNullException("VATUSA_FACILITY env variable not found");
            var apiKey = Environment.GetEnvironmentVariable("VATUSA_API_KEY") ??
                throw new ArgumentNullException("VATUSA_API_KEY env variable not found");
            var response = await client.GetAsync($"https://api.vatusa.net/facility/{facility}/transfers?apikey={apiKey}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("GetTransferRequests error '{StatusCode}' '{ReasonPhrase}'", response.StatusCode, response.ReasonPhrase);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<VatusaResponseDto<TransferRequestDto>>(content);
            if (result == null)
            {
                _logger.LogError("GetTransferRequests error 'data is null'");
                return null;
            }
            return result.Data.Transfers;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetTransferRequests error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            throw;
        }
    }

    public async Task<IList<TransferHistoryDto>?> GetTransferHistory(int cid)
    {
        try
        {
            using var client = new HttpClient();
            var apiKey = Environment.GetEnvironmentVariable("VATUSA_API_KEY") ??
                throw new ArgumentNullException("VATUSA_API_KEY env variable not found");
            var response = await client.GetAsync($"https://api.vatusa.net/user/{cid}/transfer/history?apikey={apiKey}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("GetTransferHistory error '{StatusCode}' '{ReasonPhrase}'", response.StatusCode, response.ReasonPhrase);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<VatusaResponseDto<IList<TransferHistoryDto>>>(content);
            if (result == null)
            {
                _logger.LogError("GetTransferHistory error 'data is null'");
                return null;
            }
            return result.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetTransferHistory error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            throw;
        }
    }

    public async Task<(bool Success, string? Message)> ActionTransferRequest(int transferId, string action, string by, string? reason)
    {
        try
        {
            if (action != "accept" && action != "reject")
            {
                throw new ArgumentException("Invalid action");
            }
            using var client = new HttpClient();
            var facility = Environment.GetEnvironmentVariable("VATUSA_FACILITY") ??
                throw new ArgumentNullException("VATUSA_FACILITY env variable not found");
            var apiKey = Environment.GetEnvironmentVariable("VATUSA_API_KEY") ??
                throw new ArgumentNullException("VATUSA_API_KEY env variable not found");
            var bodyDict = new Dictionary<string, string>
            {
                ["action"] = action,
                ["by"] = by
            };
            if (!string.IsNullOrWhiteSpace(reason))
            {
                bodyDict["reason"] = reason;
            }
            var body = new FormUrlEncodedContent(bodyDict);
            var response = await client.PutAsync($"https://api.vatusa.net/facility/{facility}/transfers/{transferId}?apikey={apiKey}", body);
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogError("ActionTransferRequest error '{StatusCode}' '{ReasonPhrase}'\n{Content}", response.StatusCode, response.ReasonPhrase, content);
                if (content.Contains("forbidden", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, "forbidden");
                }
            }
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError("ActionTransferRequest error '{Message}'\n{StackTrace}", ex.Message, ex.StackTrace);
            throw;
        }
    }
}
