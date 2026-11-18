import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../core/services/auth.service';

const phonePattern = /^[+]?[\d\s()-]{7,20}$/;

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  showPassword = false;
  submitting = false;
  errorMessage = '';

  readonly form = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(150)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(150)]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
    companyName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
    registrationNumber: ['', [Validators.required, Validators.maxLength(100)]],
    contactPerson: ['', Validators.maxLength(150)],
    phone: ['', [Validators.maxLength(20), Validators.pattern(phonePattern)]]
  });

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }

  submit(): void {
    this.errorMessage = '';
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const request = {
      ...raw,
      contactPerson: raw.contactPerson || null,
      phone: raw.phone || null
    };

    this.submitting = true;
    this.auth.register(request).subscribe({
      next: () => {
        this.submitting = false;
        void this.router.navigate(['/login']);
      },
      error: (error: HttpErrorResponse) => {
        this.submitting = false;
        this.errorMessage =
          error.error?.message ?? error.error?.title ?? 'Registration failed. Please try again.';
      }
    });
  }
}