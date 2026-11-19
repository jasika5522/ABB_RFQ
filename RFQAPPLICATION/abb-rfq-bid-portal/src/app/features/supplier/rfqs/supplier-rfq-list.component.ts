import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { SupplierRfqService } from '../services/supplier-rfq.service';
import { AuthService } from '../../../core/services/auth.service';
import { SupplierAvailableRfqSummary } from '../../../core/models/supplier-rfq.model';

@Component({
  selector: 'app-supplier-rfq-list',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, DatePipe, PageHeaderComponent, EmptyStateComponent],
  templateUrl: './supplier-rfq-list.component.html',
  styleUrl: './supplier-rfq-list.component.css'
})
export class SupplierRfqListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly supplierRfqService = inject(SupplierRfqService);
  private readonly auth = inject(AuthService);

  loading = false;
  loadError = '';
  items: SupplierAvailableRfqSummary[] = [];
  page = 1;
  pageSize = 10;
  totalCount = 0;

  readonly searchForm = this.fb.nonNullable.group({
    search: ['']
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

    this.supplierRfqService
      .getAvailable({
        page: this.page,
        pageSize: this.pageSize,
        search: this.searchForm.controls.search.value.trim() || undefined
      })
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (result) => {
          this.items = result.items;
          this.totalCount = result.totalCount;
          this.page = result.page;
          this.pageSize = result.pageSize;
        },
        error: (error: HttpErrorResponse) => this.handleError(error)
      });
  }

  onSearch(): void {
    this.loadPage(1);
  }

  clearSearch(): void {
    this.searchForm.reset({ search: '' });
    this.loadPage(1);
  }

  prevPage(): void {
    if (this.page > 1) {
      this.loadPage(this.page - 1);
    }
  }

  nextPage(): void {
    if (this.page < this.totalPages) {
      this.loadPage(this.page + 1);
    }
  }

  private handleError(error: HttpErrorResponse): void {
    if (error.status === 401) {
      this.auth.logout();
      return;
    }
    this.loadError =
      error.error?.message ??
      (error.status === 403 ? 'Access denied.' : 'Unable to load available RFQs.');
  }
}