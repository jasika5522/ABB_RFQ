import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { AuditLogService } from '../../core/services/audit-log.service';
import { AuditLog } from '../../core/models/audit-log.model';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-audit-log-list',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, DatePipe, PageHeaderComponent, EmptyStateComponent],
  templateUrl: './audit-log-list.component.html',
  styleUrl: './audit-log-list.component.css'
})
export class AuditLogListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auditService = inject(AuditLogService);
  private readonly auth = inject(AuthService);

  loading = false;
  loadError = '';
  items: AuditLog[] = [];
  page = 1;
  pageSize = 15;
  totalPages = 1;
  totalCount = 0;

  readonly filters = this.fb.nonNullable.group({
    action: [''],
    entityType: [''],
    entityId: [''],
    userId: [''],
    fromDate: [''],
    toDate: ['']
  });

  ngOnInit(): void {
    this.loadPage(1);
  }

  loadPage(page: number): void {
    this.page = page;
    this.loading = true;
    this.loadError = '';
    const raw = this.filters.getRawValue();
    const entityId = raw.entityId ? Number(raw.entityId) : undefined;
    const userId = raw.userId ? Number(raw.userId) : undefined;

    this.auditService
      .list({
        page,
        pageSize: this.pageSize,
        action: raw.action || undefined,
        entityType: raw.entityType || undefined,
        entityId: entityId && !Number.isNaN(entityId) ? entityId : undefined,
        userId: userId && !Number.isNaN(userId) ? userId : undefined,
        fromDate: raw.fromDate ? new Date(raw.fromDate).toISOString() : undefined,
        toDate: raw.toDate ? new Date(raw.toDate).toISOString() : undefined
      })
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (result) => {
          this.items = result.items;
          this.totalCount = result.totalCount;
          this.totalPages = result.totalPages;
          this.page = result.page;
        },
        error: (error: HttpErrorResponse) => {
          if (error.status === 401) {
            this.auth.logout();
            return;
          }
          this.loadError = error.error?.message ?? 'Unable to load audit logs.';
        }
      });
  }

  applyFilters(): void {
    this.loadPage(1);
  }

  clearFilters(): void {
    this.filters.reset({
      action: '',
      entityType: '',
      entityId: '',
      userId: '',
      fromDate: '',
      toDate: ''
    });
    this.loadPage(1);
  }

  actorName(log: AuditLog): string {
    return log.actor?.fullName ?? (log.actor?.email ? log.actor.email : '—');
  }
}