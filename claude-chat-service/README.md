# Claude Chat Service

Python microservice that wraps the Anthropic Claude Messages API for the BetPortal chatbot.

## Setup

```bash
cd claude-chat-service
python -m venv .venv

# Windows
.venv\Scripts\activate

pip install -r requirements.txt
copy .env.example .env
```

Edit `.env` and set your `ANTHROPIC_API_KEY`.

## Run

```bash
uvicorn app.main:app --reload --host 0.0.0.0 --port 8000
```

## API

### `GET /health`

Returns service health status.

### `POST /api/chat`

Request:

```json
{
  "system_prompt": "You are a helpful assistant.",
  "messages": [
    { "role": "user", "content": "Hello" }
  ],
  "model": "claude-3-5-sonnet-20241022",
  "max_tokens": 1024
}
```

Response:

```json
{
  "content": "Hello! How can I help you today?"
}
```

## Docker

```bash
docker build -t claude-chat-service .
docker run --env-file .env -p 8000:8000 claude-chat-service
```
