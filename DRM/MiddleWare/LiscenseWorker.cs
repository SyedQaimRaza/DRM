using System.Text.Json;
using System.Text;

namespace DRM.MiddleWare
{
    public class LiscenseWorker
    {
        private readonly HttpClient _httpClient;

        public LiscenseWorker(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> IsLicenseValidAsync(string username)
        {
            var jsonContent = JsonSerializer.Serialize(new { userName = username });
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("http://13.36.39.94:5000/api/LiscenseHelper/Check", content);

                if (!response.IsSuccessStatusCode)
                    return false;

                var result = (await response.Content.ReadAsStringAsync()).Trim();
                return result == "0"; // 0 => Valid, 1 => Expired
            }
            catch
            {
                return false; // Treat errors as invalid
            }
        }

    }
}
