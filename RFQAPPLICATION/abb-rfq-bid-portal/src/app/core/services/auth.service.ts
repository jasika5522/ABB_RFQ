import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AuthSession,
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  RegistrationResponse,
  UserResponse
} from '../models/user.model';
import { ROLE_DASHBOARD } from '../constants/roles';

const SESSION_KEY = 'abb_rfq_auth_session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly currentUserSubject = new BehaviorSubject<UserResponse | null>(this.loadUserFromStorage());
  readonly currentUser$ = this.currentUserSubject.asObservable();

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${environment.apiBaseUrl}/auth/login`, request)
      .pipe(tap((response) => this.persistSession(response)));
  }

  register(request: RegisterRequest): Observable<RegistrationResponse> {
    return this.http.post<RegistrationResponse>(`${environment.apiBaseUrl}/auth/register`, request);
  }

  fetchCurrentUser(): Observable<UserResponse> {
    return this.http.get<UserResponse>(`${environment.apiBaseUrl}/users/me`).pipe(
      tap((user) => {
        this.currentUserSubject.next(user);
        this.updateStoredUser(user);
      })
    );
  }

  logout(): void {
    sessionStorage.removeItem(SESSION_KEY);
    this.currentUserSubject.next(null);
    void this.router.navigate(['/login']);
  }

  isAuthenticated(): boolean {
    const session = this.getSession();
    if (!session?.token) {
      return false;
    }
    return new Date(session.expiresAt) > new Date();
  }

  getToken(): string | null {
    return this.getSession()?.token ?? null;
  }

  getCurrentUser(): UserResponse | null {
    return this.currentUserSubject.value;
  }

  getDashboardRouteForRole(role: string): string {
    return ROLE_DASHBOARD[role as keyof typeof ROLE_DASHBOARD] ?? '/login';
  }

  private persistSession(response: LoginResponse): void {
    const session: AuthSession = {
      token: response.token,
      expiresAt: response.expiresAt,
      user: response.user
    };
    sessionStorage.setItem(SESSION_KEY, JSON.stringify(session));
    this.currentUserSubject.next(response.user);
  }

  private getSession(): AuthSession | null {
    const raw = sessionStorage.getItem(SESSION_KEY);
    if (!raw) {
      return null;
    }
    try {
      return JSON.parse(raw) as AuthSession;
    } catch {
      return null;
    }
  }

  private loadUserFromStorage(): UserResponse | null {
    return this.getSession()?.user ?? null;
  }

  private updateStoredUser(user: UserResponse): void {
    const session = this.getSession();
    if (!session) {
      return;
    }
    session.user = user;
    sessionStorage.setItem(SESSION_KEY, JSON.stringify(session));
  }
}