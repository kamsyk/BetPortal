import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { API_BASE_URL } from '../../core/tokens';
import { ColumnState } from 'ag-grid-community';

export interface GridSettings {
  id: number;
  userId: number;
  pageIdentifier: string;
  settingsJson: string | null;
  modifiedDate: string;
}

export interface GridSettingsData {
  pageSize?: number;
  filterModel?: any;
  columnState?: ColumnState[];
}

export interface SaveGridSettingsRequest {
  pageIdentifier: string;
  settings: GridSettingsData;
}

@Injectable({
  providedIn: 'root',
})
export class GridSettingsService {
  private readonly http = inject(HttpClient);
  private readonly apiBase = inject(API_BASE_URL);
  private readonly settingsCache = new Map<string, GridSettingsData>();

  getSettings(pageIdentifier: string): Observable<GridSettings> {
    return this.http.get<GridSettings>(`${this.apiBase}/gridsettings/${pageIdentifier}`).pipe(
      tap((response) => {
        if (response.settingsJson) {
          const parsed = JSON.parse(response.settingsJson);
          this.settingsCache.set(pageIdentifier, parsed);
        }
      })
    );
  }

  saveSettings(pageIdentifier: string, settings: GridSettingsData): Observable<GridSettings> {
    const request: SaveGridSettingsRequest = {
      pageIdentifier,
      settings,
    };

    return this.http.post<GridSettings>(`${this.apiBase}/gridsettings`, request).pipe(
      tap(() => {
        this.settingsCache.set(pageIdentifier, settings);
      })
    );
  }

  deleteSettings(pageIdentifier: string): Observable<void> {
    return this.http.delete<void>(`${this.apiBase}/gridsettings/${pageIdentifier}`).pipe(
      tap(() => {
        this.settingsCache.delete(pageIdentifier);
      })
    );
  }

  getCachedSettings(pageIdentifier: string): GridSettingsData | undefined {
    return this.settingsCache.get(pageIdentifier);
  }
}
