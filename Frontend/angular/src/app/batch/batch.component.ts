import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormsModule,
  ReactiveFormsModule,
  FormGroup,
  FormControl,
  Validators,
} from '@angular/forms';
import {
  PurchaseBatchService,
  PurchaseBatch,
  VariantOption,
  CreateBatchDto,
  UpdateBatchDto,
} from '../services/purchase-batch.service';
import { SupplierService, Supplier } from '../services/supplier.service';

// =====================
// LOCAL INTERFACES
// =====================

/** Item inside Add Batch form (local only, not from API) */
interface BatchLineLocal {
  variantId: number;
  quantity: number;
  costPrice: number;
  lineTotal: number;
  productName: string;
  size: string;
  classType: string;
  salePrice: number;
}

@Component({
  selector: 'app-batch',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './batch.component.html',
  styleUrls: ['./batch.component.css'],
})
export class BatchComponent implements OnInit {

  // ── EDIT VIEW ──
editBatch = {
  name: '',
  supplierId: 0,
  date: '',
  status: 'Partial',
};
editPaidAmount: number = 0;
editTotalAmount: number = 0;
// =====================
// SUPPLIER SEARCH (Add Form)
// =====================
supplierSearchTerm = '';
filteredSuppliers: Supplier[] = [];
showSupplierDropdown = false;
selectedSupplierObj: Supplier | null = null;

// =====================
// SUPPLIER SEARCH (Edit Form)
// =====================
editSupplierSearchTerm = '';
editFilteredSuppliers: Supplier[] = [];
showEditSupplierDropdown = false;
  // =====================
  // LIST VIEW
  // =====================
  batches: PurchaseBatch[] = [];
  filteredBatches: PurchaseBatch[] = [];
  searchTerm = '';
  isLoading = false;
  errorMessage = '';

  // =====================
  // VIEW TOGGLES
  // =====================
  showAddBatchForm = false;
  showEditForm    = false;
  showDetailView  = false;   // ← NEW

  // =====================
  // ADD BATCH
  // =====================
  batchId = 0;

  batch = {
    name: '',
    supplierId: 0,
    date: new Date().toISOString().split('T')[0],
    status: 'Partial',
  };

  suppliers: Supplier[] = [];

  // Variant search
  variantSearchTerm = '';
  variantOptions: VariantOption[] = [];
  showVariantDropdown = false;
  isVariantLoading = false;
  selectedVariant: VariantOption | null = null;

  // New line inputs
newLine = { quantity: 0, costPrice: 0, salePrice: 0 };

  // Items list
  batchItems: BatchLineLocal[] = [];

  // Payment
  paidAmount = 0;

  // Computed getters
  get totalAmount(): number {
    return this.batchItems.reduce((sum, i) => sum + i.lineTotal, 0);
  }

  get remaining(): number {
    return this.totalAmount - this.paidAmount;
  }

  get paidPercentage(): number {
    return this.totalAmount > 0
      ? Math.min(100, (this.paidAmount / this.totalAmount) * 100)
      : 0;
  }

  get canAddLine(): boolean {
    return (
      !!this.selectedVariant &&
      this.newLine.quantity > 0 &&
      this.newLine.costPrice > 0
    );
  }

  // =====================
  // EDIT BATCH
  // =====================
  editingBatch: PurchaseBatch | null = null;
  batchForm!: FormGroup;

  // =====================
  // DETAIL VIEW  ← NEW
  // =====================
  detailBatch: PurchaseBatch | null = null;       // summary shown in header/cards
  batchDetail: PurchaseBatch | null = null;       // full data with items[] from API
  isDetailLoading = false;
  detailError = '';

  /** Paid % for the detail view progress bar */
  get detailPaidPct(): number {
    if (!this.detailBatch || this.detailBatch.totalPrice <= 0) return 0;
    return Math.min(100, (this.detailBatch.paid / this.detailBatch.totalPrice) * 100);
  }

  // =====================
  // SAVE STATE
  // =====================
  isSaving  = false;
  saveError = '';

