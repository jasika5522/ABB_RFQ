import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { UserRole } from '../models/user.model';

export const roleGuard = (allowedRoles: UserRole[]): CanActivateFn => () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const user = auth.getCurrentUser();

  if (!user) {
    return router.createUrlTree(['/login']);
  }

  if (allowedRoles.includes(user.role)) {
    return true;
  }

  return router.createUrlTree([auth.getDashboardRouteForRole(user.role)]);
};