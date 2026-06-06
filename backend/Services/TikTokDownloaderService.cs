using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace backend.Services
{
    public class TikTokDownloaderService(HttpClient httpClient, ILogger<TikTokDownloaderService> logger)
    {
        public async Task<TikWmData?> GetTikTokDataAsync(string tiktokUrl)
        {
            string apiUrl = $"https://tikwm.com/api/?url={tiktokUrl}";

            try
            {
                var response = await httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogInformation("TikWM API unavailable. Status: {Code}", response.StatusCode);
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<TikWmResponse>();

                if(result != null && result.Code == 0 && result.Data != null)
                {
                    if(!string.IsNullOrEmpty(result.Data.Play) && result.Data.Play.StartsWith('/'))
                        result.Data.Play = "https://tikwm.com" + result.Data.Play;

                    return result.Data;
                }
                else
                    logger.LogWarning("TikWM returned error: {Msg}", result?.Msg);
            }
            catch (Exception ex) 
            {
                logger.LogError(ex, "Error while requesting TikWM API");
            }

            return null;
        }
        public async Task<Stream?> GetFileStreamAsync(string url)
        {
            try
            {
                var fileBytes = await httpClient.GetByteArrayAsync(url);
                return new MemoryStream(fileBytes);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while donwloading media file: {Url}", url);
            }

            return null;
        }
    }

    // JSON mapping
    public class TikWmResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("msg")]
        public string? Msg { get; set; }

        [JsonPropertyName("data")]
        public TikWmData? Data { get; set; }
    }
    public class TikWmData
    {
        [JsonPropertyName("play")]
        public string? Play { get; set; }

        [JsonPropertyName("images")]
        public List<string>? Images { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }
    }
}
