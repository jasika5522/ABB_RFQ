import { Component, inject } from '@angular/core';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-supplier-profile',
  standalone: true,
  imports: [PageHeaderComponent, EmptyStateComponent],
  templateUrl: './supplier-profile.component.html',
  styleUrl: './supplier-profile.component.css'
})
export class SupplierProfileComponent {
  private readonly auth = inject(AuthService);
  readonly user = this.auth.getCurrentUser();
}