  constructor(
    private purchaseBatchService: PurchaseBatchService,
    private supplierService: SupplierService,
  ) {}

  ngOnInit(): void {
    this.loadBatches();
    this.loadSuppliers();
    this.initEditForm();
  }

  // =====================
  // LOAD DATA
  // =====================
  loadBatches(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.purchaseBatchService.getAllBatches().subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success && res.data) {
          this.batches = res.data;
          this.filteredBatches = res.data;
        } else {
          this.errorMessage = res.message || 'Batches load nahi hue';
        }
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = 'Server se connect nahi ho pa raha!';
        console.error(err);
      },
    });
  }

  loadSuppliers(): void {
    this.supplierService.getAllSuppliers().subscribe({
      next: (res) => {
        if (res.success && res.data) this.suppliers = res.data;
      },
      error: (err) => console.error('Suppliers error:', err),
    });
  }
// ADD FORM — supplier search
onSupplierSearch(): void {
  const term = this.supplierSearchTerm.toLowerCase().trim();
  this.filteredSuppliers = term
    ? this.suppliers.filter(s => s.name.toLowerCase().includes(term))
    : this.suppliers;
  this.showSupplierDropdown = true;
  // Agar text change hua toh selection clear karo
  this.batch.supplierId = 0;
  this.selectedSupplierObj = null;
}

selectSupplier(supplier: Supplier): void {
  this.selectedSupplierObj = supplier;
  this.batch.supplierId = supplier.supplierId;
  this.supplierSearchTerm = supplier.name;  // input mein naam dikhao
  this.showSupplierDropdown = false;
}

// EDIT FORM — supplier search
onEditSupplierSearch(): void {
  const term = this.editSupplierSearchTerm.toLowerCase().trim();
  this.editFilteredSuppliers = term
    ? this.suppliers.filter(s => s.name.toLowerCase().includes(term))
    : this.suppliers;
  this.showEditSupplierDropdown = true;
  this.editBatch.supplierId = 0;
}

selectEditSupplier(supplier: Supplier): void {
  this.editBatch.supplierId = supplier.supplierId;
  this.editSupplierSearchTerm = supplier.name;
  this.showEditSupplierDropdown = false;
}
  // =====================
  // SEARCH
  // =====================
  onSearch(): void {
    if (!this.searchTerm.trim()) {
      this.filteredBatches = this.batches;
      return;
    }
    const term = this.searchTerm.toLowerCase();
    this.filteredBatches = this.batches.filter(
      (b) =>
        b.batchName.toLowerCase().includes(term) ||
        b.supplierName.toLowerCase().includes(term),
    );
  }

  onRefresh(): void {
    this.searchTerm = '';
    this.loadBatches();
  }

  // =====================
  // ADD BATCH — OPEN
  // =====================
  onAddNew(): void {
    this.showAddBatchForm = true;
    this.showEditForm     = false;
    this.showDetailView   = false;
    this.resetAddForm();

    this.purchaseBatchService.getNextId().subscribe({
      next: (res) => { if (res.success) this.batchId = res.data; },
      error: (err) => console.error('Next ID error:', err),
    });
  }

