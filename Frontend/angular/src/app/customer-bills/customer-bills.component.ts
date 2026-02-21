import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface CustomerBill {
  id: number;
  full_name: string;
  total_amount: number;
  paid: number;
  remaining: number;
}

@Component({
  selector: 'app-customer-bills',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './customer-bills.component.html',
  styleUrls: ['./customer-bills.component.css']
})
export class CustomerBillsComponent {
  // Dummy data for customer bills
  bills: CustomerBill[] = [
    { id: 1, full_name: 'John Smith', total_amount: 1500.00, paid: 1200.00, remaining: 300.00 },
    { id: 2, full_name: 'Sarah Johnson', total_amount: 2500.00, paid: 2500.00, remaining: 0.00 },
    { id: 3, full_name: 'Michael Brown', total_amount: 800.00, paid: 500.00, remaining: 300.00 },
    { id: 4, full_name: 'Emily Davis', total_amount: 3200.00, paid: 2800.00, remaining: 400.00 },
    { id: 5, full_name: 'David Wilson', total_amount: 950.00, paid: 450.00, remaining: 500.00 },
    { id: 6, full_name: 'Lisa Anderson', total_amount: 1800.00, paid: 1800.00, remaining: 0.00 },
    { id: 7, full_name: 'Robert Taylor', total_amount: 2200.00, paid: 1500.00, remaining: 700.00 }
  ];

  searchTerm: string = '';
  filteredBills = this.bills;

  onSearch() {
    this.filteredBills = this.bills.filter(bill =>
      bill.full_name.toLowerCase().includes(this.searchTerm.toLowerCase())
    );
  }

  onClearSearch() {
    this.searchTerm = '';
    this.filteredBills = this.bills;
  }

  onPayment(bill: CustomerBill) {
    console.log('Payment clicked for:', bill);
  }

  onDetails(bill: CustomerBill) {
    console.log('Details clicked for:', bill);
  }

  onRefresh() {
    console.log('Refresh clicked');
    this.filteredBills = this.bills;
  }
}