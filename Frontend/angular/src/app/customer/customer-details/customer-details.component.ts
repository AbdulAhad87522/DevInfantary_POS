import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from "@angular/router";
import { CustomerService, Customer } from '../../services/customer.service';

@Component({
  selector: 'app-customer-details',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './customer-details.component.html',
  styleUrl: './customer-details.component.css'
})
export class CustomerDetailsComponent implements OnInit {
  
  customers: Customer[] = [];
  filteredCustomers: Customer[] = [];
  
  // KPIs
  totalCustomers = 0;
  activeCustomers = 0;

  // Search
  searchTerm = '';

  // Loading states
  isLoading = false;
  errorMessage = '';
  includeInactive = false;

  // UI flags
  showViewDetails = false;
  showEditForm = false;
  selectedCustomer: Customer | null = null;
  editingCustomer: any = {};

  constructor(private customerService: CustomerService) {}

  ngOnInit(): void {
    this.loadCustomers();
  }

  loadCustomers(): void {
    this.isLoading = true;
    this.errorMessage = '';
    
    this.customerService.getAllCustomers(this.includeInactive).subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success && response.data) {
          this.customers = response.data;
          this.filteredCustomers = response.data;
          this.updateKPIs();
          console.log('Customers loaded:', this.customers);
        } else {
          this.errorMessage = response.message || 'Failed to load customers';
        }
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = 'Error connecting to server. Please try again.';
        console.error('Error loading customers:', error);
      }
    });
  }

  updateKPIs(): void {
    this.totalCustomers = this.customers.length;
    this.activeCustomers = this.customers.filter(c => c.isActive).length;
  }

  get filteredCustomersList() {
    if (!this.searchTerm.trim()) return this.filteredCustomers;

    const term = this.searchTerm.toLowerCase();
    return this.filteredCustomers.filter(c =>
      c.fullName.toLowerCase().includes(term) ||
      (c.phone && c.phone.toLowerCase().includes(term)) ||
      (c.address && c.address.toLowerCase().includes(term))
    );
  }

  search(): void {
    if (!this.searchTerm.trim()) {
      this.filteredCustomers = this.customers;
      return;
    }
    
    const term = this.searchTerm.toLowerCase();
    this.filteredCustomers = this.customers.filter(c =>
      c.fullName.toLowerCase().includes(term) ||
      (c.phone && c.phone.toLowerCase().includes(term)) ||
      (c.address && c.address.toLowerCase().includes(term))
    );
  }

  toggleIncludeInactive(): void {
    this.loadCustomers();
  }

  addCustomer() {
    // Navigate to add customer page
    // You can implement router navigation here
    console.log('Navigate to add customer');
  }

  viewCustomer(customer: Customer) {
    console.log('View customer:', customer);
    this.selectedCustomer = customer;
    this.showViewDetails = true;
  }

  closeViewDetails() {
    this.showViewDetails = false;
    this.selectedCustomer = null;
  }

  openEditCustomer(customer: any) {
    this.editingCustomer = { ...customer }; // deep copy
    this.showEditForm = true;
  }

  closeEditForm() {
    this.showEditForm = false;
    this.editingCustomer = {};
  }

  onSubmitEdit(form: any) {
    if (form.invalid) {
      Object.keys(form.controls).forEach(key => form.controls[key].markAsTouched());
      return;
    }

    this.isLoading = true;
    
    this.customerService.updateCustomer(this.editingCustomer.customerId, this.editingCustomer).subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success) {
          console.log('Customer updated:', response.data);
          this.loadCustomers(); // Reload the list
          this.closeEditForm();
        } else {
          this.errorMessage = response.message || 'Failed to update customer';
        }
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = 'Error updating customer. Please try again.';
        console.error('Error updating customer:', error);
      }
    });
  }

  deleteCustomer(customer: Customer) {
    if (confirm(`Delete ${customer.fullName}?`)) {
      this.isLoading = true;
      
      this.customerService.deleteCustomer(customer.customerId).subscribe({
        next: (response) => {
          this.isLoading = false;
          if (response.success) {
            console.log('Customer deleted:', customer.customerId);
            this.loadCustomers(); // Reload the list
          } else {
            this.errorMessage = response.message || 'Failed to delete customer';
          }
        },
        error: (error) => {
          this.isLoading = false;
          this.errorMessage = 'Error deleting customer. Please try again.';
          console.error('Error deleting customer:', error);
        }
      });
    }
  }

  restoreCustomer(customer: Customer) {
  if (confirm(`Restore ${customer.fullName}?`)) {
    this.isLoading = true;
    
    this.customerService.restoreCustomer(customer.customerId).subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success) {
          console.log('Customer restored:', customer.customerId);
          this.loadCustomers(); // Reload the list
        } else {
          this.errorMessage = response.message || 'Failed to restore customer';
        }
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = 'Error restoring customer. Please try again.';
        console.error('Error restoring customer:', error);
      }
    });
  }
}



  refresh() {
    this.loadCustomers();
  }
}