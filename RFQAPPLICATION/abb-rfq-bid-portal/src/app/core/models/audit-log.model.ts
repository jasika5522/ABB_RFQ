export interface AuditActor {
  userId: number;
  fullName: string;
  email: string;
  role: string;
}

export interface AuditLog {
  auditId: number;
  action: string;
  entityType: string;
  entityId: number;
  oldValues?: unknown;
  newValues?: unknown;
  ipAddress?: string | null;
  createdAt: string;
  actor?: AuditActor | null;
}

export interface PagedAuditLogs {
  items: AuditLog[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AuditLogQuery {
  page?: number;
  pageSize?: number;
  action?: string;
  entityType?: string;
  entityId?: number;
  userId?: number;
  fromDate?: string;
  toDate?: string;
}