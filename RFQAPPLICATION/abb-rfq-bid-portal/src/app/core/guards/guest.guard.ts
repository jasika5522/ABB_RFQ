import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    return true;
  }

  const user = auth.getCurrentUser();
  return router.createUrlTree([
    user ? auth.getDashboardRouteForRole(user.role) : '/login'
  ]);
};