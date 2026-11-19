import { Component } from '@angular/core';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { NavCardComponent } from '../../../shared/components/nav-card/nav-card.component';

@Component({
  selector: 'app-approver-dashboard',
  standalone: true,
  imports: [PageHeaderComponent, NavCardComponent],
  templateUrl: './approver-dashboard.component.html',
  styleUrl: './approver-dashboard.component.css'
})
export class ApproverDashboardComponent {
  readonly links = [
    {
      title: 'Suppliers',
      description: 'View registered suppliers and approval status (read-only).',
      route: '/approver/suppliers'
    },
    {
      title: 'Evaluation review',
      description: 'Approve or reject evaluated RFQ rounds.',
      route: '/approver/evaluations'
    },
    {
      title: 'Award management',
      description: 'View awards and create awards after approval.',
      route: '/approver/awards'
    },
    {
      title: 'Audit logs',
      description: 'Trace procurement actions and decisions.',
      route: '/audit-logs'
    }
  ];
}