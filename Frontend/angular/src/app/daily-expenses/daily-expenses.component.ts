import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { 
  DailyExpensesService, 
  DailyExpense, 
  DailyExpenseDto,
  DailyExpenseUpdateDto 
} from '../services/daily-expenses.service';

@Component({
  selector: 'app-daily-expenses',
  templateUrl: './daily-expenses.component.html',
  styleUrls: ['./daily-expenses.component.css']
})
export class DailyExpensesComponent implements OnInit {
  expenses: DailyExpense[] = [];
  totalAmount: number = 0;
  loading: boolean = false;
  errorMessage: string = '';
  successMessage: string = '';

  // Pagination
  pageNumber: number = 1;
  pageSize: number = 10;
  totalCount: number = 0;
  totalPages: number = 0;

  // Modal states
  showCreateModal: boolean = false;
  showEditModal: boolean = false;
  showDeleteModal: boolean = false;
  selectedExpense: DailyExpense | null = null;

  // Forms
  expenseForm!: FormGroup;
  searchForm!: FormGroup;
  dateRangeForm!: FormGroup;

  // View mode
  viewMode: 'all' | 'paginated' | 'search' | 'dateRange' = 'paginated';

  constructor(
    private expenseService: DailyExpensesService,
    private fb: FormBuilder
  ) { }

  ngOnInit(): void {
    this.initializeForms();
    this.loadExpensesPaginated();
    this.loadTotalAmount();
  }

  initializeForms(): void {
    this.expenseForm = this.fb.group({
      date: [new Date().toISOString().split('T')[0], Validators.required],
      description: ['', [Validators.required, Validators.minLength(3)]],
      amount: [0, [Validators.required, Validators.min(0.01)]],
      category: ['']
    });

    this.searchForm = this.fb.group({
      term: ['', Validators.required]
    });

    this.dateRangeForm = this.fb.group({
      startDate: ['', Validators.required],
      endDate: ['', Validators.required]
    });
  }

  // Load expenses (paginated)
  loadExpensesPaginated(): void {
    this.loading = true;
    this.errorMessage = '';
    this.viewMode = 'paginated';

    this.expenseService.getExpensesPaginated(this.pageNumber, this.pageSize).subscribe({
      next: (response) => {
        if (response.success) {
          this.expenses = response.data;
          this.totalCount = response.totalCount;
          this.totalPages = response.totalPages;
        }
        this.loading = false;
      },
      error: (error) => {
        this.errorMessage = 'Failed to load expenses';
        console.error(error);
        this.loading = false;
      }
    });
  }

  // Load all expenses
  loadAllExpenses(): void {
    this.loading = true;
    this.errorMessage = '';
    this.viewMode = 'all';

    this.expenseService.getAllExpenses().subscribe({
      next: (response) => {
        if (response.success) {
          this.expenses = response.data;
        }
        this.loading = false;
      },
      error: (error) => {
        this.errorMessage = 'Failed to load expenses';
        console.error(error);
        this.loading = false;
      }
    });
  }

  // Search expenses
  searchExpenses(): void {
    if (this.searchForm.invalid) return;

    this.loading = true;
    this.errorMessage = '';
    this.viewMode = 'search';

    const term = this.searchForm.value.term;

    this.expenseService.searchExpenses(term).subscribe({
      next: (response) => {
        if (response.success) {
          this.expenses = response.data;
          this.successMessage = response.message;
          setTimeout(() => this.successMessage = '', 3000);
        }
        this.loading = false;
      },
      error: (error) => {
        this.errorMessage = 'Search failed';
        console.error(error);
        this.loading = false;
      }
    });
  }

  // Filter by date range
  filterByDateRange(): void {
    if (this.dateRangeForm.invalid) return;

    this.loading = true;
    this.errorMessage = '';
    this.viewMode = 'dateRange';

    const { startDate, endDate } = this.dateRangeForm.value;

    this.expenseService.getExpensesByDateRange(new Date(startDate), new Date(endDate)).subscribe({
      next: (response) => {
        if (response.success) {
          this.expenses = response.data;
          this.successMessage = response.message;
          setTimeout(() => this.successMessage = '', 3000);
        }
        this.loading = false;
      },
      error: (error) => {
        this.errorMessage = 'Failed to filter expenses';
        console.error(error);
        this.loading = false;
      }
    });
  }

