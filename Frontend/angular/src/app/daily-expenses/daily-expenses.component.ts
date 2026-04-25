import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import {
  DailyExpensesService,
  DailyExpense,
  DailyExpenseDto,
  DailyExpenseUpdateDto,
} from '../services/daily-expenses.service';

@Component({
  selector: 'app-daily-expenses',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, HttpClientModule],
  templateUrl: './daily-expenses.component.html',
  styleUrls: ['./daily-expenses.component.css'],
  providers: [DailyExpensesService],
})
export class DailyExpensesComponent implements OnInit {
  expenses: DailyExpense[] = [];
  totalAmount = 0;
  loading = false;
  errorMessage = '';
  successMessage = '';

  // Pagination
  pageNumber = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;
  pagesArray: number[] = [];

  // Modal flags
  showCreateModal = false;
  showEditModal = false;
  showDeleteModal = false;
  selectedExpense: DailyExpense | null = null;

  // Forms
  expenseForm!: FormGroup;
  searchForm!: FormGroup;
  dateRangeForm!: FormGroup;

  // Active tab
  activeTab: 'all' | 'search' | 'dateRange' = 'all';

  constructor(
    private expenseService: DailyExpensesService,
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    this.initForms();
    this.loadPaginated();
    this.loadTotal();
  }

  // ── Forms ────────────────────────────────────────────────────

  initForms(): void {
    this.expenseForm = this.fb.group({
      date: [this.todayISO(), Validators.required],
      description: ['', [Validators.required, Validators.minLength(3)]],
      amount: [null, [Validators.required, Validators.min(1)]],
    });

    this.searchForm = this.fb.group({
      term: ['', Validators.required],
    });

    this.dateRangeForm = this.fb.group({
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
    });
  }

  // ── Tab switching ────────────────────────────────────────────

  switchTab(tab: 'all' | 'search' | 'dateRange'): void {
    this.activeTab = tab;
    this.clearMessages();
    if (tab === 'all') {
      this.pageNumber = 1;
      this.searchForm.reset();
      this.dateRangeForm.reset();
      this.loadPaginated();
    }
  }

  // ── Data loading ─────────────────────────────────────────────

