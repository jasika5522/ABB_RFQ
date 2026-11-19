import { Component } from '@angular/core';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { NavCardComponent } from '../../../shared/components/nav-card/nav-card.component';

@Component({
  selector: 'app-procurement-dashboard',
  standalone: true,
  imports: [PageHeaderComponent, NavCardComponent],
  templateUrl: './procurement-dashboard.component.html',
  styleUrl: './procurement-dashboard.component.css'
})
export class ProcurementDashboardComponent {
  readonly links = [
    {
      title: 'Supplier management',
      description: 'Review registrations and approve or suspend suppliers.',
      route: '/procurement/suppliers'
    },
    {
      title: 'RFQ management',
      description: 'View and manage draft and published RFQs.',
      route: '/procurement/rfqs'
    },
    {
      title: 'Create RFQ',
      description: 'Start a new draft RFQ with line items.',
      route: '/procurement/rfqs/create'
    },
    {
      title: 'Bid evaluation',
      description: 'Evaluate closed rounds and view rankings.',
      route: '/procurement/evaluations'
    },
    {
      title: 'Audit logs',
      description: 'Review system audit history.',
      route: '/audit-logs'
    }
  ];
}