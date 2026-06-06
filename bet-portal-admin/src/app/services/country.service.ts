import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../core/tokens';

export interface Country {
  id: number;
  name: string;
  active: boolean;
}

interface CreateCountryRequest {
  name: string;
  active: boolean;
}

export interface CountryListQuery {
  name?: string;
  active?: boolean;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}

@Injectable({
  providedIn: 'root',
})
export class CountryService {
  private readonly http = inject(HttpClient);
  private readonly apiBase = inject(API_BASE_URL);

  getCountries(query?: CountryListQuery): Observable<Country[]> {
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

    return this.http.get<Country[]>(`${this.apiBase}/country/list`, { params });
  }

  createCountry(payload: CreateCountryRequest): Observable<Country> {
    return this.http.post<Country>(`${this.apiBase}/country/add`, payload);
  }
}
