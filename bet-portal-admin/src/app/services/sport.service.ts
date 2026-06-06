import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
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

interface UpdateSportRequest {
  name: string;
  active: boolean;
}

export interface SportListQuery {
  name?: string;
  active?: boolean;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}

@Injectable({
  providedIn: 'root',
})
export class SportService {
  private readonly http = inject(HttpClient);
  private readonly apiBase = inject(API_BASE_URL);

  getSports(query?: SportListQuery): Observable<Sport[]> {
    let params = new HttpParams();

    if (query?.name) {
      params = params.set('name', query.name);
    }

    if (query?.active !== undefined) {
      params = params.set('active', query.active);
    }

    if (query?.sortBy) {
      params = params.set('sortBy', query.sortBy);
    }

    if (query?.sortOrder) {
      params = params.set('sortOrder', query.sortOrder);
    }

    return this.http.get<Sport[]>(`${this.apiBase}/sport/list`, { params });
  }

  createSport(payload: CreateSportRequest): Observable<Sport> {
    return this.http.post<Sport>(`${this.apiBase}/sport/add`, payload);
  }

  updateSport(id: number, payload: UpdateSportRequest): Observable<Sport> {
    return this.http.put<Sport>(`${this.apiBase}/sport/${id}`, payload);
  }

  deleteSport(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiBase}/sport/${id}`);
  }
}
