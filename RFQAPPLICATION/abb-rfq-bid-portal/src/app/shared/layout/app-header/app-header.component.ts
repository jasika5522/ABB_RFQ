import { Component, Input, inject } from '@angular/core';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-app-header',
  standalone: true,
  templateUrl: './app-header.component.html',
  styleUrl: './app-header.component.css'
})
export class AppHeaderComponent {
  private readonly auth = inject(AuthService);

  @Input() userName = '';
  @Input() userRole = '';
  @Input() sidebarOpen = false;
  @Input() toggleSidebar?: () => void;

  logout(): void {
    this.auth.logout();
  }
}