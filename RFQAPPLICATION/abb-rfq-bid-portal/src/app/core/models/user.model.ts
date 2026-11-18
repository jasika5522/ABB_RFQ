export type UserRole = 'Supplier' | 'ProcurementOfficer' | 'Approver';

export interface UserResponse {
  userId: number;
  fullName: string;
  email: string;
  role: UserRole;
  isActive: boolean;
  createdAt: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  user: UserResponse;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  companyName: string;
  registrationNumber: string;
  contactPerson?: string | null;
  phone?: string | null;
}

export interface SupplierResponse {
  supplierId: number;
  userId: number;
  companyName: string;
  registrationNumber: string;
  contactPerson?: string | null;
  phone?: string | null;
  approvalStatus: string;
  createdAt: string;
}

export interface RegistrationResponse {
  user: UserResponse;
  supplier: SupplierResponse;
}

export interface AuthSession {
  token: string;
  expiresAt: string;
  user: UserResponse;
}