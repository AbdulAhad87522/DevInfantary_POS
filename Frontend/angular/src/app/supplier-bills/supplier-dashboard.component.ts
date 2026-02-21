import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
interface SupplierBill {
  id: number;
  supplier_name: string;
  total: number;
  paid: number;
  pending: number;
  status: string;
}
@Component({
  selector: 'app-supplier-dashboard',
  standalone: true,
  imports: [CommonModule,FormsModule],
  templateUrl: './supplier-dashboard.component.html',
  styleUrl: './supplier-dashboard.component.css'
})
export class SupplierDashboardComponent {
 // Dummy data for supplier bills
  bills: SupplierBill[] = [
    { id: 1, supplier_name: 'ABC Corp', total: 1500.00, paid: 1200.00, pending: 300.00, status: 'Pending' },
    { id: 2, supplier_name: 'XYZ Ltd', total: 2000.00, paid: 2000.00, pending: 0.00, status: 'Completed' },
    { id: 3, supplier_name: 'Global Supplies', total: 800.00, paid: 500.00, pending: 300.00, status: 'Pending' },
    { id: 4, supplier_name: 'Tech Parts Inc', total: 1200.00, paid: 1000.00, pending: 200.00, status: 'Pending' },
    { id: 5, supplier_name: 'Build Materials Co', total: 1800.00, paid: 1800.00, pending: 0.00, status: 'Completed' },
    { id: 6, supplier_name: 'Fast Shipping', total: 950.00, paid: 450.00, pending: 500.00, status: 'Overdue' },
    { id: 7, supplier_name: 'Quality Metals', total: 3200.00, paid: 3200.00, pending: 0.00, status: 'Completed' }
  ];

  searchTerm: string = '';
  filteredBills = this.bills;

  onSearch() {
    this.filteredBills = this.bills.filter(bill =>
      bill.supplier_name.toLowerCase().includes(this.searchTerm.toLowerCase())
    );
  }

  onClearSearch() {
    this.searchTerm = '';
    this.filteredBills = this.bills;
  }

  onRefresh() {
    console.log('Refresh clicked');
    this.filteredBills = this.bills;
  }

  onViewDetails(bill: SupplierBill) {
    console.log('View Details for:', bill);
  }

  onAddPayment(bill: SupplierBill) {
    console.log('Add Payment for:', bill);
  }
}
