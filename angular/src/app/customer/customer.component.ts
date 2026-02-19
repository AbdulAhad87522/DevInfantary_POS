import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
interface Customer {
  id: number;
  fullName: string;
  phone: string;
  address: string;
  currentBalance: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  notes: string;
}
@Component({
  selector: 'app-customer',
  standalone: true,
  imports: [FormsModule,CommonModule],
  templateUrl: './customer.component.html',
  styleUrl: './customer.component.css'
})
export class CustomerComponent {
// Dummy data (in real app → fetch from service / API)
  customers: Customer[] = [
    {
      id: 1,
      fullName: 'Ahmed Khan',
      phone: '+92 321 1234567',
      address: 'House 45, Street 12, Gulberg III, Lahore',
      currentBalance: 14500,
      isActive: true,
      createdAt: '2025-03-15',
      updatedAt: '2026-02-10',
      notes: 'Regular buyer of electrical items'
    },
    {
      id: 2,
      fullName: 'Sana Malik',
      phone: '+92 300 9876543',
      address: 'Flat 3B, Phase 5, DHA Lahore',
      currentBalance: -3200,   // negative = credit / advance
      isActive: true,
      createdAt: '2025-11-20',
      updatedAt: '2026-01-28',
      notes: 'Prefers cash payments'
    },
    {
      id: 3,
      fullName: 'Bilal Traders',
      phone: '+92 333 4567890',
      address: 'Shop #7, Anarkali Bazaar, Lahore',
      currentBalance: 8750,
      isActive: false,
      createdAt: '2024-09-05',
      updatedAt: '2025-12-19',
      notes: 'Inactive since Dec 2025'
    }
  ];

  // KPIs
  totalCustomers = this.customers.length;
  activeCustomers = this.customers.filter(c => c.isActive).length;

  // Search
  searchTerm = '';

  get filteredCustomers() {
    if (!this.searchTerm.trim()) return this.customers;

    const term = this.searchTerm.toLowerCase();
    return this.customers.filter(c =>
      c.fullName.toLowerCase().includes(term) ||
      c.phone.includes(term) ||
      c.address.toLowerCase().includes(term)
    );
  }

  // Actions (placeholders – connect to real service later)
  addCustomer() {
    console.log('Open Add Customer modal/form');
    // In real app: open dialog / navigate to form
  }

  viewCustomer(customer: Customer) {
    console.log('View customer:', customer);
  }

  editCustomer(customer: Customer) {
    console.log('Edit customer:', customer);
  }

  deleteCustomer(customer: Customer) {
    if (confirm(`Delete ${customer.fullName}?`)) {
      console.log('Delete customer:', customer.id);
      // In real app: call service.deleteCustomer(id)
    }
  }

  refresh() {
    console.log('Refresh / reload customers');
    // In real app: reload from API
  }
}
