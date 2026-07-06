from pydantic import BaseModel, Field


class ChatMessage(BaseModel):
    role: str
    content: str


class ChatRequest(BaseModel):
    system_prompt: str
    messages: list[ChatMessage]
    model: str | None = None
    max_tokens: int | None = Field(default=None, ge=1, le=8192)


class ChatResponse(BaseModel):
    content: str


class HealthResponse(BaseModel):
    status: str