  loadPaginated(): void {
    this.loading = true;
    this.clearMessages();

    this.expenseService
      .getExpensesPaginated(this.pageNumber, this.pageSize)
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.expenses = res.data ?? [];
            this.totalCount = res.totalCount;
            this.totalPages = res.totalPages;
            this.pagesArray = Array.from(
              { length: this.totalPages },
              (_, i) => i + 1
            );
          } else {
            this.errorMessage = res.message || 'Failed to load expenses';
          }
          this.loading = false;
        },
        error: (err) => {
          this.errorMessage =
            err?.error?.message ?? 'Failed to load expenses. Is the API running?';
          this.loading = false;
        },
      });
  }

  searchExpenses(): void {
    if (this.searchForm.invalid) {
      this.searchForm.markAllAsTouched();
      return;
    }
    this.loading = true;
    this.clearMessages();

    this.expenseService
      .searchExpenses(this.searchForm.value.term)
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.expenses = res.data ?? [];
            this.showSuccess(`Found ${this.expenses.length} result(s)`);
          } else {
            this.errorMessage = res.message || 'Search failed';
          }
          this.loading = false;
        },
        error: (err) => {
          this.errorMessage = err?.error?.message ?? 'Search failed';
          this.loading = false;
        },
      });
  }

  filterByDateRange(): void {
    if (this.dateRangeForm.invalid) {
      this.dateRangeForm.markAllAsTouched();
      return;
    }
    this.loading = true;
    this.clearMessages();
    const { startDate, endDate } = this.dateRangeForm.value;

    this.expenseService
      .getExpensesByDateRange(startDate, endDate)
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.expenses = res.data ?? [];
            this.showSuccess(
              `Showing ${this.expenses.length} expense(s) in range`
            );
          } else {
            this.errorMessage = res.message || 'Filter failed';
          }
          this.loading = false;
        },
        error: (err) => {
          this.errorMessage =
            err?.error?.message ?? 'Failed to filter expenses';
          this.loading = false;
        },
      });
  }

  loadTotal(): void {
    this.expenseService.getTotalAmount().subscribe({
      next: (res) => {
        if (res.success) this.totalAmount = res.data ?? 0;
      },
      error: (err) => console.error('Failed to load total', err),
    });
  }

  // ── CRUD ─────────────────────────────────────────────────────

  createExpense(): void {
    if (this.expenseForm.invalid) {
      this.expenseForm.markAllAsTouched();
      return;
    }
    this.loading = true;
    this.clearMessages();

    const dto: DailyExpenseDto = {
      date: this.expenseForm.value.date,
      description: this.expenseForm.value.description,
      amount: Math.round(+this.expenseForm.value.amount),
    };

    this.expenseService.createExpense(dto).subscribe({
      next: (res) => {
        if (res.success) {
          this.showSuccess('Expense created successfully!');
          this.closeCreateModal();
          this.pageNumber = 1;
          this.loadPaginated();
          this.loadTotal();
        } else {
          this.errorMessage = res.message || 'Failed to create expense';
        }
        this.loading = false;
      },
      error: (err) => {
        this.errorMessage =
          err?.error?.message ?? 'Failed to create expense';
        this.loading = false;
      },
    });
  }

  updateExpense(): void {
    if (this.expenseForm.invalid || !this.selectedExpense) {
      this.expenseForm.markAllAsTouched();
      return;
    }
    this.loading = true;
    this.clearMessages();

    const dto: DailyExpenseUpdateDto = {
      date: this.expenseForm.value.date,
      description: this.expenseForm.value.description,
      amount: Math.round(+this.expenseForm.value.amount),
    };

    this.expenseService
      .updateExpense(this.selectedExpense.expenseId, dto)
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.showSuccess('Expense updated successfully!');
            this.closeEditModal();
            this.loadPaginated();
            this.loadTotal();
          } else {
            this.errorMessage = res.message || 'Failed to update expense';
          }
          this.loading = false;
        },
        error: (err) => {
          this.errorMessage =
            err?.error?.message ?? 'Failed to update expense';
          this.loading = false;
        },
      });
  }

  confirmDelete(): void {
    if (!this.selectedExpense) return;
    this.loading = true;
    this.clearMessages();

    this.expenseService
      .deleteExpense(this.selectedExpense.expenseId)
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.showSuccess('Expense deleted successfully!');
            this.closeDeleteModal();
            if (this.expenses.length === 1 && this.pageNumber > 1)
              this.pageNumber--;
            this.loadPaginated();
            this.loadTotal();
          } else {
            this.errorMessage = res.message || 'Failed to delete expense';
          }
          this.loading = false;
        },
        error: (err) => {
          this.errorMessage =
            err?.error?.message ?? 'Failed to delete expense';
          this.loading = false;
        },
      });
  }

  // ── Pagination ────────────────────────────────────────────────

  previousPage(): void {
    if (this.pageNumber > 1) {
      this.pageNumber--;
      this.loadPaginated();
    }
  }

  nextPage(): void {
    if (this.pageNumber < this.totalPages) {
      this.pageNumber++;
      this.loadPaginated();
    }
  }

  goToPage(p: number): void {
    if (p !== this.pageNumber) {
      this.pageNumber = p;
      this.loadPaginated();
    }
  }

  // ── Modals ────────────────────────────────────────────────────

  openCreateModal(): void {
    this.clearMessages();
    this.expenseForm.reset({
      date: this.todayISO(),
      description: '',
      amount: null,
    });
    this.showCreateModal = true;
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
    this.expenseForm.reset();
    this.clearMessages();
  }

  openEditModal(expense: DailyExpense): void {
    this.selectedExpense = expense;
    this.clearMessages();
    const datePart = expense.date
      ? expense.date.split('T')[0]
      : this.todayISO();
    this.expenseForm.reset({
      date: datePart,
      description: expense.description,
      amount: expense.amount,
    });
    this.showEditModal = true;
  }

  closeEditModal(): void {
    this.showEditModal = false;
    this.selectedExpense = null;
    this.expenseForm.reset();
    this.clearMessages();
  }

  openDeleteModal(expense: DailyExpense): void {
    this.selectedExpense = expense;
    this.showDeleteModal = true;
  }

  closeDeleteModal(): void {
    this.showDeleteModal = false;
    this.selectedExpense = null;
  }

  // ── Helpers ───────────────────────────────────────────────────

  todayISO(): string {
    return new Date().toISOString().split('T')[0];
  }

  formatDate(dateStr: string | undefined): string {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  }

  formatCurrency(amount: number): string {
    return (amount ?? 0).toLocaleString('en-US', {
      style: 'currency',
      currency: 'USD',
      maximumFractionDigits: 0,
    });
  }

  hasError(ctrl: string): boolean {
    const c = this.expenseForm.get(ctrl);
    return !!(c && c.invalid && c.touched);
  }

  showSuccess(msg: string): void {
    this.successMessage = msg;
    setTimeout(() => (this.successMessage = ''), 3500);
  }

  clearMessages(): void {
    this.errorMessage = '';
    this.successMessage = '';
  }
}