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

  // Search
  searchTerm: string = '';

  // Loading & errors
  isLoading = false;
  errorMessage = '';

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

  // Saari customer summaries load karo
  loadSummaries(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.customerBillsService.getAllSummaries().subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success && response.data) {
          this.summaries = response.data;
          this.filteredSummaries = response.data;
          console.log('Summaries loaded:', this.summaries);
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

  // Search - API se ya local filter
  onSearch(): void {
    if (!this.searchTerm.trim()) {
      this.filteredSummaries = this.summaries;
      return;
    }

    // Option 1: Local filter (fast)
    const term = this.searchTerm.toLowerCase();
    this.filteredSummaries = this.summaries.filter((s) =>
      s.customerName.toLowerCase().includes(term),
    );

    // Option 2: API se search (comment out kiya hua, zaroorat pe use karo)
    // this.customerBillsService.getAllSummaries(this.searchTerm).subscribe(...)
  }

  onClearSearch(): void {
    this.searchTerm = '';
    this.filteredSummaries = this.summaries;
  }

  onRefresh(): void {
    this.searchTerm = '';
    this.loadSummaries();
  }

  // Payment modal kholo
  onPayment(summary: CustomerBillSummary): void {
    this.selectedSummary = summary;
    this.paymentAmount = summary.remaining; // Default: poora remaining
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
  }

  // Payment submit karo
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
          this.paymentSuccess = `Payment kamiyab! Applied: ${response.data.applied}`;
          console.log('Payment result:', response.data);
          // 2 second baad close karo aur refresh karo
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

  // Details modal kholo
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
}
