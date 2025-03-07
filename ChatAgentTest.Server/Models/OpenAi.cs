namespace ChatAgentTest.Server.Models
{
    public class CreateAssistantRequest
    {
        public string Instructions { get; set; } = "You are a personal math tutor. When asked a question, write and run Python code to answer the question.";
        public string Name { get; set; } = "Math Tutor";
        public string Model { get; set; } = "gpt-4o";
    }

}