resetAddForm(): void {
  this.batchItems = [];
  this.paidAmount = 0;
  this.newLine = { quantity: 0, costPrice: 0, salePrice: 0 };
  this.selectedVariant = null;
  this.variantSearchTerm = '';
  this.variantOptions = [];
  this.showVariantDropdown = false;
  this.saveError = '';

  // 👇 Yeh naya add karo
  this.supplierSearchTerm = '';
  this.selectedSupplierObj = null;
  this.showSupplierDropdown = false;
  this.filteredSuppliers = [];

  this.batch = {
    name: '',
    supplierId: 0,
    date: new Date().toISOString().split('T')[0],
    status: 'Partial',
  };
}
  // =====================
  // VARIANT SEARCH
  // =====================
  onVariantSearch(): void {
    if (this.variantSearchTerm.trim().length < 1) {
      this.variantOptions = [];
      this.showVariantDropdown = false;
      return;
    }

    this.isVariantLoading = true;
    this.showVariantDropdown = true;

    this.purchaseBatchService.getVariants(this.variantSearchTerm).subscribe({
      next: (res) => {
        this.isVariantLoading = false;
        if (res.success && res.data) this.variantOptions = res.data;
      },
      error: (err) => {
        this.isVariantLoading = false;
        console.error('Variant search error:', err);
      },
    });
  }

  selectVariantOption(variant: VariantOption): void {
  this.selectedVariant = variant;
  this.variantSearchTerm = `${variant.productName} — ${variant.size}${variant.classType ? ' (' + variant.classType + ')' : ''}`;
  this.showVariantDropdown = false;
  this.newLine.salePrice = variant.salePrice;  // sale price auto-fill
  // costPrice zero hi rahega — user manually bharega
}

  // =====================
  // ADD / REMOVE ITEMS
  // =====================
  addToBatch(): void {
    if (!this.canAddLine || !this.selectedVariant) return;

    const lineTotal = +(this.newLine.quantity * this.newLine.costPrice).toFixed(2);

    this.batchItems.push({
  variantId:   this.selectedVariant.variantId,
  quantity:    this.newLine.quantity,
  costPrice:   this.newLine.costPrice,
  lineTotal,
  productName: this.selectedVariant.productName,
  size:        this.selectedVariant.size,
  classType:   this.selectedVariant.classType,
  salePrice:   this.newLine.salePrice,   // ← newLine.salePrice use karo
});

this.newLine = { quantity: 0, costPrice: 0, salePrice: 0 };  // ← SAHI
//     this.selectedVariant = null;
    this.variantSearchTerm = '';
    this.variantOptions = [];
    this.showVariantDropdown = false;
  }

  removeItem(index: number): void {
    if (confirm('Remove this item?')) {
      this.batchItems.splice(index, 1);
    }
  }

  // =====================
  // SAVE BATCH
  // =====================
  saveBatch(): void {
    if (!this.batch.name || !this.batch.supplierId || this.batchItems.length === 0)
      return;

    this.isSaving = true;
    this.saveError = '';

    const dto: CreateBatchDto = {
  supplierId: Number(this.batch.supplierId),
  batchName:  this.batch.name,
  totalPrice: this.totalAmount,
  paid:       this.paidAmount,
  status:     this.batch.status,
  purchaseDate: new Date(this.batch.date).toISOString(),   // ← ADD THIS
  items: this.batchItems.map((item) => ({
    variantId:        item.variantId,
    quantityReceived: item.quantity,
    costPrice:        item.costPrice,
    salePrice:        item.salePrice,
  })),
};

    this.purchaseBatchService.createBatch(dto).subscribe({
      next: (res) => {
        this.isSaving = false;
        if (res.success) {
          this.cancel();
          this.loadBatches();
        } else {
          this.saveError = res.message || 'Save fail ho gayi';
        }
      },
      error: (err) => {
        this.isSaving = false;
        this.saveError = 'Server error! Dobara try karo.';
        console.error(err);
      },
    });
  }

  cancel(): void {
    this.showAddBatchForm = false;
    this.resetAddForm();
  }

  // =====================
  // EDIT BATCH
  // =====================
  initEditForm(): void {
    this.batchForm = new FormGroup({
      batchName:  new FormControl('', [Validators.required, Validators.minLength(2)]),
      supplierId: new FormControl('', [Validators.required]),
      totalPrice: new FormControl(0,  [Validators.required, Validators.min(0)]),
      paid:       new FormControl(0,  [Validators.required, Validators.min(0)]),
      status:     new FormControl('', [Validators.required]),
    });
  }

  onEdit(batch: PurchaseBatch): void {
  this.editingBatch     = batch;
  this.showEditForm     = true;
  this.showAddBatchForm = false;
  this.showDetailView   = false;
  this.saveError        = '';

  this.editBatch = {
    name:       batch.batchName,
    supplierId: batch.supplierId,
    date:       batch.createdAt?.split('T')[0] || new Date().toISOString().split('T')[0],
    status:     batch.status,
  };
  this.editPaidAmount  = batch.paid;
  this.editTotalAmount = batch.totalPrice;

  // 👇 Yeh naya add karo — supplier naam pre-fill karo
  this.editSupplierSearchTerm = batch.supplierName;
  this.editFilteredSuppliers = [];
  this.showEditSupplierDropdown = false;
}
@HostListener('document:click', ['$event'])
onDocumentClick(event: MouseEvent): void {
  const target = event.target as HTMLElement;
  // Agar click kisi dropdown ke andar nahi hua toh band karo
  if (!target.closest('.form-field')) {
    this.showSupplierDropdown = false;
    this.showEditSupplierDropdown = false;
    this.showVariantDropdown = false;
  }
}
  onSubmit(): void {
  if (!this.editingBatch) return;
  if (!this.editBatch.name || !this.editBatch.supplierId) {
    this.saveError = 'Batch name aur supplier required hain!';
    return;
  }

  this.isSaving  = true;
  this.saveError = '';

  const dto: UpdateBatchDto = {
    supplierId: Number(this.editBatch.supplierId),
    batchName:  this.editBatch.name,
    totalPrice: this.editTotalAmount,
    paid:       this.editPaidAmount,
    status:     this.editBatch.status,
  };

  this.purchaseBatchService.updateBatch(this.editingBatch.batchId, dto).subscribe({
    next: (res) => {
      this.isSaving = false;
      if (res.success) {
        this.onCancel();
        this.loadBatches();
      } else {
        this.saveError = res.message || 'Update fail ho gayi';
      }
    },
    error: (err) => {
      this.isSaving = false;
      this.saveError = 'Server error!';
      console.error(err);
    },
  });
}

  onCancel(): void {
  this.showEditForm  = false;
  this.editingBatch  = null;
  this.saveError     = '';
}

  // =====================
  // DETAIL VIEW  ← NEW
  // =====================

  /**
   * Called when "Details" button is clicked.
   * Switches to detail view and loads full batch data (with items[]) from API.
   */
  onAddDetails(batch: PurchaseBatch): void {
    this.detailBatch      = batch;
    this.showDetailView   = true;
    this.showAddBatchForm = false;
    this.showEditForm     = false;
    this.batchDetail      = null;
    this.detailError      = '';

    this.loadBatchDetail(batch.batchId);
  }

  /** Fetches GET /api/PurchaseBatches/{id} — returns PurchaseBatch with items[] */
  loadBatchDetail(batchId: number): void {
    this.isDetailLoading = true;
    this.detailError     = '';

    this.purchaseBatchService.getBatchById(batchId).subscribe({
      next: (res) => {
        this.isDetailLoading = false;
        if (res.success && res.data) {
          this.batchDetail = res.data;
          // Also update detailBatch with fresh data from API
          this.detailBatch = res.data;
        } else {
          this.detailError = res.message || 'Detail load nahi hui';
        }
      },
      error: (err) => {
        this.isDetailLoading = false;
        this.detailError = 'Server error! Dobara try karo.';
        console.error(err);
      },
    });
  }

  /** Back button from detail view → returns to list */
  onBackFromDetail(): void {
    this.showDetailView = false;
    this.detailBatch    = null;
    this.batchDetail    = null;
    this.detailError    = '';
  }

  // =====================
  // FORM HELPERS
  // =====================
  isFieldInvalid(field: string): boolean {
    const control = this.batchForm.get(field);
    return !!(control && control.invalid && (control.dirty || control.touched));
  }

  getErrorMessage(field: string): string {
    const control = this.batchForm.get(field);
    if (!control || !control.errors) return '';
    if (control.errors['required'])  return `${field} required hai`;
    if (control.errors['minlength']) return `Minimum ${control.errors['minlength'].requiredLength} characters chahiye`;
    if (control.errors['min'])       return 'Value 0 se kam nahi ho sakti';
    return 'Invalid input';
  }
}