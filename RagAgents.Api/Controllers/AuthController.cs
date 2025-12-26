using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace RagAgents.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("token")]
        public async Task<IActionResult> GetToken()
        {
            // HARD-CODED VALUES (TESTING ONLY)
            string tenantId = "1ddc83ad-14d5-48f3-b208-e23b0d229339";
            string clientId = "0d10b9b2-db36-4350-84c3-55d4d17cd4d1";
            string clientSecret = "a5R8Q~WlkxhIgr.DA.gy7dVp0eVw85eKK4NkRaWp";
            string scope = "api://7d10b9b2-db36-4350-84c3-55d4d17cd4d1/.default";

            var tokenEndpoint =
                $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

            var client = _httpClientFactory.CreateClient();

            var form = new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "grant_type", "client_credentials" },
                { "scope", scope }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(form)
            };

            request.Content.Headers.ContentType =
                new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, content);
            }

            return Content(content, "application/json");
        }
    }
}
