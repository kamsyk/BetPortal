import { Component, inject, signal, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ChatService, ChatMessage } from '../../services/chat.service';

@Component({
  selector: 'app-advisor',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    TranslatePipe,
  ],
  templateUrl: './advisor.component.html',
  styleUrls: ['./advisor.component.scss']
})
export class AdvisorComponent implements AfterViewChecked {
  private readonly chatService = inject(ChatService);
  private readonly translate = inject(TranslateService);

  @ViewChild('messagesContainer') private messagesContainer!: ElementRef;

  messages = signal<ChatMessage[]>([]);
  userInput = signal<string>('');
  isLoading = signal<boolean>(false);
  private shouldScrollToBottom = false;

  ngAfterViewChecked() {
    if (this.shouldScrollToBottom) {
      this.scrollToBottom();
      this.shouldScrollToBottom = false;
    }
  }

  sendMessage(): void {
    const message = this.userInput().trim();
    if (!message || this.isLoading()) {
      return;
    }

    // Add user message
    const userMessage: ChatMessage = {
      role: 'user',
      content: message,
      timestamp: new Date()
    };

    this.messages.update(msgs => [...msgs, userMessage]);
    this.userInput.set('');
    this.isLoading.set(true);
    this.shouldScrollToBottom = true;

    // Send to backend and get response
    this.chatService.sendMessage(message, this.messages()).subscribe({
      next: (response) => {
        const assistantMessage: ChatMessage = {
          role: 'assistant',
          content: response.content,
          timestamp: new Date()
        };
        this.messages.update(msgs => [...msgs, assistantMessage]);
        this.isLoading.set(false);
        this.shouldScrollToBottom = true;
      },
      error: (error) => {
        console.error('Error sending message:', error);
        const errorMessage: ChatMessage = {
          role: 'assistant',
          content: this.translate.instant('advisor.errorMessage'),
          timestamp: new Date()
        };
        this.messages.update(msgs => [...msgs, errorMessage]);
        this.isLoading.set(false);
        this.shouldScrollToBottom = true;
      }
    });
  }

  clearChat(): void {
    this.messages.set([]);
    this.userInput.set('');
  }

  private scrollToBottom(): void {
    try {
      if (this.messagesContainer) {
        const element = this.messagesContainer.nativeElement;
        element.scrollTop = element.scrollHeight;
      }
    } catch (err) {
      console.error('Error scrolling to bottom:', err);
    }
  }

  onKeyPress(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }
}
