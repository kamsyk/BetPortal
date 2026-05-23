import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { ChatbotService } from '../../services/chatbot.service';

interface ChatMessage {
  role: 'user' | 'assistant';
  text: string;
  sources?: string[];
}

@Component({
  selector: 'app-chatbot',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chatbot.component.html',
  styleUrl: './chatbot.component.scss',
})
export class ChatbotComponent {
  private readonly chatbotService = inject(ChatbotService);

  readonly draft = signal('');
  readonly loading = signal(false);
  readonly reindexing = signal(false);
  readonly reindexStatus = signal('');
  readonly messages = signal<ChatMessage[]>([
    {
      role: 'assistant',
      text: 'Hi, I am your BetPortal advisor. Ask me about Sports data in the local database.',
    },
  ]);

  sendMessage(): void {
    const question = this.draft().trim();
    if (!question || this.loading()) {
      return;
    }

    this.messages.update((items) => [...items, { role: 'user', text: question }]);
    this.draft.set('');
    this.loading.set(true);

    this.chatbotService.ask(question).subscribe({
      next: (response) => {
        const answerText = response.success
          ? response.answer
          : response.message || response.answer || 'Chat request failed.';

        this.messages.update((items) => [
          ...items,
          {
            role: 'assistant',
            text: answerText,
            sources: response.sources,
          },
        ]);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.messages.update((items) => [
          ...items,
          {
            role: 'assistant',
            text: this.mapChatErrorMessage(error),
          },
        ]);
        this.loading.set(false);
      },
    });
  }

  private mapChatErrorMessage(error: unknown): string {
    if (!(error instanceof HttpErrorResponse)) {
      return 'The chatbot is unavailable due to an unexpected client error.';
    }

    if (error.status === 401) {
      return 'Your session expired. Please log in again and retry.';
    }

    if (error.status === 403) {
      return 'Access denied for chatbot request. Verify your user permissions.';
    }

    if (error.status === 0) {
      return 'API is unreachable. Verify BetPortalWebApi is running.';
    }

    const message = (error.error as { message?: string } | null)?.message;
    if (typeof message === 'string' && message.trim().length > 0) {
      return message;
    }

    return `Chat request failed with HTTP ${error.status}.`;
  }

  runReindex(): void {
    if (this.reindexing()) {
      return;
    }

    this.reindexStatus.set('');
    this.reindexing.set(true);

    this.chatbotService.reindex().subscribe({
      next: (response) => {
        const statusText = response.success
          ? response.message
          : response.message || 'Reindex failed.';

        this.reindexStatus.set(statusText);
        this.reindexing.set(false);
      },
      error: (error: unknown) => {
        this.reindexStatus.set(this.mapReindexErrorMessage(error));
        this.reindexing.set(false);
      },
    });
  }

  private mapReindexErrorMessage(error: unknown): string {
    if (!(error instanceof HttpErrorResponse)) {
      return 'Reindex failed due to an unexpected client error.';
    }

    if (error.status === 401) {
      return 'Your session expired. Please log in again, then retry reindex.';
    }

    if (error.status === 403) {
      return 'Access denied. Reindex requires AdminAccess permission.';
    }

    if (error.status === 0) {
      return 'API is unreachable. Verify BetPortalWebApi is running.';
    }

    const message = (error.error as { message?: string } | null)?.message;
    if (typeof message === 'string' && message.trim().length > 0) {
      return message;
    }

    return `Reindex failed with HTTP ${error.status}.`;
  }
}
