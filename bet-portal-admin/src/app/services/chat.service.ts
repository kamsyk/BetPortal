import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/tokens';

export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  timestamp: Date;
}

export interface ChatRequest {
  message: string;
  conversationHistory: ChatMessage[];
}

export interface ChatResponse {
  content: string;
  timestamp: string;
}

@Injectable({
  providedIn: 'root',
})
export class ChatService {
  private readonly http = inject(HttpClient);
  private readonly apiBase = inject(API_BASE_URL);

  sendMessage(message: string, conversationHistory: ChatMessage[]): Observable<ChatResponse> {
    const request: ChatRequest = {
      message,
      conversationHistory: conversationHistory.slice(-10) // Keep last 10 messages for context
    };

    return this.http.post<ChatResponse>(`${this.apiBase}/chat/ask`, request);
  }
}
