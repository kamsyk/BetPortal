import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class TmpAuthService {
  //TODO: KSY - move this method to KsControls AuthService when done testing
  private readonly baseUrl = 'http://localhost:5026/api/auth'; // .NET API server URL
  private http = inject(HttpClient);

  /**
   * Logout the current user by calling the API logout endpoint
   * @returns Observable with the logout response
   */
  logout(): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(`${this.baseUrl}/logout`, {});
  }
}
