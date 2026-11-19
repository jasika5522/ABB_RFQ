import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { SupplierManagementService } from '../../../core/services/supplier-management.service';
import { AuthService } from '../../../core/services/auth.service';
import { SupplierDetail, SUPPLIER_APPROVAL_STATUSES } from '../../../core/models/supplier-management.model';

@Component({
  selector: 'app-procurement-supplier-list',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, DatePipe, PageHeaderComponent, EmptyStateComponent],
  templateUrl: './procurement-supplier-list.component.html',
  styleUrl: './procurement-supplier-list.component.css'
})
export class ProcurementSupplierListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly supplierService = inject(SupplierManagementService);
  private readonly auth = inject(AuthService);

  loading = false;
  loadError = '';
  items: SupplierDetail[] = [];
  page = 1;
  pageSize = 10;
  totalCount = 0;
  readonly statusFilters = ['', ...SUPPLIER_APPROVAL_STATUSES];

  readonly filters = this.fb.nonNullable.group({
    companyName: [''],
    approvalStatus: ['']
  });

  ngOnInit(): void {
    this.loadPage(1);
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  loadPage(page: number): void {
    this.page = page;
    this.loading = true;
    this.loadError = '';
    const raw = this.filters.getRawValue();

    this.supplierService
      .list({
        page: this.page,
        pageSize: this.pageSize,
        companyName: raw.companyName || undefined,
        approvalStatus: raw.approvalStatus || undefined
      })
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (result) => {
          this.items = result.items;
          this.totalCount = result.totalCount;
          this.page = result.page;
        },
        error: (error: HttpErrorResponse) => this.handleError(error)
      });
  }

  applyFilters(): void {
    this.loadPage(1);
  }

  clearFilters(): void {
    this.filters.reset({ companyName: '', approvalStatus: '' });
    this.loadPage(1);
  }

  statusClass(status: string): string {
    return `status-pill status-${status.toLowerCase()}`;
  }

  quickFilter(status: string): void {
    this.filters.patchValue({ approvalStatus: status });
    this.loadPage(1);
  }

  private handleError(error: HttpErrorResponse): void {
    if (error.status === 401) {
      this.auth.logout();
      return;
    }
    this.loadError = error.error?.message ?? 'Unable to load suppliers.';
  }
}