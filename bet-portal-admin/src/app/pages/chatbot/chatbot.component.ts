import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
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
        this.messages.update((items) => [
          ...items,
          {
            role: 'assistant',
            text: response.answer,
            sources: response.sources,
          },
        ]);
        this.loading.set(false);
      },
      error: () => {
        this.messages.update((items) => [
          ...items,
          {
            role: 'assistant',
            text: 'The chatbot is unavailable. Verify API and Ollama are running.',
          },
        ]);
        this.loading.set(false);
      },
    });
  }

  runReindex(): void {
    if (this.reindexing()) {
      return;
    }

    this.reindexStatus.set('');
    this.reindexing.set(true);

    this.chatbotService.reindex().subscribe({
      next: (response) => {
        this.reindexStatus.set(response.message);
        this.reindexing.set(false);
      },
      error: () => {
        this.reindexStatus.set('Reindex failed. Verify Qdrant/Ollama and AdminAccess policy.');
        this.reindexing.set(false);
      },
    });
  }
}
