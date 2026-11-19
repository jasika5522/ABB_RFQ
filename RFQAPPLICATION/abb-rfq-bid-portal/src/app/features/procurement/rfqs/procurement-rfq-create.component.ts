import { Component, inject } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { DecimalPipe } from '@angular/common';
import { finalize } from 'rxjs';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ProcurementRfqService } from '../services/procurement-rfq.service';
import { AuthService } from '../../../core/services/auth.service';
import { CreateRfqRequest } from '../../../core/models/procurement-rfq.model';

@Component({
  selector: 'app-procurement-rfq-create',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe, PageHeaderComponent],
  templateUrl: './procurement-rfq-create.component.html',
  styleUrl: './procurement-rfq-create.component.css'
})
export class ProcurementRfqCreateComponent {
  private readonly fb = inject(FormBuilder);
  private readonly rfqService = inject(ProcurementRfqService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  saving = false;
  errorMessage = '';

  readonly form = this.fb.nonNullable.group({
    rfqNumber: ['', [Validators.required, Validators.maxLength(50)]],
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: [''],
    currencyCode: ['', [Validators.required, Validators.pattern(/^[A-Za-z]{3}$/)]],
    items: this.fb.array([this.createItemGroup()])
  });

  get items(): FormArray {
    return this.form.controls.items;
  }

  addItem(): void {
    this.items.push(this.createItemGroup());
  }

  removeItem(index: number): void {
    if (this.items.length > 1) {
      this.items.removeAt(index);
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const request: CreateRfqRequest = {
      rfqNumber: raw.rfqNumber.trim(),
      title: raw.title.trim(),
      description: raw.description.trim() || null,
      currencyCode: raw.currencyCode.trim().toUpperCase(),
      items: raw.items.map((item) => ({
        itemName: item.itemName.trim(),
        description: item.description.trim() || null,
        quantity: Number(item.quantity),
        unit: item.unit.trim()
      }))
    };

    this.saving = true;
    this.errorMessage = '';

    this.rfqService
      .create(request)
      .pipe(finalize(() => (this.saving = false)))
      .subscribe({
        next: (rfq) => {
          void this.router.navigate(['/procurement/rfqs', rfq.rfqId], {
            state: { successMessage: `RFQ ${rfq.rfqNumber} created as Draft.` }
          });
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage = error.error?.message ?? 'Unable to create RFQ.';
          if (error.status === 401) {
            this.auth.logout();
          }
        }
      });
  }

  private createItemGroup() {
    return this.fb.nonNullable.group({
      itemName: ['', [Validators.required, Validators.maxLength(200)]],
      description: [''],
      quantity: [1, [Validators.required, Validators.min(0.001)]],
      unit: ['EA', [Validators.required, Validators.maxLength(30)]]
    });
  }
}