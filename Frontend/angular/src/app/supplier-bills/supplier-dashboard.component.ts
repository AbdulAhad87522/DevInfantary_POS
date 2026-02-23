import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SupplierBillsService, SupplierSummary, PaymentRequest } from '../services/supplier-bills.service';
// Agar service alag folder mein banai hai toh path adjust karo
// jaise: '../services/supplier-bills.service'

@Component({
  selector: 'app-supplier-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './supplier-dashboard.component.html',
  styleUrl: './supplier-dashboard.component.css'
})
export class SupplierDashboardComponent implements OnInit {

  bills: SupplierSummary[] = [];
  filteredBills: SupplierSummary[] = [];
  searchTerm: string = '';

  // Loading aur error states
  isLoading: boolean = false;
  errorMessage: string = '';

  // Payment modal ke liye
  showPaymentModal: boolean = false;
  selectedSupplier: SupplierSummary | null = null;
  paymentAmount: number = 0;
  paymentRemarks: string = '';
  isSubmittingPayment: boolean = false;

  constructor(private supplierService: SupplierBillsService) {}

  ngOnInit() {
    this.loadSummaries(); // Component load hote hi API call hogi
  }

  // API se data load karo
  loadSummaries(search?: string) {
    this.isLoading = true;
    this.errorMessage = '';

    this.supplierService.getSummaries(search).subscribe({
      next: (response) => {
        if (response.success) {
          this.bills = response.data;
          this.filteredBills = response.data;
        } else {
          this.errorMessage = response.message || 'Kuch gadbad hui';
        }
        this.isLoading = false;
      },
      error: (err) => {
        console.error('API Error:', err);
        this.errorMessage = 'Server se data nahi aaya. Backend chal raha hai?';
        this.isLoading = false;
      }
    });
  }

  // Search - ab API ko search term bhejenge
  onSearch() {
    // Local filter bhi rakh sakte ho ya API se search karo
    // Abhi local filter: (zyada fast hoga)
    this.filteredBills = this.bills.filter(bill =>
      bill.supplierName.toLowerCase().includes(this.searchTerm.toLowerCase())
    );
  }

  onClearSearch() {
    this.searchTerm = '';
    this.filteredBills = this.bills;
  }

  onRefresh() {
    this.searchTerm = '';
    this.loadSummaries();
  }

  onViewDetails(bill: SupplierSummary) {
    // Abhi ke liye console - agle step mein detail modal banenge
    console.log('View Details:', bill);
    alert(`${bill.supplierName} - Batches: ${bill.batchCount}`);
    // Baad mein yahan router navigate ya modal open karenge
  }

  // Payment modal open karo
  onAddPayment(bill: SupplierSummary) {
    this.selectedSupplier = bill;
    this.paymentAmount = 0;
    this.paymentRemarks = '';
    this.showPaymentModal = true;
  }

  // Payment submit karo
  submitPayment() {
    if (!this.selectedSupplier || this.paymentAmount <= 0) return;

    this.isSubmittingPayment = true;

    const paymentData: PaymentRequest = {
      supplierId: this.selectedSupplier.supplierId,
      paymentAmount: this.paymentAmount,
      paymentDate: new Date().toISOString(),
      remarks: this.paymentRemarks
    };

    this.supplierService.addPayment(paymentData).subscribe({
      next: (response) => {
        if (response.success) {
          alert('Payment successfully add ho gayi!');
          this.closePaymentModal();
          this.loadSummaries(); // List refresh karo
        } else {
          alert('Error: ' + response.message);
        }
        this.isSubmittingPayment = false;
      },
      error: (err) => {
        console.error('Payment Error:', err);
        alert('Payment submit nahi hui. Dobara try karo.');
        this.isSubmittingPayment = false;
      }
    });
  }

  closePaymentModal() {
    this.showPaymentModal = false;
    this.selectedSupplier = null;
  }
}