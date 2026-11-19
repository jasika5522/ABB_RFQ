import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { AuditLogService } from '../../core/services/audit-log.service';
import { AuthService } from '../../core/services/auth.service';
import { AuditLog } from '../../core/models/audit-log.model';
import { formatAuditJson } from '../../core/utils/audit-json.util';

@Component({
  selector: 'app-audit-log-detail',
  standalone: true,
  imports: [RouterLink, DatePipe, PageHeaderComponent],
  templateUrl: './audit-log-detail.component.html',
  styleUrl: './audit-log-detail.component.css'
})
export class AuditLogDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly auditService = inject(AuditLogService);
  private readonly auth = inject(AuthService);

  loading = false;
  loadError = '';
  log: AuditLog | null = null;

  ngOnInit(): void {
    const auditId = Number(this.route.snapshot.paramMap.get('auditId'));
    if (!auditId) {
      this.loadError = 'Invalid audit ID.';
      return;
    }
    this.loading = true;
    this.auditService
      .getById(auditId)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (log) => (this.log = log),
        error: (error: HttpErrorResponse) => {
          if (error.status === 401) {
            this.auth.logout();
            return;
          }
          this.loadError = error.error?.message ?? 'Audit log not found.';
        }
      });
  }

  formatJson(value: unknown): string {
    return formatAuditJson(value);
  }
}