  // Load total amount
  loadTotalAmount(startDate?: Date, endDate?: Date): void {
    this.expenseService.getTotalAmount(startDate, endDate).subscribe({
      next: (response) => {
        if (response.success) {
          this.totalAmount = response.data;
        }
      },
      error: (error) => {
        console.error('Failed to load total amount', error);
      }
    });
  }

  // Create expense
  createExpense(): void {
    if (this.expenseForm.invalid) return;

    this.loading = true;
    this.errorMessage = '';

    const dto: DailyExpenseDto = {
      date: new Date(this.expenseForm.value.date),
      description: this.expenseForm.value.description,
      amount: this.expenseForm.value.amount,
      category: this.expenseForm.value.category || undefined
    };

    this.expenseService.createExpense(dto).subscribe({
      next: (response) => {
        if (response.success) {
          this.successMessage = response.message;
          this.closeCreateModal();
          this.loadExpensesPaginated();
          this.loadTotalAmount();
          setTimeout(() => this.successMessage = '', 3000);
        }
        this.loading = false;
      },
      error: (error) => {
        this.errorMessage = error.error?.message || 'Failed to create expense';
        this.loading = false;
      }
    });
  }

  // Update expense
  updateExpense(): void {
    if (this.expenseForm.invalid || !this.selectedExpense) return;

    this.loading = true;
    this.errorMessage = '';

    const dto: DailyExpenseUpdateDto = {
      date: new Date(this.expenseForm.value.date),
      description: this.expenseForm.value.description,
      amount: this.expenseForm.value.amount,
      category: this.expenseForm.value.category || undefined
    };

    this.expenseService.updateExpense(this.selectedExpense.expenseId, dto).subscribe({
      next: (response) => {
        if (response.success) {
          this.successMessage = response.message;
          this.closeEditModal();
          this.loadExpensesPaginated();
          this.loadTotalAmount();
          setTimeout(() => this.successMessage = '', 3000);
        }
        this.loading = false;
      },
      error: (error) => {
        this.errorMessage = error.error?.message || 'Failed to update expense';
        this.loading = false;
      }
    });
  }

  // Delete expense
  confirmDelete(): void {
    if (!this.selectedExpense) return;

    this.loading = true;
    this.errorMessage = '';

    this.expenseService.deleteExpense(this.selectedExpense.expenseId).subscribe({
      next: (response) => {
        if (response.success) {
          this.successMessage = response.message;
          this.closeDeleteModal();
          this.loadExpensesPaginated();
          this.loadTotalAmount();
          setTimeout(() => this.successMessage = '', 3000);
        }
        this.loading = false;
      },
      error: (error) => {
        this.errorMessage = error.error?.message || 'Failed to delete expense';
        this.loading = false;
      }
    });
  }

  // Pagination
  nextPage(): void {
    if (this.pageNumber < this.totalPages) {
      this.pageNumber++;
      this.loadExpensesPaginated();
    }
  }

  previousPage(): void {
    if (this.pageNumber > 1) {
      this.pageNumber--;
      this.loadExpensesPaginated();
    }
  }

  goToPage(page: number): void {
    this.pageNumber = page;
    this.loadExpensesPaginated();
  }

  // Modal handlers
  openCreateModal(): void {
    this.expenseForm.reset({
      date: new Date().toISOString().split('T')[0],
      description: '',
      amount: 0,
      category: ''
    });
    this.showCreateModal = true;
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
    this.expenseForm.reset();
  }

  openEditModal(expense: DailyExpense): void {
    this.selectedExpense = expense;
    this.expenseForm.patchValue({
      date: new Date(expense.date).toISOString().split('T')[0],
      description: expense.description,
      amount: expense.amount,
      category: expense.category || ''
    });
    this.showEditModal = true;
  }

  closeEditModal(): void {
    this.showEditModal = false;
    this.selectedExpense = null;
    this.expenseForm.reset();
  }

  openDeleteModal(expense: DailyExpense): void {
    this.selectedExpense = expense;
    this.showDeleteModal = true;
  }

  closeDeleteModal(): void {
    this.showDeleteModal = false;
    this.selectedExpense = null;
  }

  // Reset filters
  resetFilters(): void {
    this.searchForm.reset();
    this.dateRangeForm.reset();
    this.pageNumber = 1;
    this.loadExpensesPaginated();
  }

  // Utility
  formatDate(date: Date): string {
    return new Date(date).toLocaleDateString();
  }

  formatCurrency(amount: number): string {
    return amount.toLocaleString('en-US', { style: 'currency', currency: 'USD' });
  }
}