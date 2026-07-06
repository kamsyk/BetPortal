# AI Advisor Chatbot

## Overview
The AI Advisor is an intelligent chatbot feature integrated into the BetPortal Admin application. It uses Claude AI to answer questions about your betting portal data, providing insights about sports, users, roles, and other database information.

## Features
- **Real-time Chat Interface**: Modern, responsive chat UI with Material Design
- **Claude AI Integration**: Powered by Anthropic's Claude 3.5 Sonnet model
- **Database Context**: Automatically retrieves relevant database information based on your questions
- **Conversation History**: Maintains context across multiple messages
- **Secure**: Protected by authentication and authorization

## Setup Instructions

### 1. Get Claude API Key
1. Visit [Anthropic Console](https://console.anthropic.com/)
2. Sign up or log in to your account
3. Navigate to API Keys section
4. Create a new API key
5. Copy the API key

### 2. Configure Services

#### Python Claude microservice
1. Copy `claude-chat-service/.env.example` to `claude-chat-service/.env`
2. Set your Anthropic API key in `.env`:
```env
ANTHROPIC_API_KEY=sk-ant-your-actual-api-key-here
CLAUDE_MODEL=claude-3-5-sonnet-20241022
CLAUDE_MAX_TOKENS=1024
```

#### BetPortal API
Edit `BetPortalWebApi/appsettings.json` or `appsettings.Development.json`:
```json
{
  "ClaudeChatService": {
    "BaseUrl": "http://localhost:8000",
    "Model": "claude-3-5-sonnet-20241022",
    "MaxTokens": "1024",
    "TimeoutSeconds": "120"
  }
}
```

For production, you can override the service URL with environment variables:
```bash
ClaudeChatService__BaseUrl=http://claude-chat-service:8000
```

### 3. Build and Run

#### Claude chat microservice (Python)
```bash
cd claude-chat-service
python -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
copy .env.example .env
uvicorn app.main:app --reload --host 0.0.0.0 --port 8000
```

#### Backend
```bash
cd BetPortalWebApi
dotnet restore
dotnet build
dotnet run
```

#### Frontend
```bash
cd bet-portal-admin
npm install
npm start
```

### 4. Access the Chatbot
1. Log in to the admin portal
2. Click on "Advisor" in the navigation menu
3. Start asking questions!

## Usage Examples

### General Statistics
```
"What's the current state of the system?"
"Give me an overview of the data"
```

### Sports Queries
```
"How many sports are in the database?"
"List all active sports"
"What sports do we have?"
```

### User Management
```
"How many users are registered?"
"Show me recent users"
"What's the count of active users?"
```

### Roles and Permissions
```
"What roles exist in the system?"
"How many administrators do we have?"
"List all available roles"
```

## Architecture

### Frontend (Angular)
- **AdvisorComponent**: Main chat interface with message display and input
- **ChatService**: Handles API communication with backend
- **Material Design**: Modern UI with cards, icons, and animations

### Backend (C#/.NET)
- **ChatController**: API endpoint for chat requests
- **ChatService**: Builds prompts, fetches DB context, calls Python Claude microservice
- **DbQueryService**: Intelligently fetches relevant database information

### Claude Chat Microservice (Python/FastAPI)
- **FastAPI app**: Exposes `/api/chat` and `/health`
- **ClaudeService**: Calls Anthropic Claude Messages API
- **Configuration**: API key and model settings in `.env`

## API Endpoints

### POST /api/chat/ask
Request body:
```json
{
  "message": "How many sports are active?",
  "conversationHistory": [
    {
      "role": "user",
      "content": "Hello",
      "timestamp": "2024-01-01T12:00:00Z"
    },
    {
      "role": "assistant",
      "content": "Hi! How can I help you?",
      "timestamp": "2024-01-01T12:00:01Z"
    }
  ]
}
```

Response:
```json
{
  "content": "There are 15 active sports in the database...",
  "timestamp": "2024-01-01T12:00:05Z"
}
```

## Configuration Options

### Claude Chat Service Settings
- **BaseUrl**: URL of the Python microservice (default: `http://localhost:8000`)
- **Model**: Claude model to use (default: claude-3-5-sonnet-20241022)
- **MaxTokens**: Maximum response length (default: 1024)
- **TimeoutSeconds**: HTTP timeout for chat requests (default: 120)

Anthropic API credentials are configured in `claude-chat-service/.env`, not in the .NET API.

### Customization
To modify the AI's behavior, edit the system prompt in `ChatService.BuildSystemPrompt()`:
```csharp
private string BuildSystemPrompt(string dbContext)
{
    var prompt = new StringBuilder();
    prompt.AppendLine("You are an AI assistant for...");
    // Customize here
    return prompt.ToString();
}
```

## Security Considerations
- ✅ Authentication required (uses existing auth system)
- ✅ API key stored in configuration (not in code)
- ✅ Rate limiting via Claude API limits
- ✅ Database queries are read-only
- ⚠️ Consider implementing request throttling for production

## Troubleshooting

### "Claude chat service base URL is not configured"
- Ensure `ClaudeChatService:BaseUrl` is set in appsettings
- Verify the Python service is running on that URL

### "Error sending message"
- Check backend logs for detailed error information
- Verify the Python microservice is running (`GET /health`)
- Verify `ANTHROPIC_API_KEY` is set in `claude-chat-service/.env`
- Check network connectivity between BetPortal API and the Python service

### Empty responses
- Check if database has data
- Verify database connection string
- Review backend logs for query errors

## Future Enhancements
- [ ] Add support for charts and visualizations
- [ ] Implement message export functionality
- [ ] Add support for file attachments
- [ ] Create pre-defined question templates
- [ ] Add multi-language support
- [ ] Implement conversation saving/loading

## Cost Considerations
Claude API is a paid service. Monitor your usage at the Anthropic Console:
- Claude 3.5 Sonnet: ~$3 per million input tokens, ~$15 per million output tokens
- Typical question: 500-2000 tokens
- Consider implementing caching for common queries

## Support
For issues or questions, please contact the development team or create an issue in the project repository.
