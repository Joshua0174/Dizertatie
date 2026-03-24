using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PresentationLayer.Tests
{
    // IClassFixture pornește API-ul tău în memorie o singură dată pentru toate testele din această clasă
    public class RateLimiterTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public RateLimiterTests(WebApplicationFactory<Program> factory)
        {
            // Creăm un "browser" fals (HttpClient) care comunică direct cu API-ul din memorie
            _client = factory.CreateClient();
        }

        [Fact] // [Fact] îi spune lui xUnit că aceasta este o metodă de test
        public async Task Login_ShouldReturn429_WhenRateLimitExceeded()
        {
            // Arrange (Pregătirea datelor)
            // Trimitem un JSON oarecare, nu contează dacă email-ul există sau nu, 
            // pentru că vrem doar să testăm limitatorul de pe ușă, nu baza de date.
            var jsonBody = "{\"email\":\"test@test.com\",\"password\":\"Parola123!\"}";

            // Act & Assert (Acțiune și Verificare)
            // 1. Facem 5 cereri rapide. Acestea ar trebui să treacă de Rate Limiter.
            for (int i = 1; i <= 5; i++)
            {
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                var response = await _client.PostAsync("/api/auth/login", content);

                // Ne asigurăm că NU am primit eroarea 429 la primele 5 încercări
                Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
            }

            // 2. Facem a 6-a cerere. Aici capcana trebuie să se închidă!
            var blockedContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var blockedResponse = await _client.PostAsync("/api/auth/login", blockedContent);

            // Verificăm cu strictețe dacă am primit exact statusul 429
            Assert.Equal(HttpStatusCode.TooManyRequests, blockedResponse.StatusCode);
        }
    }
}