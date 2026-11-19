import { AsyncPipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AppHeaderComponent } from '../app-header/app-header.component';
import { AppSidebarComponent } from '../app-sidebar/app-sidebar.component';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [RouterOutlet, AppHeaderComponent, AppSidebarComponent, AsyncPipe],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.css'
})
export class MainLayoutComponent implements OnInit {
  private readonly auth = inject(AuthService);
  sidebarOpen = false;

  readonly currentUser$ = this.auth.currentUser$;

  ngOnInit(): void {
    if (!this.auth.isAuthenticated()) {
      return;
    }

    this.auth.fetchCurrentUser().subscribe({
      error: () => this.auth.logout()
    });
  }

  toggleSidebar(): void {
    this.sidebarOpen = !this.sidebarOpen;
  }
}