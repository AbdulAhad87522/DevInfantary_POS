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
  // Add these properties
  showAddForm = true;
  fullname: string = '';
  phone: string = '';
  address: string = '';
  pendingAmount: number = 0;  // NEW FIELD - pending amount
  
  // Add loading state
  isLoading = false;
  errorMessage = '';

  // Inject Router and SupplierService
  constructor(
    private supplierService: SupplierService,
    private router: Router
  ) {}

  // Methods
  openAddCustomerForm() {
    this.showAddForm = true;
  }

  closeAddForm() {
    this.showAddForm = false;
    this.router.navigate(['/suppliers']);
  }

  onSubmitCustomer(form: any) {
    if (form.invalid) {
      return;
    }

    this.isLoading = true;
    
    // ADD THESE CONSOLE LOGS
    console.log('========== DEBUGGING ==========');
    console.log('1. Raw pendingAmount value:', this.pendingAmount);
    console.log('2. Type of pendingAmount:', typeof this.pendingAmount);
    console.log('3. Is pendingAmount > 0?', this.pendingAmount > 0);
    console.log('4. fullname:', this.fullname);
    console.log('5. phone:', this.phone);
    console.log('6. address:', this.address);
    
    const supplierData = {
      name: this.fullname,
      contact: this.phone,
      address: this.address,
      InitialBalance: this.pendingAmount || 0,
      notes: ''
    };
    
    console.log('7. Supplier data being sent:', JSON.stringify(supplierData));
    console.log('8. InitialBalance value:', supplierData.InitialBalance);
    console.log('================================');

    this.supplierService.createSupplier(supplierData).subscribe({
      next: (response) => {
        this.isLoading = false;
        console.log('9. Backend response:', response);
        if (response.success) {
          this.closeAddForm();
        } else {
          this.errorMessage = response.message || 'Failed to add supplier';
        }
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = 'Error connecting to server';
        console.error('10. Error:', error);
      }
    });
  }
}