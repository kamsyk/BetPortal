using System.Text;
using System.Text.Json;
using BetPortalWebApi.Contracts.Chat;
using BetPortalWebApi.Services.Interfaces;

namespace BetPortalWebApi.Services
{
    public class ChatService : IChatService
    {
        private const string ClaudeChatHttpClientName = "ClaudeChatService";

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
                var dbContext = await _dbQueryService.GetDatabaseContextAsync(userMessage);
                var systemPrompt = BuildSystemPrompt(dbContext);
                var messages = BuildClaudeMessages(conversationHistory, userMessage);
                var response = await CallClaudeChatServiceAsync(systemPrompt, messages);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat response");
                throw;
            }
        }

        private static List<ClaudeChatServiceMessage> BuildClaudeMessages(
            List<ChatMessage> conversationHistory,
            string currentMessage)
        {
            var messages = conversationHistory
                .Select(msg => new ClaudeChatServiceMessage
                {
                    Role = msg.Role,
                    Content = msg.Content,
                })
                .ToList();

            messages.Add(new ClaudeChatServiceMessage
            {
                Role = "user",
                Content = currentMessage,
            });

            return messages;
        }

        private string BuildSystemPrompt(string dbContext)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("You are a helpful AI assistant for a sports betting portal.");
            prompt.AppendLine("You help users with information about sports, countries, betting, odds, results, and general betting guidance.");
            prompt.AppendLine();
            prompt.AppendLine("Current database context:");
            prompt.AppendLine(dbContext);
            prompt.AppendLine();
            prompt.AppendLine("Guidelines:");
            prompt.AppendLine("- Provide clear, friendly, and engaging answers about sports, countries, and betting");
            prompt.AppendLine("- Help users understand betting odds, markets, and how to place bets");
            prompt.AppendLine("- Share information about available sports and countries in the system");
            prompt.AppendLine("- Share sports results, statistics, and upcoming events when available");
            prompt.AppendLine("- Use the database context to give accurate, up-to-date information");
            prompt.AppendLine("- Explain betting concepts in simple terms for users of all experience levels");
            prompt.AppendLine("- Encourage responsible gambling practices");
            prompt.AppendLine("- If you don't have enough information, ask for clarification");
            prompt.AppendLine("- Be helpful, professional, and supportive");

            return prompt.ToString();
        }

        private async Task<string> CallClaudeChatServiceAsync(
            string systemPrompt,
            List<ClaudeChatServiceMessage> messages)
        {
            var baseUrl = _configuration["ClaudeChatService:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException("Claude chat service base URL is not configured");
            }

            var request = new ClaudeChatServiceRequest
            {
                SystemPrompt = systemPrompt,
                Messages = messages,
                Model = _configuration["ClaudeChatService:Model"],
                MaxTokens = int.TryParse(_configuration["ClaudeChatService:MaxTokens"], out var maxTokens)
                    ? maxTokens
                    : null,
            };

            var client = _httpClientFactory.CreateClient(ClaudeChatHttpClientName);
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("/api/chat", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Claude chat service error: {StatusCode} - {Content}",
                    response.StatusCode,
                    errorContent);
                throw new HttpRequestException($"Claude chat service returned error: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            var chatResponse = JsonSerializer.Deserialize<ClaudeChatServiceResponse>(responseContent, options);

            return chatResponse?.Content ?? "I'm sorry, I couldn't generate a response.";
        }
    }
}
