using System.Text.Json;

namespace ChatAgentTest.Server.WebsocketAudioServer
{
    public static class JsonOptions
    {
        public static readonly JsonSerializerOptions SnakeCase = new JsonSerializerOptions
        {
            PropertyNamingPolicy = new SnakeCaseNamingPolicy()
        };
    }

    public class SnakeCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name) =>
            string.Concat(name.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString().ToLower() : x.ToString().ToLower()));
    }
}
