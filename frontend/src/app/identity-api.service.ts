import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiConfigurationService } from './api-configuration.service';

export interface AuthResponse {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  token: string;
  expiresAtUtc: string;
  roles: string[];
}

export interface UserProfile {
  userId: string;
  email: string;
  userName: string;
  firstName: string;
  lastName: string;
  phoneNumber: string | null;
}

export type UserProfileUpdate = Pick<
  UserProfile,
  'firstName' | 'lastName' | 'userName' | 'phoneNumber'
>;

@Injectable({ providedIn: 'root' })
export class IdentityApiService {
  private readonly http = inject(HttpClient);
  private readonly api = inject(ApiConfigurationService);

  register(
    firstName: string,
    lastName: string,
    userName: string,
    email: string,
    password: string,
  ): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.api.identityApiUrl}/register`, {
      firstName,
      lastName,
      userName,
      email,
      password,
    });
  }

  login(login: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.api.identityApiUrl}/login`, { login, password });
  }

  getUserProfile(token: string): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.api.identityApiUrl}/me`, {
      headers: this.auth(token),
    });
  }

  updateUserProfile(profile: UserProfileUpdate, token: string): Observable<UserProfile> {
    return this.http.put<UserProfile>(`${this.api.identityApiUrl}/me`, profile, {
      headers: this.auth(token),
    });
  }

  private auth(token: string): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }
}
