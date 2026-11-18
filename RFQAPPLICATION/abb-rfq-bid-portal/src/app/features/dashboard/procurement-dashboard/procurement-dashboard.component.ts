import { Component } from '@angular/core';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatCardComponent } from '../../../shared/components/stat-card/stat-card.component';
import { PROCUREMENT_DASHBOARD_STATS } from './procurement-dashboard.placeholder';

@Component({
  selector: 'app-procurement-dashboard',
  standalone: true,
  imports: [PageHeaderComponent, StatCardComponent],
  templateUrl: './procurement-dashboard.component.html',
  styleUrl: './procurement-dashboard.component.css'
})
export class ProcurementDashboardComponent {
  readonly stats = PROCUREMENT_DASHBOARD_STATS;
}