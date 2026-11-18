import { UserRole } from '../models/user.model';

export const ROLES = {
  Supplier: 'Supplier' as UserRole,
  ProcurementOfficer: 'ProcurementOfficer' as UserRole,
  Approver: 'Approver' as UserRole
};

export const ROLE_DASHBOARD: Record<UserRole, string> = {
  Supplier: '/supplier/dashboard',
  ProcurementOfficer: '/procurement/dashboard',
  Approver: '/approver/dashboard'
};