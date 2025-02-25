using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ChatAgentTest.Server.WebRTCAudioServer
{
    public class AvailabilityService
    {
        private readonly HttpClient _httpClient;

        public AvailabilityService(HttpClient httpClient, string openAiApiKey)
        {
            _httpClient = httpClient;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openAiApiKey);
            _httpClient.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");
            _httpClient.DefaultRequestHeaders.Add("OpenAI-Organization", "org-jTpVlnfeflUXzVnv6ps8Hy00");
        }

        public async Task<string?> GetAvailabilityAsync(string assistantId, string floorplan, string dateRange)
        {
            // Create a thread
            var threadId = await CreateThreadAsync();

            // Create a message
            var message = await CreateMessageAsync(threadId, $"Check availability for floorplan {floorplan} on {dateRange}.");

            // Run the assistant on the thread
            var runId = await CreateRunAsync(assistantId, threadId);

            // Poll until the run is complete
            while (true)
            {
                await Task.Delay(2000); // Wait a couple seconds before checking

                var runStatusResponse = await _httpClient.GetAsync($"https://api.openai.com/v1/threads/{threadId}/runs/{runId}");
                if (!runStatusResponse.IsSuccessStatusCode)
                    return null;

                var runStatusJson = JsonDocument.Parse(await runStatusResponse.Content.ReadAsStringAsync());
                string status = runStatusJson.RootElement.GetProperty("status").GetString()!;

                if (status == "completed")
                    break;
                if (status == "failed" || status == "cancelled")
                    return null;
            }

            // Retrieve messages from the thread
            var messagesResponse = await _httpClient.GetAsync($"https://api.openai.com/v1/threads/{threadId}/messages");

            if (!messagesResponse.IsSuccessStatusCode)
                return null;

            var messagesJson = JsonDocument.Parse(await messagesResponse.Content.ReadAsStringAsync());
            var messages = messagesJson.RootElement.GetProperty("data").EnumerateArray().ToArray();
            var lastMessage = messages.Last().GetProperty("content").EnumerateArray().First().GetProperty("text").GetProperty("value").GetString();

            return lastMessage;
        }

        public async Task<string?> CreateThreadAsync()
        {
            var requestBody = new
            {
                messages = new object[]
                {
                }
            };

            var requestJson = JsonSerializer.Serialize(requestBody);
            var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/threads")
            {
                Content = requestContent
            };

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error creating thread: {response.StatusCode} - {errorContent}");
            }

            var jsonResponse = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return jsonResponse.RootElement.GetProperty("id").GetString();
        }
        public async Task<string?> CreateMessageAsync(string threadId, string message)
        {
            var requestBody = new
            {
                role = "user",
                content = new object[]
                {
                    new
                    {
                        type = "text",
                        text = message
                    }
                }
            };

            var requestJson = JsonSerializer.Serialize(requestBody);
            var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.openai.com/v1/threads/{threadId}/messages")
            {
                Content = requestContent
            };

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error creating thread: {response.StatusCode} - {errorContent}");
            }

            var jsonResponse = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return jsonResponse.RootElement.GetProperty("id").GetString();
        }

        public async Task<string?> CreateRunAsync(string assistantId, string threadId)
        {
            var listAssistantsResponse = await _httpClient.GetAsync("https://api.openai.com/v1/assistants");
            var listAssistantsResponseText = await listAssistantsResponse.Content.ReadAsStringAsync();

            var checkAssistantResponse = await _httpClient.GetAsync($"https://api.openai.com/v1/assistants/{assistantId}");
            var responseText = await checkAssistantResponse.Content.ReadAsStringAsync();

            var requestBody = new { assistant_id = assistantId };
            var runRequest = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.openai.com/v1/threads/{threadId}/runs")
            {
                Content = runRequest
            };

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error creating run: {response.StatusCode} - {errorContent}");
            }

            var runJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return runJson.RootElement.GetProperty("id").GetString()!;
        }
    }
}
