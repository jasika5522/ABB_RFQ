export interface SupplierDetail {
  supplierId: number;
  userId: number;
  companyName: string;
  registrationNumber: string;
  contactPerson?: string | null;
  phone?: string | null;
  approvalStatus: string;
  createdAt: string;
  userFullName: string;
  userEmail: string;
  userIsActive: boolean;
  userCreatedAt: string;
}

export interface PagedSuppliers {
  items: SupplierDetail[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface SupplierListQuery {
  page?: number;
  pageSize?: number;
  approvalStatus?: string;
  companyName?: string;
}

export interface UpdateSupplierStatusRequest {
  approvalStatus: string;
}

export const SUPPLIER_APPROVAL_STATUSES = ['Pending', 'Approved', 'Suspended'] as const;