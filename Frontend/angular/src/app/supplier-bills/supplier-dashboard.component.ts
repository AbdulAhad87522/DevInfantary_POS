import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  SupplierBillsService,
  SupplierSummary,
  Batch,
  Payment,
  PaymentRequest,
} from '../services/supplier-bills.service';

type ViewMode = 'list' | 'supplier-detail' | 'batch-detail';

@Component({
  selector: 'app-supplier-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './supplier-dashboard.component.html',
  styleUrl: './supplier-dashboard.component.css',
})
export class SupplierDashboardComponent implements OnInit {

  // ── View State ──
  viewMode: ViewMode = 'list';

  // ── List View ──
  bills: SupplierSummary[] = [];
  filteredBills: SupplierSummary[] = [];
  searchTerm: string = '';
  activeFilter: string = 'all';
  isLoading: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';

  // ── Supplier Detail View ──
  selectedSupplier: SupplierSummary | null = null;
  supplierBatches: Batch[] = [];
  supplierPayments: Payment[] = [];
  isBatchesLoading: boolean = false;
  batchesError: string = '';
  batchFilter: string = 'all';
  detailTab: 'batches' | 'payments' = 'batches';

  // ── Batch Detail View ──
  selectedBatch: Batch | null = null;
  isBatchDetailLoading: boolean = false;
  batchDetailError: string = '';

  // ── Payment Modal ──
  showPaymentModal: boolean = false;
  paymentTargetSupplier: SupplierSummary | null = null;
  paymentAmount: number = 0;
  paymentRemarks: string = '';
  isSubmittingPayment: boolean = false;
  paymentSuccess: string = '';
  paymentError: string = '';

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

  // ── Supplier Detail KPIs ──
  get supplierTotalBatches(): number {
    return this.supplierBatches.length;
  }
  get supplierTotalPaid(): number {
    return this.supplierBatches.reduce((s, b) => s + b.paid, 0);
  }
  get supplierTotalDue(): number {
    return this.supplierBatches.reduce((s, b) => s + b.remaining, 0);
  }
  get filteredSupplierBatches(): Batch[] {
    if (this.batchFilter === 'completed') return this.supplierBatches.filter(b => b.remaining === 0);
    if (this.batchFilter === 'pending') return this.supplierBatches.filter(b => b.remaining > 0);
    return this.supplierBatches;
  }

  // ── Batch Detail Getters ──
  get batchItemsTotal(): number {
    return this.selectedBatch?.items.reduce((s, i) => s + i.lineTotal, 0) ?? 0;
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
    if (this.searchTerm.trim()) {
      const term = this.searchTerm.toLowerCase();
      result = result.filter(b => b.supplierName.toLowerCase().includes(term));
    }
    if (this.activeFilter !== 'all') {
      result = result.filter(b => {
        const status = b.status.toLowerCase();
        if (this.activeFilter === 'completed') return status === 'completed' || status === 'paid';
        return status === this.activeFilter;
      });
    }
    this.filteredBills = result;
  }

  onSearch() { this.applyFilters(); }
  onClearSearch() { this.searchTerm = ''; this.applyFilters(); }
  setFilter(filter: string) { this.activeFilter = filter; this.applyFilters(); }
  onRefresh() { this.searchTerm = ''; this.activeFilter = 'all'; this.loadSummaries(); }

  // ── Navigate to Supplier Detail ──
  openSupplierDetail(supplier: SupplierSummary): void {
    this.selectedSupplier = supplier;
    this.supplierBatches = [];
    this.supplierPayments = [];
    this.batchesError = '';
    this.batchFilter = 'all';
    this.detailTab = 'batches';
    this.viewMode = 'supplier-detail';
    this.isBatchesLoading = true;

    // Load batches
    this.supplierService.getSupplierBatches(supplier.supplierId).subscribe({
      next: (response) => {
        this.isBatchesLoading = false;
        if (response.success) {
          this.supplierBatches = response.data;
        } else {
          this.batchesError = response.message || 'Batches load nahi hue';
        }
      },
      error: () => {
        this.isBatchesLoading = false;
        this.batchesError = 'Server error! Dobara try karo.';
      },
    });

    // Load payments
    this.supplierService.getSupplierPayments(supplier.supplierId).subscribe({
      next: (response) => {
        if (response.success) {
          this.supplierPayments = response.data;
        }
      },
      error: () => {}
    });
  }

  backToList(): void {
    this.viewMode = 'list';
    this.selectedSupplier = null;
    this.supplierBatches = [];
    this.supplierPayments = [];
    this.selectedBatch = null;
  }

  // ── Navigate to Batch Detail ──
  openBatchDetail(batch: Batch): void {
    this.selectedBatch = null;
    this.batchDetailError = '';
    this.isBatchDetailLoading = true;
    this.viewMode = 'batch-detail';

    this.supplierService.getBatchById(batch.batchId).subscribe({
      next: (response) => {
        this.isBatchDetailLoading = false;
        if (response.success) {
          this.selectedBatch = response.data;
        } else {
          this.batchDetailError = response.message || 'Batch detail load nahi hui';
        }
      },
      error: () => {
        this.isBatchDetailLoading = false;
        this.batchDetailError = 'Server error! Dobara try karo.';
      },
    });
  }

  backToSupplierDetail(): void {
    this.viewMode = 'supplier-detail';
    this.selectedBatch = null;
  }

  // ── Payment Modal ──
  onAddPayment(supplier: SupplierSummary): void {
    this.paymentTargetSupplier = supplier;
    this.paymentAmount = supplier.remaining;
    this.paymentRemarks = '';
    this.paymentSuccess = '';
    this.paymentError = '';
    this.showPaymentModal = true;
  }

  closePaymentModal(): void {
    this.showPaymentModal = false;
    this.paymentTargetSupplier = null;
    this.paymentAmount = 0;
    this.paymentRemarks = '';
    this.paymentSuccess = '';
    this.paymentError = '';
  }

  submitPayment(): void {
    if (!this.paymentTargetSupplier || this.paymentAmount <= 0) {
      this.paymentError = 'Amount sahi daalo';
      return;
    }

    this.isSubmittingPayment = true;
    this.paymentError = '';

    const paymentData: PaymentRequest = {
      supplierId: this.paymentTargetSupplier.supplierId,
      paymentAmount: this.paymentAmount,
      paymentDate: new Date().toISOString(),
      remarks: this.paymentRemarks,
    };

    this.supplierService.addPayment(paymentData).subscribe({
      next: (response) => {
        this.isSubmittingPayment = false;
        if (response.success) {
          this.paymentSuccess = `₨ ${this.paymentAmount.toLocaleString()} payment successfully recorded!`;
          setTimeout(() => {
            this.closePaymentModal();
            if (this.viewMode === 'list') {
              this.loadSummaries();
            } else if (this.viewMode === 'supplier-detail' && this.selectedSupplier) {
              this.loadSummaries();
              this.openSupplierDetail(this.selectedSupplier);
            }
          }, 1800);
        } else {
          this.paymentError = response.message || 'Payment submit nahi hui';
        }
      },
      error: () => {
        this.isSubmittingPayment = false;
        this.paymentError = 'Payment submit nahi hui. Dobara try karo.';
      },
    });
  }

  getStatusClass(status: string): string {
    const s = status?.toLowerCase();
    if (s === 'completed' || s === 'paid') return 'st-completed';
    if (s === 'partial') return 'st-partial';
    return 'st-pending';
  }

  showSuccessMsg(msg: string) {
    this.successMessage = msg;
    setTimeout(() => (this.successMessage = ''), 4000);
  }
}