import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { SupplierService } from '../../services/supplier.service';

@Component({
  selector: 'app-add-supplier',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './add-supplier.component.html',
  styleUrl: './add-supplier.component.css'
})
export class AddSupplierComponent {
  showAddForm = true;
  fullname: string = '';
  phone: string = '';
  address: string = '';
  // pendingAmount HATA DIYA

  isLoading = false;
  errorMessage = '';

  constructor(
    private supplierService: SupplierService,
    private router: Router
  ) {}

  openAddCustomerForm() {
    this.showAddForm = true;
  }

  closeAddForm() {
    this.showAddForm = false;
    this.router.navigate(['/suppliers']);
  }

  onSubmitCustomer(form: any) {
    if (form.invalid) return;

    this.isLoading = true;

    const supplierData = {
      name: this.fullname,
      contact: this.phone,
      address: this.address,
      notes: ''
    };

    this.supplierService.createSupplier(supplierData).subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success) {
          this.closeAddForm();
        } else {
          this.errorMessage = response.message || 'Failed to add supplier';
        }
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = 'Error connecting to server';
        console.error('Error:', error);
      }
    });
  }
}