import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { Router } from '@angular/router';
import { TokenService } from './token.service';
import { 
  LoginRequest, 
  RegisterRequest, 
  AuthResponse, 
  User,
  RefreshTokenRequest 
} from '../models/user.model';
import { environment } from '../../../environments/environment.development';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  private apiUrl = environment.apiUrl;

  constructor(
    private http: HttpClient,
    private tokenService: TokenService,
    private router: Router
  ) {
    this.checkAuthStatus();
  }

  private checkAuthStatus(): void {
    const token = this.tokenService.getAccessToken();
    const userData = this.tokenService.getUserData();

    if (token && userData && !this.tokenService.isTokenExpired()) {
      this.currentUserSubject.next(userData);
      this.isAuthenticatedSubject.next(true);
    } else {
      this.logout();
    }
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/auth/register`, request).pipe(
      tap(response => this.handleAuthResponse(response))
    );
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/auth/login`, request).pipe(
      tap(response => this.handleAuthResponse(response))
    );
  }

  logout(): void {
    const token = this.tokenService.getAccessToken();
    
    if (token) {
      this.http.post(`${this.apiUrl}/auth/logout`, {}).subscribe({
        next: () => {
          this.clearAuthState();
        },
        error: () => {
          // 오류가 발생해도 로컬 상태는 지웁니다
          this.clearAuthState();
        }
      });
    } else {
      this.clearAuthState();
    }
  }

  refreshToken(): Observable<AuthResponse> {
    const accessToken = this.tokenService.getAccessToken();
    const refreshToken = this.tokenService.getRefreshToken();

    if (!accessToken || !refreshToken) {
      throw new Error('No tokens available');
    }

    const request: RefreshTokenRequest = {
      accessToken,
      refreshToken
    };

    return this.http.post<AuthResponse>(`${this.apiUrl}/auth/refresh`, request).pipe(
      tap(response => this.handleAuthResponse(response))
    );
  }

  getCurrentUser(): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/auth/me`).pipe(
      tap(user => {
        this.currentUserSubject.next(user);
        this.tokenService.setUserData(user);
      })
    );
  }

  isAuthenticated(): boolean {
    return this.isAuthenticatedSubject.value;
  }

  getAccessToken(): string | null {
    return this.tokenService.getAccessToken();
  }

  private handleAuthResponse(response: AuthResponse): void {
    this.tokenService.setTokens(
      response.accessToken,
      response.refreshToken,
      response.expiresAt
    );

    const user: User = {
      id: response.userId,
      email: response.email,
      userName: response.userName,
      role: response.role as any,
      createdAt: new Date()
    };

    this.tokenService.setUserData(user);
    this.currentUserSubject.next(user);
    this.isAuthenticatedSubject.next(true);
  }

  private clearAuthState(): void {
    this.tokenService.clearTokens();
    this.currentUserSubject.next(null);
    this.isAuthenticatedSubject.next(false);
    this.router.navigate(['/auth/login']);
  }
}
