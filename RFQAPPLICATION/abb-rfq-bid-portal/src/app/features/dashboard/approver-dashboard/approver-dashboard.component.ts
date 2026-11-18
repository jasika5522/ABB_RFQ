import { Component } from '@angular/core';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatCardComponent } from '../../../shared/components/stat-card/stat-card.component';
import { APPROVER_DASHBOARD_STATS } from './approver-dashboard.placeholder';

@Component({
  selector: 'app-approver-dashboard',
  standalone: true,
  imports: [PageHeaderComponent, StatCardComponent],
  templateUrl: './approver-dashboard.component.html',
  styleUrl: './approver-dashboard.component.css'
})
export class ApproverDashboardComponent {
  readonly stats = APPROVER_DASHBOARD_STATS;
}