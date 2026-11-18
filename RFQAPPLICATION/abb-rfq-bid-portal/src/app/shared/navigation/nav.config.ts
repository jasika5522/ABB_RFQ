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
    { label: 'My Bids', route: '/supplier/bids' },
    { label: 'My Profile', route: '/supplier/profile' }
  ],
  ProcurementOfficer: [
    { label: 'Dashboard', route: '/procurement/dashboard' },
    { label: 'Suppliers', route: '/procurement/suppliers' },
    { label: 'RFQ Management', route: '/procurement/rfqs' },
    { label: 'RFQ Rounds', route: '/procurement/rounds' },
    { label: 'Bid Evaluation', route: '/procurement/evaluations' },
    { label: 'Awards', route: '/procurement/awards' },
    { label: 'Audit Logs', route: '/procurement/audit-logs' }
  ],
  Approver: [
    { label: 'Dashboard', route: '/approver/dashboard' },
    { label: 'Suppliers', route: '/approver/suppliers' },
    { label: 'RFQs', route: '/approver/rfqs' },
    { label: 'Bid Evaluations', route: '/approver/evaluations' },
    { label: 'Evaluation Decisions', route: '/approver/decisions' },
    { label: 'Awards', route: '/approver/awards' },
    { label: 'Audit Logs', route: '/approver/audit-logs' }
  ]
};