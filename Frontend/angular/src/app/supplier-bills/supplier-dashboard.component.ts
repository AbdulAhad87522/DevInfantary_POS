import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  SupplierBillsService,
  SupplierSummary,
  PaymentRequest,
} from '../services/supplier-bills.service';

@Component({
  selector: 'app-supplier-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './supplier-dashboard.component.html',
  styleUrl: './supplier-dashboard.component.css',
})
export class SupplierDashboardComponent implements OnInit {
  bills: SupplierSummary[] = [];
  filteredBills: SupplierSummary[] = [];

  searchTerm: string = '';
  activeFilter: string = 'all'; // 'all' | 'completed' | 'partial' | 'pending'

  isLoading: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';

  // Payment modal
  showPaymentModal: boolean = false;
  selectedSupplier: SupplierSummary | null = null;
  paymentAmount: number = 0;
  paymentRemarks: string = '';
  isSubmittingPayment: boolean = false;

  constructor(private supplierService: SupplierBillsService) {}

  ngOnInit() {
    this.loadSummaries();
  }

  // ── KPI Getters ──
  get totalBilled(): number {
    return this.bills.reduce((sum, b) => sum + b.totalPrice, 0);
  }

  get totalPaid(): number {
    return this.bills.reduce((sum, b) => sum + b.paid, 0);
  }

  get totalPending(): number {
    return this.bills.reduce((sum, b) => sum + b.remaining, 0);
  }

  // ── Load Data ──
  loadSummaries() {
    this.isLoading = true;
    this.errorMessage = '';

    this.supplierService.getSummaries().subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success) {
          this.bills = response.data;
          this.applyFilters();
        } else {
          this.errorMessage = response.message || 'Data load nahi hua';
        }
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = 'Server se data nahi aaya. Backend chal raha hai?';
        console.error('API Error:', err);
      },
    });
  }

  // ── Filtering ──
  applyFilters() {
    let result = [...this.bills];

    // Search filter
    if (this.searchTerm.trim()) {
      const term = this.searchTerm.toLowerCase();
      result = result.filter((b) =>
        b.supplierName.toLowerCase().includes(term),
      );
    }

    // Status filter
    if (this.activeFilter !== 'all') {
      result = result.filter((b) => {
        const status = b.status.toLowerCase();
        if (this.activeFilter === 'completed') {
          return status === 'completed' || status === 'paid';
        }
        return status === this.activeFilter;
      });
    }

    this.filteredBills = result;
  }

  onSearch() {
    this.applyFilters();
  }

  onClearSearch() {
    this.searchTerm = '';
    this.applyFilters();
  }

  setFilter(filter: string) {
    this.activeFilter = filter;
    this.applyFilters();
  }

  onRefresh() {
    this.searchTerm = '';
    this.activeFilter = 'all';
    this.loadSummaries();
  }

  // ── View Details ──
  onViewDetails(bill: SupplierSummary) {
    // Placeholder — agle step mein detail modal ya route banao
    console.log('View Details:', bill);
    alert(
      `${bill.supplierName}\nBatches: ${bill.batchCount}\nTotal: ₨ ${bill.totalPrice}\nPaid: ₨ ${bill.paid}\nRemaining: ₨ ${bill.remaining}`,
    );
  }

  // ── Payment Modal ──
  onAddPayment(bill: SupplierSummary) {
    this.selectedSupplier = bill;
    this.paymentAmount = 0;
    this.paymentRemarks = '';
    this.showPaymentModal = true;
  }

  closePaymentModal() {
    this.showPaymentModal = false;
    this.selectedSupplier = null;
    this.paymentAmount = 0;
    this.paymentRemarks = '';
  }

  submitPayment() {
    if (
      !this.selectedSupplier ||
      this.paymentAmount <= 0 ||
      this.paymentAmount > this.selectedSupplier.remaining
    )
      return;

    this.isSubmittingPayment = true;

    const paymentData: PaymentRequest = {
      supplierId: this.selectedSupplier.supplierId,
      paymentAmount: this.paymentAmount,
      paymentDate: new Date().toISOString(),
      remarks: this.paymentRemarks,
    };

    this.supplierService.addPayment(paymentData).subscribe({
      next: (response) => {
        this.isSubmittingPayment = false;
        if (response.success) {
          this.showSuccess(
            `₨ ${this.paymentAmount.toLocaleString()} payment ${this.selectedSupplier?.supplierName} ko add ho gayi!`,
          );
          this.closePaymentModal();
          this.loadSummaries();
        } else {
          this.errorMessage = response.message || 'Payment submit nahi hui';
        }
      },
      error: (err) => {
        this.isSubmittingPayment = false;
        this.errorMessage = 'Payment submit nahi hui. Dobara try karo.';
        console.error('Payment Error:', err);
      },
    });
  }

  showSuccess(msg: string) {
    this.successMessage = msg;
    setTimeout(() => (this.successMessage = ''), 4000);
  }
}