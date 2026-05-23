using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BetPortalWebApi.Contracts.Chat;
using BetPortalWebApi.Services.Interfaces;
using BpDbLayer;
using BpDbLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace BetPortalWebApi.Services
{
    public sealed class ChatbotService : IChatbotService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly BpDbContext _dbContext;
        private readonly ILogger<ChatbotService> _logger;
        private readonly string _model;
        private readonly string _embeddingModel;
        private readonly string _vectorCollection;
        private readonly int _vectorSize;
        private readonly int _topK;

        public ChatbotService(
            HttpClient httpClient,
            IHttpClientFactory httpClientFactory,
            BpDbContext dbContext,
            IConfiguration configuration,
            ILogger<ChatbotService> logger)
        {
            _httpClient = httpClient;
            _httpClientFactory = httpClientFactory;
            _dbContext = dbContext;
            _logger = logger;
            _model = configuration["Llm:Model"] ?? "qwen2.5:7b-instruct";
            _embeddingModel = configuration["Llm:EmbeddingModel"] ?? "nomic-embed-text";
            _vectorCollection = configuration["VectorStore:Collection"] ?? "betportal_sports";
            _vectorSize = int.TryParse(configuration["VectorStore:VectorSize"], out var vectorSize) ? vectorSize : 768;
            _topK = int.TryParse(configuration["VectorStore:TopK"], out var topK) ? topK : 6;
        }

        public async Task<ChatAskResponse> AskAsync(string question, CancellationToken cancellationToken)
        {
            var trimmedQuestion = question.Trim();
            var contextRows = await BuildContextAsync(trimmedQuestion, cancellationToken);
            var prompt = BuildPrompt(trimmedQuestion, contextRows);

            var request = new
            {
                model = _model,
                prompt,
                stream = false,
                options = new
                {
                    temperature = 0.2,
                },
            };

            try
            {
                using var response = await _httpClient.PostAsJsonAsync("api/generate", request, cancellationToken);
                response.EnsureSuccessStatusCode();

                await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var doc = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
                var answer = doc.RootElement.TryGetProperty("response", out var answerNode)
                    ? answerNode.GetString() ?? string.Empty
                    : string.Empty;

                return new ChatAskResponse
                {
                    Answer = string.IsNullOrWhiteSpace(answer)
                        ? "I could not generate a response from the local model."
                        : answer,
                    Sources = contextRows.Select(c => c.SourceLabel).ToList(),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chat model request failed");
                return new ChatAskResponse
                {
                    Answer = "Local LLM is unavailable right now. Verify Ollama is running and the model is pulled.",
                    Sources = contextRows.Select(c => c.SourceLabel).ToList(),
                };
            }
        }

        public async Task<ChatReindexResponse> ReindexAsync(CancellationToken cancellationToken)
        {
            try
            {
                var sports = await _dbContext.Sports.AsNoTracking().OrderBy(s => s.Id).Take(1000).ToListAsync(cancellationToken);
                await EnsureCollectionAsync(cancellationToken);

                var indexed = 0;
                foreach (var sport in sports)
                {
                    var text = BuildSportText(sport);
                    var vector = await GenerateEmbeddingAsync(text, cancellationToken);
                    if (vector.Count == 0)
                    {
                        continue;
                    }

                    await UpsertPointAsync(sport.Id, text, vector, cancellationToken);
                    indexed++;
                }

                return new ChatReindexResponse
                {
                    Success = true,
                    IndexedItems = indexed,
                    Message = $"Indexed {indexed} sports into vector store '{_vectorCollection}'.",
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reindex failed");
                return new ChatReindexResponse
                {
                    Success = false,
                    IndexedItems = 0,
                    Message = "Reindex failed. Verify Ollama and Qdrant are available.",
                };
            }
        }

        private static string BuildPrompt(string question, List<ContextRow> contextRows)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are BetPortal assistant.");
            sb.AppendLine("Use only the provided context for factual claims.");
            sb.AppendLine("If information is missing, clearly say you do not know.");
            sb.AppendLine("Keep answers concise and practical.");
            sb.AppendLine();
            sb.AppendLine("Context:");

            foreach (var row in contextRows)
            {
                sb.AppendLine($"- [{row.SourceLabel}] {row.Text}");
            }

            sb.AppendLine();
            sb.AppendLine($"User question: {question}");
            sb.AppendLine("Answer:");

            return sb.ToString();
        }

        private static List<string> Tokenize(string text)
        {
            return text
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => t.Trim().ToLowerInvariant())
                .Where(t => t.Length >= 3)
                .Distinct()
                .ToList();
        }

        private async Task<List<ContextRow>> BuildContextAsync(string question, CancellationToken cancellationToken)
        {
            var semanticRows = await TryVectorSearchAsync(question, cancellationToken);
            if (semanticRows.Count > 0)
            {
                return semanticRows;
            }

            var tokens = Tokenize(question);
            var sports = await _dbContext.Sports.AsNoTracking().Take(500).ToListAsync(cancellationToken);

            var ranked = sports
                .Select(s =>
                {
                    var score = 0;
                    var sportName = s.Name ?? string.Empty;

                    foreach (var token in tokens)
                    {
                        if (sportName.Contains(token, StringComparison.OrdinalIgnoreCase))
                        {
                            score += 2;
                        }
                    }

                    if (question.Contains(sportName, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 3;
                    }

                    return new
                    {
                        Sport = s,
                        Score = score,
                    };
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Sport.Name)
                .Take(8)
                .ToList();

            if (ranked.Count == 0)
            {
                ranked = sports
                    .OrderBy(s => s.Name)
                    .Take(5)
                    .Select(s => new { Sport = s, Score = 0 })
                    .ToList();
            }

            return ranked
                .Select(x => new ContextRow
                {
                    SourceLabel = $"Sports:{x.Sport.Id}",
                    Text = $"Sport Id={x.Sport.Id}, Name={x.Sport.Name}, Active={x.Sport.Active}",
                })
                .ToList();
        }

        private async Task<List<ContextRow>> TryVectorSearchAsync(string question, CancellationToken cancellationToken)
        {
            try
            {
                var vector = await GenerateEmbeddingAsync(question, cancellationToken);
                if (vector.Count == 0)
                {
                    return [];
                }

                var qdrant = _httpClientFactory.CreateClient("Qdrant");
                var searchRequest = new
                {
                    vector,
                    limit = _topK,
                    with_payload = true,
                };

                using var response = await qdrant.PostAsJsonAsync($"collections/{_vectorCollection}/points/search", searchRequest, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Vector search failed with status code {StatusCode}", response.StatusCode);
                    return [];
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                if (!doc.RootElement.TryGetProperty("result", out var resultNode) || resultNode.ValueKind != JsonValueKind.Array)
                {
                    return [];
                }

                var rows = new List<ContextRow>();
                foreach (var item in resultNode.EnumerateArray())
                {
                    if (!item.TryGetProperty("payload", out var payloadNode))
                    {
                        continue;
                    }

                    var source = payloadNode.TryGetProperty("source", out var sourceNode)
                        ? sourceNode.GetString() ?? string.Empty
                        : string.Empty;
                    var text = payloadNode.TryGetProperty("text", out var textNode)
                        ? textNode.GetString() ?? string.Empty
                        : string.Empty;

                    if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(text))
                    {
                        continue;
                    }

                    rows.Add(new ContextRow
                    {
                        SourceLabel = source,
                        Text = text,
                    });
                }

                return rows;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Vector search is unavailable, falling back to token retrieval");
                return [];
            }
        }

        private async Task<List<float>> GenerateEmbeddingAsync(string input, CancellationToken cancellationToken)
        {
            var request = new
            {
                model = _embeddingModel,
                prompt = input,
            };

            using var response = await _httpClient.PostAsJsonAsync("api/embeddings", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Embedding request failed with status code {StatusCode}", response.StatusCode);
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (!doc.RootElement.TryGetProperty("embedding", out var embeddingNode) || embeddingNode.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var vector = new List<float>(_vectorSize);
            foreach (var value in embeddingNode.EnumerateArray())
            {
                vector.Add(value.GetSingle());
            }

            return vector;
        }

        private async Task EnsureCollectionAsync(CancellationToken cancellationToken)
        {
            var qdrant = _httpClientFactory.CreateClient("Qdrant");
            using var getResponse = await qdrant.GetAsync($"collections/{_vectorCollection}", cancellationToken);
            if (getResponse.IsSuccessStatusCode)
            {
                return;
            }

            var createRequest = new
            {
                vectors = new
                {
                    size = _vectorSize,
                    distance = "Cosine",
                },
            };

            using var createResponse = await qdrant.PutAsJsonAsync($"collections/{_vectorCollection}", createRequest, cancellationToken);
            createResponse.EnsureSuccessStatusCode();
        }

        private async Task UpsertPointAsync(int sportId, string text, List<float> vector, CancellationToken cancellationToken)
        {
            var qdrant = _httpClientFactory.CreateClient("Qdrant");
            var pointId = 1_000_000 + sportId;
            var upsertRequest = new
            {
                points = new object[]
                {
                    new
                    {
                        id = pointId,
                        vector,
                        payload = new
                        {
                            source = $"Sports:{sportId}",
                            text,
                            entity = "Sport",
                            entityId = sportId,
                        },
                    },
                },
            };

            using var response = await qdrant.PutAsJsonAsync($"collections/{_vectorCollection}/points?wait=true", upsertRequest, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        private static string BuildSportText(Sport sport)
        {
            return $"Sport Id={sport.Id}, Name={sport.Name}, Active={sport.Active}.";
        }

        private sealed class ContextRow
        {
            public string SourceLabel { get; set; } = string.Empty;

            public string Text { get; set; } = string.Empty;
        }
    }
}
