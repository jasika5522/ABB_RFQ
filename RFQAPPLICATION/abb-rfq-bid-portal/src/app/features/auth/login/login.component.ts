import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  showPassword = false;
  submitting = false;
  errorMessage = '';

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]]
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

    this.submitting = true;
    this.auth.login(this.form.getRawValue()).subscribe({
      next: (response) => {
        this.submitting = false;
        this.auth.fetchCurrentUser().subscribe({
          next: (user) => {
            void this.router.navigateByUrl(this.auth.getDashboardRouteForRole(user.role));
          },
          error: () => {
            this.errorMessage = 'Login succeeded but the session could not be verified.';
            this.auth.logout();
          }
        });
      },
      error: (error: HttpErrorResponse) => {
        this.submitting = false;
        this.errorMessage =
          error.error?.message ?? error.error?.title ?? 'Login failed. Check your credentials.';
      }
    });
  }
}