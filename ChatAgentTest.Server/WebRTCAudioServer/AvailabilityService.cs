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
            _httpClient.DefaultRequestHeaders.Add("OpenAI-Beta", "realtime=v2");
        }

        public async Task<string?> GetAvailabilityAsync(string assistantId, string floorplan, string dateRange)
        {
            // Create a thread
            var threadId = await CreateThreadAsync($"Check availability for floorplan {floorplan} on {dateRange}.");

            //  Run the assistant on the thread
            var runBody = new { assistant_id = assistantId };
            var runRequest = new StringContent(JsonSerializer.Serialize(runBody), Encoding.UTF8, "application/json");
            var runResponse = await _httpClient.PostAsync($"https://api.openai.com/v1/threads/{threadId}/runs", runRequest);

            if (!runResponse.IsSuccessStatusCode)
                return null;

            var runJson = JsonDocument.Parse(await runResponse.Content.ReadAsStringAsync());
            string runId = runJson.RootElement.GetProperty("id").GetString()!;

            // 4. Poll until the run is complete
            while (true)
            {
                await Task.Delay(2000); // Wait 2 seconds before checking

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

            // 5. Retrieve messages from the thread
            var messagesResponse = await _httpClient.GetAsync($"https://api.openai.com/v1/threads/{threadId}/messages");

            if (!messagesResponse.IsSuccessStatusCode)
                return null;

            var messagesJson = JsonDocument.Parse(await messagesResponse.Content.ReadAsStringAsync());
            var messages = messagesJson.RootElement.GetProperty("data").EnumerateArray().ToArray();
            var lastMessage = messages.Last().GetProperty("content").EnumerateArray().First().GetProperty("text").GetProperty("value").GetString();

            return lastMessage;
        }

        public async Task<string?> CreateThreadAsync(string message)
        {
            var requestBody = new
            {
                messages = new object[]
                {
                    new
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
                    }
                }
            };



            var requestJson = JsonSerializer.Serialize(requestBody);
            var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/threads")
            {
                Content = requestContent,
                Headers =
                {
                    { "Authorization", $"Bearer {""}" },
                    { "OpenAI-Beta", "assistants=v2" } // Correct API version
                }
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

    }

}
