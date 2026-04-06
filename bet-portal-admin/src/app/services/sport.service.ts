import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/tokens';

export interface Sport {
  id: number;
  name: string;
  active: boolean;
}

interface CreateSportRequest {
  name: string;
  active: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class SportService {
  private readonly http = inject(HttpClient);
  private readonly apiBase = inject(API_BASE_URL);

  getSports(): Observable<Sport[]> {
    return this.http.get<Sport[]>(`${this.apiBase}/sport/list`);
  }

  createSport(payload: CreateSportRequest): Observable<Sport> {
    return this.http.post<Sport>(`${this.apiBase}/sport/add`, payload);
  }
}
