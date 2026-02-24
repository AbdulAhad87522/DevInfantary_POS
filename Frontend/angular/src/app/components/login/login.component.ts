import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../Services/auth.service';
import { LoginRequest } from '../../models/auth.model';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  credentials: LoginRequest = {
    username: '',
    password: ''
  };

  loading: boolean = false;
  errorMessage: string = '';

  constructor(
    private authService: AuthService,
    private router: Router
  ) {
    // Redirect if already logged in
    if (this.authService.isAuthenticated()) {
      this.router.navigate(['/dashboard']);
    }
  }

  onSubmit(): void {
    if (!this.credentials.username || !this.credentials.password) {
      this.errorMessage = 'Please enter username and password';
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    this.authService.login(this.credentials).subscribe({
     next: (response) => {
  console.log('Login response:', response);
  if (response.success) {
    console.log('✅ Login successful, redirecting to dashboard...');
    this.router.navigate(['/']).then(success => {
      console.log('Navigation success:', success);
      if (success) {
        console.log('Current URL:', window.location.href);
      } else {
        console.log('Navigation failed. Current routes:', this.router.config);
      }
    });
  } else {
    this.errorMessage = response.message;
  }
  this.loading = false;
},
      error: (error) => {
        console.error('❌ Login error:', error);
        this.errorMessage = error.error?.message || 'Login failed. Please try again.';
        this.loading = false;
      }
    });
  }
}
