import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { SupplierManagementService } from '../../../core/services/supplier-management.service';
import { AuthService } from '../../../core/services/auth.service';
import { SupplierDetail } from '../../../core/models/supplier-management.model';

@Component({
  selector: 'app-approver-supplier-detail',
  standalone: true,
  imports: [RouterLink, DatePipe, PageHeaderComponent],
  templateUrl: './approver-supplier-detail.component.html',
  styleUrl: './approver-supplier-detail.component.css'
})
export class ApproverSupplierDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly supplierService = inject(SupplierManagementService);
  private readonly auth = inject(AuthService);

  supplierId = 0;
  loading = false;
  loadError = '';
  supplier: SupplierDetail | null = null;

  ngOnInit(): void {
    this.supplierId = Number(this.route.snapshot.paramMap.get('supplierId'));
    if (!this.supplierId) {
      this.loadError = 'Invalid supplier ID.';
      return;
    }
    this.loading = true;
    this.supplierService
      .getById(this.supplierId)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (supplier) => (this.supplier = supplier),
        error: (error: HttpErrorResponse) => {
          if (error.status === 401) {
            this.auth.logout();
            return;
          }
          this.loadError = error.error?.message ?? 'Supplier not found.';
        }
      });
  }

  statusClass(status: string): string {
    return `status-pill status-${status.toLowerCase()}`;
  }
}