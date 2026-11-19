import { Component } from '@angular/core';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { NavCardComponent } from '../../../shared/components/nav-card/nav-card.component';

@Component({
  selector: 'app-supplier-dashboard',
  standalone: true,
  imports: [PageHeaderComponent, NavCardComponent],
  templateUrl: './supplier-dashboard.component.html',
  styleUrl: './supplier-dashboard.component.css'
})
export class SupplierDashboardComponent {
  readonly links = [
    {
      title: 'My profile',
      description: 'Update company details and contact information.',
      route: '/supplier/profile'
    },
    {
      title: 'Available RFQs',
      description: 'Browse published RFQs with open bidding rounds.',
      route: '/supplier/rfqs'
    },
    {
      title: 'My bids',
      description: 'View submitted bids, totals, and withdrawal options.',
      route: '/supplier/my-bids'
    }
  ];
}