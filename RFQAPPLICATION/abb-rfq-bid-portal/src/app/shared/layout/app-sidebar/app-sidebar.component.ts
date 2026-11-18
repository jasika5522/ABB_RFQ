import { Component, Input, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NavItem } from '../../navigation/nav.config';
import { AuthService } from '../../../core/services/auth.service';
import { UserRole } from '../../../core/models/user.model';
import { NAV_BY_ROLE } from '../../navigation/nav.config';

@Component({
  selector: 'app-app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './app-sidebar.component.html',
  styleUrl: './app-sidebar.component.css'
})
export class AppSidebarComponent {
  private readonly auth = inject(AuthService);

  @Input() open = true;

  get navItems(): NavItem[] {
    const role = this.auth.getCurrentUser()?.role as UserRole | undefined;
    return role ? NAV_BY_ROLE[role] : [];
  }
}