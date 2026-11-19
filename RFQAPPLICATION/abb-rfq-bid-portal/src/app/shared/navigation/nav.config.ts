import { UserRole } from '../../core/models/user.model';

export interface NavItem {
  label: string;
  route: string;
  icon?: string;
}

export const NAV_BY_ROLE: Record<UserRole, NavItem[]> = {
  Supplier: [
    { label: 'Dashboard', route: '/supplier/dashboard' },
    { label: 'Available RFQs', route: '/supplier/rfqs' },
    { label: 'My Bids', route: '/supplier/my-bids' },
    { label: 'My Profile', route: '/supplier/profile' }
  ],
  ProcurementOfficer: [
    { label: 'Dashboard', route: '/procurement/dashboard' },
    { label: 'Supplier Management', route: '/procurement/suppliers' },
    { label: 'RFQ Management', route: '/procurement/rfqs' },
    { label: 'Create RFQ', route: '/procurement/rfqs/create' },
    { label: 'Bid Evaluation', route: '/procurement/evaluations' },
    { label: 'Audit Logs', route: '/audit-logs' }
  ],
  Approver: [
    { label: 'Dashboard', route: '/approver/dashboard' },
    { label: 'Suppliers', route: '/approver/suppliers' },
    { label: 'Evaluation Review', route: '/approver/evaluations' },
    { label: 'Award Management', route: '/approver/awards' },
    { label: 'Audit Logs', route: '/audit-logs' }
  ]
};