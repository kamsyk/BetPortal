import logging

from anthropic import Anthropic

from app.config import settings
from app.schemas import ChatRequest, ChatResponse

logger = logging.getLogger(__name__)


class ClaudeService:
    def __init__(self) -> None:
        self._client = Anthropic(api_key=settings.anthropic_api_key)

    def create_chat_completion(self, request: ChatRequest) -> ChatResponse:
        model = request.model or settings.claude_model
        max_tokens = request.max_tokens or settings.claude_max_tokens

        logger.info(
            "Calling Claude API model=%s max_tokens=%s message_count=%s",
            model,
            max_tokens,
            len(request.messages),
        )

        response = self._client.messages.create(
            model=model,
            max_tokens=max_tokens,
            system=request.system_prompt,
            messages=[
                {"role": message.role, "content": message.content}
                for message in request.messages
            ],
        )

        text_blocks = [
            block.text
            for block in response.content
            if getattr(block, "type", None) == "text" and block.text
        ]

        content = "\n".join(text_blocks).strip()
        if not content:
            content = "I'm sorry, I couldn't generate a response."

        return ChatResponse(content=content)
