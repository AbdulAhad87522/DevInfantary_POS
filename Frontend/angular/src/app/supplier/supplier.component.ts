import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface Supplier {
  supplier_id: number;
  name: string;
  contact: string;
  address: string;
  account_balance: number;
  is_active: boolean;
  created_at: string;
  updated_at: string;
  notes: string;
}

@Component({
  selector: 'app-supplier',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './supplier.component.html',
  styleUrls: ['./supplier.component.css']
})
export class SupplierComponent {
  // Dummy data for suppliers
  suppliers: Supplier[] = [
    {
      supplier_id: 1,
      name: 'ABC Corp',
      contact: 'john@abc.com',
      address: '123 Main St, City A',
      account_balance: 1500.50,
      is_active: true,
      created_at: '2023-01-15',
      updated_at: '2023-10-01',
      notes: 'Reliable supplier'
    },
    {
      supplier_id: 2,
      name: 'XYZ Ltd',
      contact: 'jane@xyz.com',
      address: '456 Elm St, City B',
      account_balance: -200.00,
      is_active: false,
      created_at: '2023-02-20',
      updated_at: '2023-09-15',
      notes: 'Pending payment'
    },
    {
      supplier_id: 3,
      name: 'Global Supplies',
      contact: 'bob@global.com',
      address: '789 Oak St, City C',
      account_balance: 3200.75,
      is_active: true,
      created_at: '2023-03-10',
      updated_at: '2023-11-05',
      notes: 'High volume orders'
    },
    {
      supplier_id: 4,
      name: 'Tech Parts Inc',
      contact: 'alice@techparts.com',
      address: '101 Pine St, City D',
      account_balance: 0.00,
      is_active: true,
      created_at: '2023-04-05',
      updated_at: '2023-10-20',
      notes: 'New supplier'
    },
    {
      supplier_id: 5,
      name: 'Build Materials Co',
      contact: 'charlie@build.com',
      address: '202 Maple St, City E',
      account_balance: -500.25,
      is_active: false,
      created_at: '2023-05-12',
      updated_at: '2023-08-30',
      notes: 'Under review'
    }
  ];

  // Search functionality (filter by name)
  searchTerm: string = '';
  filteredSuppliers = this.suppliers;

  onSearch() {
    this.filteredSuppliers = this.suppliers.filter(supplier =>
      supplier.name.toLowerCase().includes(this.searchTerm.toLowerCase())
    );
  }

  onClearSearch() {
    this.searchTerm = '';
    this.filteredSuppliers = this.suppliers;
  }

  // Top button actions (placeholders)
  onAdd() {
    console.log('Add clicked');
    // Add logic here (e.g., open modal)
  }

  onEdit() {
    console.log('Edit clicked');
    // Add logic here (e.g., for selected items)
  }

  onDelete() {
    console.log('Delete clicked');
    // Add logic here (e.g., confirm delete)
  }

  // Row actions
  onEditRow(supplier: Supplier) {
    console.log('Edit row for:', supplier);
  }

  onDeleteRow(supplier: Supplier) {
    console.log('Delete row for:', supplier);
  }
}