import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-home-redirect',
  standalone: true,
  template: ''
})
export class HomeRedirectComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  ngOnInit(): void {
    const user = this.auth.getCurrentUser();
    if (this.auth.isAuthenticated() && user) {
      void this.router.navigateByUrl(this.auth.getDashboardRouteForRole(user.role));
      return;
    }
    void this.router.navigate(['/login']);
  }
}