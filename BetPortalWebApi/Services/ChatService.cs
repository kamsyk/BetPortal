using System.Text;
using System.Text.Json;
using BetPortalWebApi.Contracts.Chat;
using BetPortalWebApi.Services.Interfaces;

namespace BetPortalWebApi.Services
{
    public class ChatService : IChatService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDbQueryService _dbQueryService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            IHttpClientFactory httpClientFactory,
            IDbQueryService dbQueryService,
            IConfiguration configuration,
            ILogger<ChatService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _dbQueryService = dbQueryService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GetChatResponseAsync(string userMessage, List<ChatMessage> conversationHistory)
        {
            try
            {
                // Get relevant database context based on the user's message
                var dbContext = await _dbQueryService.GetDatabaseContextAsync(userMessage);

                // Build the system prompt with database context
                var systemPrompt = BuildSystemPrompt(dbContext);

                // Build the messages for Claude API
                var messages = BuildClaudeMessages(conversationHistory, userMessage);

                // Call Claude API
                var response = await CallClaudeApiAsync(systemPrompt, messages);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat response");
                throw;
            }
        }

        private string BuildSystemPrompt(string dbContext)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("You are an AI assistant for a betting portal management system.");
            prompt.AppendLine("You help administrators understand and analyze their data.");
            prompt.AppendLine();
            prompt.AppendLine("Current database context:");
            prompt.AppendLine(dbContext);
            prompt.AppendLine();
            prompt.AppendLine("Guidelines:");
            prompt.AppendLine("- Provide clear, concise answers");
            prompt.AppendLine("- Use the database context to give accurate information");
            prompt.AppendLine("- If you don't have enough information, ask for clarification");
            prompt.AppendLine("- Be helpful and professional");

            return prompt.ToString();
        }

        private List<object> BuildClaudeMessages(List<ChatMessage> conversationHistory, string currentMessage)
        {
            var messages = new List<object>();

            // Add conversation history
            foreach (var msg in conversationHistory)
            {
                messages.Add(new
                {
                    role = msg.Role,
                    content = msg.Content
                });
            }

            // Add current message
            messages.Add(new
            {
                role = "user",
                content = currentMessage
            });

            return messages;
        }

        private async Task<string> CallClaudeApiAsync(string systemPrompt, List<object> messages)
        {
            var apiKey = _configuration["Claude:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Claude API key is not configured");
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var requestBody = new
            {
                model = _configuration["Claude:Model"] ?? "claude-3-5-sonnet-20241022",
                max_tokens = int.Parse(_configuration["Claude:MaxTokens"] ?? "1024"),
                system = systemPrompt,
                messages
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("https://api.anthropic.com/v1/messages", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Claude API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Claude API returned error: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Claude API Response: {Response}", responseContent);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var claudeResponse = JsonSerializer.Deserialize<ClaudeApiResponse>(responseContent, options);

            var result = claudeResponse?.Content?.FirstOrDefault()?.Text ?? "I'm sorry, I couldn't generate a response.";
            _logger.LogInformation("Parsed response text: {Text}", result);
            
            return result;
        }

        private class ClaudeApiResponse
        {
            public List<ContentBlock>? Content { get; set; }

            public class ContentBlock
            {
                public string? Type { get; set; }
                public string? Text { get; set; }
            }
        }
    }
}
