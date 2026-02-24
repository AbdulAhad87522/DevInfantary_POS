import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  CustomerBillDetail,
  CustomerBillsService,
  CustomerBillSummary,
  RecordPaymentDto,
} from '../services/customer-bills.service';

@Component({
  selector: 'app-customer-bills',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './customer-bills.component.html',
  styleUrls: ['./customer-bills.component.css'],
})
export class CustomerBillsComponent implements OnInit {
  // Main data
  summaries: CustomerBillSummary[] = [];
  filteredSummaries: CustomerBillSummary[] = [];

  // Search & Filter
  searchTerm: string = '';
  activeFilter: string = 'all'; // 'all' | 'paid' | 'outstanding'

  // Loading & errors
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  // Payment modal
  showPaymentModal = false;
  selectedSummary: CustomerBillSummary | null = null;
  paymentAmount: number = 0;
  paymentRemarks: string = '';
  isPaymentLoading = false;
  paymentSuccess = '';
  paymentError = '';

  // Details modal
  showDetailsModal = false;
  selectedCustomerBills: CustomerBillDetail[] = [];
  isDetailsLoading = false;

  constructor(private customerBillsService: CustomerBillsService) {}

  ngOnInit(): void {
    this.loadSummaries();
  }

  // ── KPI Getters ──
  get totalBilled(): number {
    return this.summaries.reduce((sum, s) => sum + s.totalAmount, 0);
  }

  get totalPaid(): number {
    return this.summaries.reduce((sum, s) => sum + s.paid, 0);
  }

  get totalRemaining(): number {
    return this.summaries.reduce((sum, s) => sum + s.remaining, 0);
  }

  // ── Load Data ──
  loadSummaries(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.customerBillsService.getAllSummaries().subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success && response.data) {
          this.summaries = response.data;
          this.applyFilters();
        } else {
          this.errorMessage = response.message || 'Data load nahi hua';
        }
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = 'Server se connect nahi ho pa raha!';
        console.error('Error:', error);
      },
    });
  }

  // ── Filtering ──
  applyFilters(): void {
    let result = [...this.summaries];

    // Search filter
    if (this.searchTerm.trim()) {
      const term = this.searchTerm.toLowerCase();
      result = result.filter((s) =>
        s.customerName.toLowerCase().includes(term),
      );
    }

    // Status filter
    if (this.activeFilter === 'paid') {
      result = result.filter((s) => s.remaining === 0);
    } else if (this.activeFilter === 'outstanding') {
      result = result.filter((s) => s.remaining > 0);
    }

    this.filteredSummaries = result;
  }

  onSearch(): void {
    this.applyFilters();
  }

  onClearSearch(): void {
    this.searchTerm = '';
    this.applyFilters();
  }

  setFilter(filter: string): void {
    this.activeFilter = filter;
    this.applyFilters();
  }

  onRefresh(): void {
    this.searchTerm = '';
    this.activeFilter = 'all';
    this.loadSummaries();
  }

  // ── Payment Modal ──
  onPayment(summary: CustomerBillSummary): void {
    this.selectedSummary = summary;
    this.paymentAmount = summary.remaining;
    this.paymentRemarks = '';
    this.paymentSuccess = '';
    this.paymentError = '';
    this.showPaymentModal = true;
  }

  closePaymentModal(): void {
    this.showPaymentModal = false;
    this.selectedSummary = null;
    this.paymentAmount = 0;
    this.paymentRemarks = '';
    this.paymentSuccess = '';
    this.paymentError = '';
  }

  submitPayment(): void {
    if (!this.selectedSummary || this.paymentAmount <= 0) {
      this.paymentError = 'Amount sahi daalo';
      return;
    }

    this.isPaymentLoading = true;
    this.paymentError = '';

    const paymentData: RecordPaymentDto = {
      customerId: this.selectedSummary.customerId,
      paymentAmount: this.paymentAmount,
      remarks: this.paymentRemarks || '',
    };

    this.customerBillsService.recordPayment(paymentData).subscribe({
      next: (response) => {
        this.isPaymentLoading = false;
        if (response.success) {
          this.paymentSuccess = `Payment kamiyab! Applied: ₨ ${response.data.applied.toLocaleString()}`;
          setTimeout(() => {
            this.closePaymentModal();
            this.loadSummaries();
          }, 2000);
        } else {
          this.paymentError = response.message || 'Payment fail ho gayi';
        }
      },
      error: (error) => {
        this.isPaymentLoading = false;
        this.paymentError = 'Server error! Dobara try karo.';
        console.error('Payment error:', error);
      },
    });
  }

  // ── Details Modal ──
  onDetails(summary: CustomerBillSummary): void {
    this.selectedSummary = summary;
    this.showDetailsModal = true;
    this.isDetailsLoading = true;
    this.selectedCustomerBills = [];

    this.customerBillsService.getCustomerBills(summary.customerId).subscribe({
      next: (response) => {
        this.isDetailsLoading = false;
        if (response.success && response.data) {
          this.selectedCustomerBills = response.data;
        }
      },
      error: (error) => {
        this.isDetailsLoading = false;
        console.error('Error loading bills:', error);
      },
    });
  }

  closeDetailsModal(): void {
    this.showDetailsModal = false;
    this.selectedSummary = null;
    this.selectedCustomerBills = [];
  }

  showSuccess(msg: string): void {
    this.successMessage = msg;
    setTimeout(() => (this.successMessage = ''), 4000);
  }
}