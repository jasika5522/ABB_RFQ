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
  selector: 'app-procurement-supplier-detail',
  standalone: true,
  imports: [RouterLink, DatePipe, PageHeaderComponent],
  templateUrl: './procurement-supplier-detail.component.html',
  styleUrl: './procurement-supplier-detail.component.css'
})
export class ProcurementSupplierDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly supplierService = inject(SupplierManagementService);
  private readonly auth = inject(AuthService);

  supplierId = 0;
  loading = false;
  loadError = '';
  actionError = '';
  successMessage = '';
  supplier: SupplierDetail | null = null;

  actionBusy = false;
  confirmAction: 'Approved' | 'Suspended' | null = null;

  ngOnInit(): void {
    this.supplierId = Number(this.route.snapshot.paramMap.get('supplierId'));
    if (!this.supplierId) {
      this.loadError = 'Invalid supplier ID.';
      return;
    }
    this.load();
  }

  get canApprove(): boolean {
    return this.supplier?.approvalStatus === 'Pending';
  }

  get canSuspend(): boolean {
    return this.supplier?.approvalStatus === 'Approved';
  }

  statusClass(status: string): string {
    return `status-pill status-${status.toLowerCase()}`;
  }

  load(): void {
    this.loading = true;
    this.loadError = '';
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

  startApprove(): void {
    this.confirmAction = 'Approved';
    this.actionError = '';
    this.successMessage = '';
  }

  startSuspend(): void {
    this.confirmAction = 'Suspended';
    this.actionError = '';
    this.successMessage = '';
  }

  cancelConfirm(): void {
    this.confirmAction = null;
  }

  confirmStatusChange(): void {
    if (!this.confirmAction || !this.supplier) {
      return;
    }
    const status = this.confirmAction;
    this.actionBusy = true;
    this.actionError = '';
    this.confirmAction = null;

    this.supplierService
      .updateStatus(this.supplierId, { approvalStatus: status })
      .pipe(finalize(() => (this.actionBusy = false)))
      .subscribe({
        next: (supplier) => {
          this.supplier = supplier;
          this.successMessage =
            status === 'Approved'
              ? 'Supplier approved. They can submit bids when other rules allow.'
              : 'Supplier suspended.';
        },
        error: (error: HttpErrorResponse) => {
          if (error.status === 401) {
            this.auth.logout();
            return;
          }
          this.actionError = error.error?.message ?? 'Unable to update supplier status.';
        }
      });
  }
}