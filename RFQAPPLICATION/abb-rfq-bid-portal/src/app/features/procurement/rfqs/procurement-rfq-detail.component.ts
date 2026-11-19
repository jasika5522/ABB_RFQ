import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize, forkJoin } from 'rxjs';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ProcurementRfqService } from '../services/procurement-rfq.service';
import { ProcurementRoundService } from '../services/procurement-round.service';
import { AuthService } from '../../../core/services/auth.service';
import { RfqDetailResponse, RfqRoundResponse, UpdateRfqRequest } from '../../../core/models/procurement-rfq.model';

@Component({
  selector: 'app-procurement-rfq-detail',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, DatePipe, DecimalPipe, PageHeaderComponent],
  templateUrl: './procurement-rfq-detail.component.html',
  styleUrl: './procurement-rfq-detail.component.css'
})
export class ProcurementRfqDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly rfqService = inject(ProcurementRfqService);
  private readonly roundService = inject(ProcurementRoundService);
  private readonly auth = inject(AuthService);

  loading = false;
  loadError = '';
  rfq: RfqDetailResponse | null = null;
  rounds: RfqRoundResponse[] = [];

  editMode = false;
  savingRfq = false;
  rfqError = '';
  successMessage = history.state?.['successMessage'] ?? '';

  roundDeadline = '';
  creatingRound = false;
  roundError = '';
  roundSuccess = '';

  confirmPublishRoundId: number | null = null;
  confirmCloseRoundId: number | null = null;
  roundActionBusy = false;

  readonly editForm = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: [''],
    currencyCode: ['', [Validators.required, Validators.pattern(/^[A-Za-z]{3}$/)]],
    items: this.fb.array<ReturnType<typeof this.createItemGroup>>([])
  });

  ngOnInit(): void {
    const rfqId = Number(this.route.snapshot.paramMap.get('rfqId'));
    if (!rfqId) {
      this.loadError = 'Invalid RFQ ID.';
      return;
    }
    this.loadData(rfqId);
  }

  get items(): FormArray {
    return this.editForm.controls.items;
  }

  get isDraft(): boolean {
    return this.rfq?.status === 'Draft';
  }

  canCreateRound(): boolean {
    if (!this.rfq) {
      return false;
    }
    return this.rfq.status === 'Draft' || this.rfq.status === 'Evaluated';
  }

  loadData(rfqId: number): void {
    this.loading = true;
    this.loadError = '';

    forkJoin({
      rfq: this.rfqService.getById(rfqId),
      rounds: this.roundService.list(rfqId)
    })
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: ({ rfq, rounds }) => {
          this.rfq = rfq;
          this.rounds = rounds;
          this.patchEditForm(rfq);
        },
        error: (error: HttpErrorResponse) => this.handleLoadError(error)
      });
  }

  startEdit(): void {
    if (!this.rfq || !this.isDraft) {
      return;
    }
    this.patchEditForm(this.rfq);
    this.editMode = true;
    this.rfqError = '';
  }

  cancelEdit(): void {
    if (this.rfq) {
      this.patchEditForm(this.rfq);
    }
    this.editMode = false;
  }

  saveRfq(): void {
    if (!this.rfq || this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }

    const raw = this.editForm.getRawValue();
    const request: UpdateRfqRequest = {
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

    this.savingRfq = true;
    this.rfqError = '';

    this.rfqService
      .update(this.rfq.rfqId, request)
      .pipe(finalize(() => (this.savingRfq = false)))
      .subscribe({
        next: (rfq) => {
          this.rfq = rfq;
          this.editMode = false;
          this.successMessage = 'RFQ updated successfully.';
          this.patchEditForm(rfq);
        },
        error: (error: HttpErrorResponse) => {
          this.rfqError = error.error?.message ?? 'Unable to update RFQ.';
          if (error.status === 401) {
            this.auth.logout();
          }
        }
      });
  }

  createRound(): void {
    if (!this.rfq || !this.roundDeadline) {
      this.roundError = 'Deadline is required.';
      return;
    }

    const deadline = new Date(this.roundDeadline).toISOString();
    this.creatingRound = true;
    this.roundError = '';
    this.roundSuccess = '';

    this.roundService
      .create(this.rfq.rfqId, { deadline })
      .pipe(finalize(() => (this.creatingRound = false)))
      .subscribe({
        next: () => {
          this.roundSuccess = 'Round created.';
          this.roundDeadline = '';
          this.loadData(this.rfq!.rfqId);
        },
        error: (error: HttpErrorResponse) => {
          this.roundError = error.error?.message ?? 'Unable to create round.';
        }
      });
  }

  publishRound(roundId: number): void {
    if (!this.rfq) {
      return;
    }
    this.roundActionBusy = true;
    this.roundError = '';
    this.roundService
      .publish(this.rfq.rfqId, roundId)
      .pipe(finalize(() => (this.roundActionBusy = false)))
      .subscribe({
        next: () => {
          this.confirmPublishRoundId = null;
          this.roundSuccess = 'Round published.';
          this.loadData(this.rfq!.rfqId);
        },
        error: (error: HttpErrorResponse) => {
          this.roundError = error.error?.message ?? 'Unable to publish round.';
        }
      });
  }

  closeRound(roundId: number): void {
    if (!this.rfq) {
      return;
    }
    this.roundActionBusy = true;
    this.roundError = '';
    this.roundService
      .close(this.rfq.rfqId, roundId)
      .pipe(finalize(() => (this.roundActionBusy = false)))
      .subscribe({
        next: () => {
          this.confirmCloseRoundId = null;
          this.roundSuccess = 'Round closed.';
          this.loadData(this.rfq!.rfqId);
        },
        error: (error: HttpErrorResponse) => {
          this.roundError = error.error?.message ?? 'Unable to close round.';
        }
      });
  }

  addItem(): void {
    this.items.push(this.createItemGroup());
  }

  removeItem(index: number): void {
    if (this.items.length > 1) {
      this.items.removeAt(index);
    }
  }

  statusClass(status: string): string {
    return `status-pill status-${status.toLowerCase()}`;
  }

  private patchEditForm(rfq: RfqDetailResponse): void {
    this.editForm.patchValue({
      title: rfq.title,
      description: rfq.description ?? '',
      currencyCode: rfq.currencyCode
    });
    this.items.clear();
    for (const item of rfq.items) {
      this.items.push(
        this.fb.nonNullable.group({
          itemName: [item.itemName, [Validators.required, Validators.maxLength(200)]],
          description: [item.description ?? ''],
          quantity: [item.quantity, [Validators.required, Validators.min(0.001)]],
          unit: [item.unit, [Validators.required, Validators.maxLength(30)]]
        })
      );
    }
    if (this.items.length === 0) {
      this.items.push(this.createItemGroup());
    }
  }

  private createItemGroup() {
    return this.fb.nonNullable.group({
      itemName: ['', [Validators.required, Validators.maxLength(200)]],
      description: [''],
      quantity: [1, [Validators.required, Validators.min(0.001)]],
      unit: ['EA', [Validators.required, Validators.maxLength(30)]]
    });
  }

  private handleLoadError(error: HttpErrorResponse): void {
    if (error.status === 401) {
      this.auth.logout();
      return;
    }
    this.loadError = error.error?.message ?? 'Unable to load RFQ.';
  }
}