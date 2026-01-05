export interface User {
  id: number;
  email: string;
  userName: string;
  role: UserRole;
  createdAt: Date;
  lastLoginAt?: Date;
}

export enum UserRole {
  Customer = 'Customer',
  Admin = 'Admin'
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  userName: string;
  password: string;
  confirmPassword: string;
}

export interface AuthResponse {
  userId: number;
  email: string;
  userName: string;
  role: string;
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface RefreshTokenRequest {
  accessToken: string;
  refreshToken: string;
}
