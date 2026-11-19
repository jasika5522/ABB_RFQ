import { SupplierResponse } from './user.model';

export interface UpdateSupplierProfileRequest {
  companyName: string;
  contactPerson?: string | null;
  phone?: string | null;
}

export type { SupplierResponse };