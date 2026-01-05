import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { TokenService } from '../services/token.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenService = inject(TokenService);
  const token = tokenService.getAccessToken();

  // SignalR 요청은 제외
  if (req.url.includes('/hubs/')) {
    return next(req);
  }

  // 인증 API 요청은 제외 (로그인, 회원가입)
  if (req.url.includes('/auth/login') || req.url.includes('/auth/register')) {
    return next(req);
  }

  // 토큰이 있으면 Authorization 헤더 추가
  if (token && !tokenService.isTokenExpired()) {
    const clonedReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
    return next(clonedReq);
  }

  return next(req);
};
