import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_BASE_URL } from '../../core/tokens';
import { Observable } from 'rxjs';

export interface ChatAskResponse {
  answer: string;
  sources: string[];
}

export interface ChatReindexResponse {
  success: boolean;
  indexedItems: number;
  message: string;
}

@Injectable({
  providedIn: 'root',
})
export class ChatbotService {
  private readonly http = inject(HttpClient);
  private readonly apiBase = inject(API_BASE_URL);

  ask(message: string): Observable<ChatAskResponse> {
    return this.http.post<ChatAskResponse>(`${this.apiBase}/chat/ask`, { message });
  }

  reindex(): Observable<ChatReindexResponse> {
    return this.http.post<ChatReindexResponse>(`${this.apiBase}/chat/reindex`, {});
  }
}
