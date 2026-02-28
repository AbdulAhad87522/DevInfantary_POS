import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  CustomerBillDetail,
  CustomerBillItem,
  CustomerBillsService,
  CustomerBillSummary,
  RecordPaymentDto,
} from '../services/customer-bills.service';

type ViewMode = 'list' | 'customer-detail' | 'bill-detail';

@Component({
  selector: 'app-customer-bills',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './customer-bills.component.html',
  styleUrls: ['./customer-bills.component.css'],
})
export class CustomerBillsComponent implements OnInit {
  // ── View State ──
  viewMode: ViewMode = 'list';

  // ── List View ──
  summaries: CustomerBillSummary[] = [];
  filteredSummaries: CustomerBillSummary[] = [];
  searchTerm: string = '';
  activeFilter: string = 'all';
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  // ── Customer Detail View ──
  selectedSummary: CustomerBillSummary | null = null;
  customerBills: CustomerBillDetail[] = [];
  isCustomerBillsLoading = false;
  customerBillsError = '';
  billFilter: string = 'all';

  // ── Bill Detail View ──
  selectedBill: CustomerBillDetail | null = null;
  isBillDetailLoading = false;
  billDetailError = '';

  // ── Payment Modal ──
  showPaymentModal = false;
  paymentTargetSummary: CustomerBillSummary | null = null;
  paymentAmount: number = 0;
  paymentRemarks: string = '';
  isPaymentLoading = false;
  paymentSuccess = '';
  paymentError = '';

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

  // ── Customer Detail KPIs ──
  get customerTotalBills(): number {
    return this.customerBills.length;
  }

  get customerTotalPaid(): number {
    return this.customerBills.reduce((s, b) => s + b.amountPaid, 0);
  }

  get customerTotalDue(): number {
    return this.customerBills.reduce((s, b) => s + b.amountDue, 0);
  }

  get filteredCustomerBills(): CustomerBillDetail[] {
    if (this.billFilter === 'paid') return this.customerBills.filter(b => b.amountDue === 0);
    if (this.billFilter === 'outstanding') return this.customerBills.filter(b => b.amountDue > 0);
    return this.customerBills;
  }

  // ── Bill Detail Getters ──
  get billItemsTotal(): number {
    return this.selectedBill?.items.reduce((s, i) => s + i.lineTotal, 0) ?? 0;
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
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Server se connect nahi ho pa raha!';
      },
    });
  }

  // ── Filtering (List) ──
  applyFilters(): void {
    let result = [...this.summaries];
    if (this.searchTerm.trim()) {
      const term = this.searchTerm.toLowerCase();
      result = result.filter(s => s.customerName.toLowerCase().includes(term));
    }
    if (this.activeFilter === 'paid') result = result.filter(s => s.remaining === 0);
    else if (this.activeFilter === 'outstanding') result = result.filter(s => s.remaining > 0);
    this.filteredSummaries = result;
  }

  onSearch(): void { this.applyFilters(); }
  onClearSearch(): void { this.searchTerm = ''; this.applyFilters(); }
  setFilter(filter: string): void { this.activeFilter = filter; this.applyFilters(); }

  onRefresh(): void {
    this.searchTerm = '';
    this.activeFilter = 'all';
    this.loadSummaries();
  }

  // ── Navigate to Customer Detail ──
  openCustomerDetail(summary: CustomerBillSummary): void {
    this.selectedSummary = summary;
    this.customerBills = [];
    this.customerBillsError = '';
    this.billFilter = 'all';
    this.viewMode = 'customer-detail';
    this.isCustomerBillsLoading = true;

    this.customerBillsService.getCustomerBills(summary.customerId).subscribe({
      next: (response) => {
        this.isCustomerBillsLoading = false;
        if (response.success && response.data) {
          this.customerBills = response.data;
        } else {
          this.customerBillsError = response.message || 'Bills load nahi hue';
        }
      },
      error: () => {
        this.isCustomerBillsLoading = false;
        this.customerBillsError = 'Server error! Dobara try karo.';
      },
    });
  }

  backToList(): void {
    this.viewMode = 'list';
    this.selectedSummary = null;
    this.customerBills = [];
    this.selectedBill = null;
  }

  // ── Navigate to Bill Detail ──
  openBillDetail(bill: CustomerBillDetail): void {
    this.selectedBill = null;
    this.billDetailError = '';
    this.isBillDetailLoading = true;
    this.viewMode = 'bill-detail';

    this.customerBillsService.getBillDetail(bill.billId).subscribe({
      next: (response) => {
        this.isBillDetailLoading = false;
        if (response.success && response.data) {
          this.selectedBill = response.data;
        } else {
          this.billDetailError = response.message || 'Bill detail load nahi hui';
        }
      },
      error: () => {
        this.isBillDetailLoading = false;
        this.billDetailError = 'Server error! Dobara try karo.';
      },
    });
  }

  backToCustomerDetail(): void {
    this.viewMode = 'customer-detail';
    this.selectedBill = null;
  }

  // ── Payment Modal ──
  onPayment(summary: CustomerBillSummary): void {
    this.paymentTargetSummary = summary;
    this.paymentAmount = summary.remaining;
    this.paymentRemarks = '';
    this.paymentSuccess = '';
    this.paymentError = '';
    this.showPaymentModal = true;
  }

  closePaymentModal(): void {
    this.showPaymentModal = false;
    this.paymentTargetSummary = null;
    this.paymentAmount = 0;
    this.paymentRemarks = '';
    this.paymentSuccess = '';
    this.paymentError = '';
  }

  submitPayment(): void {
    if (!this.paymentTargetSummary || this.paymentAmount <= 0) {
      this.paymentError = 'Amount sahi daalo';
      return;
    }

    this.isPaymentLoading = true;
    this.paymentError = '';

    const paymentData: RecordPaymentDto = {
      customerId: this.paymentTargetSummary.customerId,
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
            // Refresh current view
            if (this.viewMode === 'list') {
              this.loadSummaries();
            } else if (this.viewMode === 'customer-detail' && this.selectedSummary) {
              this.loadSummaries();
              this.openCustomerDetail(this.selectedSummary);
            }
          }, 1800);
        } else {
          this.paymentError = response.message || 'Payment fail ho gayi';
        }
      },
      error: () => {
        this.isPaymentLoading = false;
        this.paymentError = 'Server error! Dobara try karo.';
      },
    });
  }

  showSuccess(msg: string): void {
    this.successMessage = msg;
    setTimeout(() => (this.successMessage = ''), 4000);
  }

  getPaymentStatusClass(status: string): string {
    const s = status?.toLowerCase();
    if (s === 'paid' || s === 'fully paid') return 'st-paid';
    if (s === 'partial' || s === 'partially paid') return 'st-partial';
    return 'st-due';
  }
}