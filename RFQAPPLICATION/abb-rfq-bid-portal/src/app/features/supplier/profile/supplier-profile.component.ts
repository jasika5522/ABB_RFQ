import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin, finalize } from 'rxjs';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { AuthService } from '../../../core/services/auth.service';
import { SupplierService } from '../services/supplier.service';
import { SupplierResponse, UserResponse } from '../../../core/models/user.model';

const phonePattern = /^[+]?[\d\s()-]{0,20}$/;

@Component({
  selector: 'app-supplier-profile',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent, EmptyStateComponent, DatePipe],
  templateUrl: './supplier-profile.component.html',
  styleUrl: './supplier-profile.component.css'
})
export class SupplierProfileComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly supplierService = inject(SupplierService);

  loading = true;
  saving = false;
  editMode = false;
  loadError = '';
  saveError = '';
  successMessage = '';

  user: UserResponse | null = null;
  supplier: SupplierResponse | null = null;

  readonly companyForm = this.fb.nonNullable.group({
    companyName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
    contactPerson: ['', Validators.maxLength(150)],
    phone: ['', [Validators.maxLength(20), Validators.pattern(phonePattern)]]
  });

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile(): void {
    this.loading = true;
    this.loadError = '';
    this.successMessage = '';

    forkJoin({
      user: this.auth.fetchCurrentUser(),
      supplier: this.supplierService.getMyProfile()
    })
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: ({ user, supplier }) => {
          this.user = user;
          this.supplier = supplier;
          this.patchCompanyForm(supplier);
          this.editMode = false;
        },
        error: (error: HttpErrorResponse) => {
          this.loadError = this.resolveErrorMessage(error, 'Unable to load profile.');
          if (error.status === 401) {
            this.auth.logout();
          }
        }
      });
  }

  startEdit(): void {
    if (!this.supplier) {
      return;
    }
    this.saveError = '';
    this.successMessage = '';
    this.patchCompanyForm(this.supplier);
    this.editMode = true;
  }

  cancelEdit(): void {
    if (this.supplier) {
      this.patchCompanyForm(this.supplier);
    }
    this.companyForm.markAsPristine();
    this.saveError = '';
    this.editMode = false;
  }

  save(): void {
    this.saveError = '';
    this.successMessage = '';

    if (this.companyForm.invalid) {
      this.companyForm.markAllAsTouched();
      return;
    }

    const raw = this.companyForm.getRawValue();
    const request = {
      companyName: raw.companyName.trim(),
      contactPerson: raw.contactPerson.trim() || null,
      phone: raw.phone.trim() || null
    };

    this.saving = true;
    this.supplierService
      .updateMyProfile(request)
      .pipe(finalize(() => (this.saving = false)))
      .subscribe({
        next: (supplier) => {
          this.supplier = supplier;
          this.patchCompanyForm(supplier);
          this.companyForm.markAsPristine();
          this.editMode = false;
          this.successMessage = 'Company information saved successfully.';
        },
        error: (error: HttpErrorResponse) => {
          this.saveError = this.resolveErrorMessage(error, 'Unable to save changes.');
          if (error.status === 401) {
            this.auth.logout();
          }
        }
      });
  }

  accountStatusLabel(): string {
    return this.user?.isActive ? 'Active' : 'Inactive';
  }

  statusClass(status: string | undefined): string {
    switch (status) {
      case 'Approved':
        return 'status status-approved';
      case 'Suspended':
        return 'status status-suspended';
      default:
        return 'status status-pending';
    }
  }

  private patchCompanyForm(supplier: SupplierResponse): void {
    this.companyForm.reset({
      companyName: supplier.companyName,
      contactPerson: supplier.contactPerson ?? '',
      phone: supplier.phone ?? ''
    });
    this.companyForm.markAsPristine();
  }

  private resolveErrorMessage(error: HttpErrorResponse, fallback: string): string {
    const body = error.error;
    if (typeof body === 'string' && body.length) {
      return body;
    }
    if (body?.message) {
      return body.message;
    }
    if (body?.title) {
      return body.title;
    }
    if (error.status === 403) {
      return 'Access denied.';
    }
    if (error.status === 404) {
      return 'Supplier profile not found.';
    }
    if (error.status === 409) {
      return body?.message ?? 'A conflict occurred. Please refresh and try again.';
    }
    if (error.status >= 500) {
      return 'A server error occurred. Please try again later.';
    }
    return fallback;
  }
}