import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from "@angular/router";
import { CustomerService } from '../../services/customer.service';

@Component({
  selector: 'app-add-customer',
  standalone: true,
  imports: [FormsModule, CommonModule, RouterLink],
  templateUrl: './add-customer.component.html',
  styleUrl: './add-customer.component.css'
})
export class AddCustomerComponent {

  showAddForm = true;
  newCustomer: any = {
    fullName: '',
    phone: '',
    address: '',
    customerType: 'retail', // Added default value
    notes: ''
  };
  
  isLoading = false;
  successMessage = '';
  errorMessage = '';

  // Inject the CustomerService
  constructor(private customerService: CustomerService) {}

  openAddCustomerForm() {
    this.showAddForm = true;
    this.newCustomer = { 
      fullName: '', 
      phone: '', 
      address: '',
      customerType: 'retail',
      notes: ''
    };
    this.successMessage = '';
    this.errorMessage = '';
  }

  closeAddForm() {
    this.showAddForm = false;
    this.successMessage = '';
    this.errorMessage = '';
  }

  onSubmitCustomer(form: any) {
    if (form.valid) {
      this.isLoading = true;
      this.errorMessage = '';
      
      // Call the API to create customer
      this.customerService.createCustomer(this.newCustomer).subscribe({
        next: (response) => {
          this.isLoading = false;
          if (response.success) {
            this.successMessage = 'Customer added successfully!';
            console.log('Customer created:', response.data);
            
            // Clear form after success
            this.newCustomer = { 
              fullName: '', 
              phone: '', 
              address: '',
              customerType: 'retail',
              notes: ''
            };
            
            // Close form after 2 seconds
            setTimeout(() => {
              this.closeAddForm();
            }, 2000);
          } else {
            this.errorMessage = response.message || 'Failed to add customer';
          }
        },
        error: (error) => {
          this.isLoading = false;
          this.errorMessage = 'Error connecting to server. Please try again.';
          console.error('Error adding customer:', error);
        }
      });
    }
  }
}