using System.Net.Http;
using System.Threading.Tasks;
using DRM.Data;
using Microsoft.EntityFrameworkCore;

namespace DRM.MiddleWare
{
    public class AllowanceChecker
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public AllowanceChecker(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<bool> CanRegisterMoreStudentsAsync()
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync("http://13.36.39.94:5000/api/LiscenseHelper/Students");

                if (!response.IsSuccessStatusCode)
                    return false;

                var allowedCountStr = await response.Content.ReadAsStringAsync();
                if (!int.TryParse(allowedCountStr, out int allowedCount))
                    return false;

                var currentCount = await _context.Students.CountAsync();

                return currentCount < allowedCount;
            }
            catch
            {
                return false;
            }
        }
    }
}
