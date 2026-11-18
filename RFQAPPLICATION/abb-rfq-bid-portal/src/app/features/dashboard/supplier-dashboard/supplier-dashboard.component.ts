import { Component } from '@angular/core';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatCardComponent } from '../../../shared/components/stat-card/stat-card.component';
import { SUPPLIER_DASHBOARD_STATS } from './supplier-dashboard.placeholder';

@Component({
  selector: 'app-supplier-dashboard',
  standalone: true,
  imports: [PageHeaderComponent, StatCardComponent],
  templateUrl: './supplier-dashboard.component.html',
  styleUrl: './supplier-dashboard.component.css'
})
export class SupplierDashboardComponent {
  readonly stats = SUPPLIER_DASHBOARD_STATS;
}