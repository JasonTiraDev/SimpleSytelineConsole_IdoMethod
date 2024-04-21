using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static readonly HttpClient client = new HttpClient();

    static async Task Main()
    {
        // Configuration and authentication data
        string serverName = "localhost";
        string configName = "jtdemo_dals";
        string idoName = "UserNames";
        string methodName = "GetUserAttributes";
        string username = "sa";
        string password = "sa";

        try
        {
            string token = await GetSecurityToken($"http://{serverName}/IDORequestService/MGRESTService.svc", configName, username, password);
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Failed to obtain security token.");
                return;
            }

            string[] parameters = { "sa", null, null, null, null };
            string content = await InvokeIDOMethod($"https://{serverName}/IDORequestService", idoName, methodName, parameters, token, configName);
            Console.WriteLine(content);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
        }
    }

    // Get the security token for the specified configuration
    static async Task<string> GetSecurityToken(string baseUrl, string configName, string username, string password)
    {
        string requestUrl = $"{baseUrl}/js/token/{configName}";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        request.Headers.Add("userid", Uri.EscapeDataString(username));
        request.Headers.Add("password", Uri.EscapeDataString(password));

        try
        {
            using var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to obtain security token: " + response.StatusCode);
                Console.ResetColor();
                return null;
            }
            var responseContent = await response.Content.ReadAsStringAsync();
            return responseContent.Trim('"');
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Exception while obtaining token: {ex.Message}");
            Console.ResetColor();
            return null;
        }
    }

    static async Task<string> InvokeIDOMethod(string baseUrl, string idoName, string methodName, string[] parameters, string token, string configName)
    {
        string requestUrl = $"{baseUrl}/ido/invoke/{idoName}?method={methodName}";
        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Headers.TryAddWithoutValidation("Authorization", token);
        request.Headers.Add("X-Infor-MongooseConfig", configName);

        // Serialize parameters using System.Text.Json
        string jsonPayload = JsonSerializer.Serialize(parameters);
        request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Failed to invoke IDO method - StatusCode: {response.StatusCode}");
            Console.ResetColor();
            return null;
        }
        return await response.Content.ReadAsStringAsync();
    }
}
