/**
 * ============================================================
 * reorder.component.ts
 * ============================================================
 * Shows all products/variants that are LOW STOCK or OUT OF STOCK.
 *
 * Data source:
 *   - getProductsWithDetails()  → full product+variant list
 *   - Filtered locally:  stock > 0 && stock <= reorderLevel  → LOW
 *                        stock === 0                          → OUT
 *
 * Features:
 *  - KPI cards: low stock / out of stock / total variants / inventory value
 *  - Filter tabs: All / Low Stock / Out of Stock
 *  - Search by product, supplier, size
 *  - Edit variant (update stock, price, reorder level)
 *  - Delete variant (soft delete)
 *  - Restore product (if deleted)
 *  - Toast notifications
 *  - OnPush change detection
 * ============================================================
 */

import {
  Component, OnInit, OnDestroy,
  ChangeDetectionStrategy, ChangeDetectorRef,
  ViewChild, ElementRef
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormsModule, ReactiveFormsModule,
  FormBuilder, FormGroup, Validators
} from '@angular/forms';
import { Subject, takeUntil, finalize } from 'rxjs';
import {
  ProductService,
  Product,
  ProductVariant,
  UpdateVariantPayload
} from '../services/product.service';

// ── Local flat model for the table rows ──────────────────────
interface ReorderItem {
  productId:      number;
  variantId:      number;
  productName:    string;
  description:    string;
  supplierName:   string;
  categoryName:   string;
  size:           string;
  classType:      string;
  unitOfMeasure:  string;
  color:          string;
  location:       string;
  stock:          number;
  reorderLevel:   number;
  pricePerUnit:   number;
  pricePerLength: number;
  lengthInFeet:   number;
  isActive:       boolean;
  notes:          string;
  stockStatus:    'out' | 'low' | 'ok';   // derived
  _rawProduct:    Product;
  _rawVariant:    ProductVariant;
}

type ModalMode = 'edit' | 'delete' | 'stock' | null;
type FilterType = 'all' | 'low' | 'out';

@Component({
  selector: 'app-reorder',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './reorder.component.html',
  styleUrls: ['./reorder.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReorderComponent implements OnInit, OnDestroy {

  @ViewChild('modalContent') modalContent!: ElementRef;

  // ── Data ───────────────────────────────────────────────────
  allItems:    ReorderItem[] = [];   // full mapped list (all stock statuses)
  displayItems: ReorderItem[] = [];  // after filter + search
  selected:    ReorderItem | null = null;

  // ── UI state ───────────────────────────────────────────────
  loading    = false;
  saving     = false;
  searchTerm = '';
  activeFilter: FilterType = 'all';
  modalMode: ModalMode = null;

  toast: { msg: string; type: 'success' | 'error' } | null = null;
  private toastTimer: ReturnType<typeof setTimeout> | null = null;

  // ── Forms ──────────────────────────────────────────────────
  editForm!:  FormGroup;
  stockForm!: FormGroup;

  // ── Units list (matches inventory component) ───────────────
  readonly units = ['FT','LENGTH','PCS','MTR','PACK','UNIT','BOTTLE','BOX','KG','LITER'];

  private destroy$ = new Subject<void>();

  constructor(
    private svc: ProductService,
    private fb: FormBuilder,
    private cd: ChangeDetectorRef
  ) {}

  // ── Lifecycle ──────────────────────────────────────────────

  ngOnInit(): void {
    this.buildForms();
    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    if (this.toastTimer) clearTimeout(this.toastTimer);
  }

  // ── Data loading ───────────────────────────────────────────

  /**
   * Load all products with variants, then map to flat ReorderItem[].
   * Low-stock filter is applied locally so we can show KPIs for all.
   */
  loadData(): void {
    this.loading = true;
    this.cd.markForCheck();

    this.svc.getProductsWithDetails(true)   // includeInactive=true so we see everything
      .pipe(
        finalize(() => { this.loading = false; this.cd.markForCheck(); }),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.allItems = this.mapToReorderItems(res.data);
            console.log('[ReorderComponent] Total items mapped:', this.allItems.length);
          } else {
            this.showToast(res.message || 'Failed to load inventory.', 'error');
          }
          this.applyFilter();
        },
        error: (err) => {
          this.showToast('Cannot reach server. Check API is running.', 'error');
          console.error('[ReorderComponent]', err);
        }
      });
  }

  /**
   * Flatten Product[] + ProductVariant[] into ReorderItem[].
   * Each variant becomes one row.
   * Products without variants are skipped (nothing to reorder).
   */
  private mapToReorderItems(products: Product[]): ReorderItem[] {
    const items: ReorderItem[] = [];

    products.forEach(product => {
      if (!product.variants || product.variants.length === 0) return;

      product.variants.forEach(variant => {
        const stock  = variant.quantityInStock ?? 0;
        const reorder = variant.reorderLevel    ?? 0;

        // Determine stock status
        let stockStatus: ReorderItem['stockStatus'] = 'ok';
        if (stock === 0)              stockStatus = 'out';
        else if (stock <= reorder)    stockStatus = 'low';

        items.push({
          productId:      product.productId,
          variantId:      variant.variantId,
          productName:    product.name,
          description:    product.description || '',
          supplierName:   product.supplierName || '—',
          categoryName:   product.categoryName || '—',
          size:           variant.size          || '—',
          classType:      variant.classType     || '—',
          unitOfMeasure:  variant.unitOfMeasure || '—',
          color:          variant.color         || '',
          location:       variant.location      || '',
          stock,
          reorderLevel:   reorder,
          pricePerUnit:   variant.pricePerUnit   ?? 0,
          pricePerLength: variant.pricePerLength ?? 0,
          lengthInFeet:   variant.lengthInFeet   ?? 0,
          isActive:       variant.isActive,
          notes:          variant.notes          || '',
          stockStatus,
          _rawProduct:    product,
          _rawVariant:    variant
        });
      });
    });

    return items;
  }

  // ── Filter logic ───────────────────────────────────────────

  applyFilter(): void {
    let list = this.allItems;

    // Tab filter
    if (this.activeFilter === 'low') {
      list = list.filter(i => i.stockStatus === 'low');
    } else if (this.activeFilter === 'out') {
      list = list.filter(i => i.stockStatus === 'out');
    } else {
      // 'all' still only shows items that NEED attention
      list = list.filter(i => i.stockStatus !== 'ok');
    }

    // Search filter
    const term = this.searchTerm.toLowerCase().trim();
    if (term) {
      list = list.filter(i =>
        i.productName.toLowerCase().includes(term)   ||
        i.supplierName.toLowerCase().includes(term)  ||
        i.categoryName.toLowerCase().includes(term)  ||
        i.size.toLowerCase().includes(term)          ||
        i.classType.toLowerCase().includes(term)
      );
    }

    this.displayItems = list;
    this.cd.markForCheck();
  }

  setFilter(f: FilterType): void {
    this.activeFilter = f;
    this.applyFilter();
  }

  onSearch(val: string): void {
    this.searchTerm = val;
    this.applyFilter();
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.applyFilter();
  }

  // ── KPI getters ────────────────────────────────────────────

  get totalLowStock():  number { return this.allItems.filter(i => i.stockStatus === 'low').length; }
  get totalOutOfStock(): number { return this.allItems.filter(i => i.stockStatus === 'out').length; }
  get totalNeedReorder(): number { return this.allItems.filter(i => i.stockStatus !== 'ok').length; }
  get uniqueSuppliers(): number {
    return new Set(
      this.allItems.filter(i => i.stockStatus !== 'ok').map(i => i.supplierName)
    ).size;
  }

  // ── Urgency helper ─────────────────────────────────────────

  /**
   * Returns urgency level as a percentage for the mini progress bar.
   * 0% stock = 100% urgency.
   */
  urgencyPct(item: ReorderItem): number {
    if (item.reorderLevel === 0) return item.stock === 0 ? 100 : 0;
    const pct = (item.stock / item.reorderLevel) * 100;
    return Math.min(Math.round(pct), 100);
  }

  /** How many units short of reorder level */
  shortfall(item: ReorderItem): number {
    return Math.max(0, item.reorderLevel - item.stock);
  }

  // ── Modal openers ──────────────────────────────────────────

  openEdit(item: ReorderItem): void {
    this.selected = item;
    this.editForm.patchValue({
      size:           item.size,
      color:          item.color,
      classType:      item.classType,
      unitOfMeasure:  item.unitOfMeasure,
      pricePerUnit:   item.pricePerUnit,
      pricePerLength: item.pricePerLength,
      lengthInFeet:   item.lengthInFeet,
      quantityInStock: item.stock,
      reorderLevel:   item.reorderLevel,
      location:       item.location,
      notes:          item.notes,
      isActive:       item.isActive
    });
    this.modalMode = 'edit';
  }

  openDelete(item: ReorderItem): void {
    this.selected  = item;
    this.modalMode = 'delete';
  }

  openStockAdjust(item: ReorderItem): void {
    this.selected = item;
    this.stockForm.patchValue({
      newQuantity: item.stock,
      reason: 'Reorder stock received'
    });
    this.modalMode = 'stock';
  }

  closeModal(): void {
    this.modalMode = null;
    this.selected  = null;
  }

  // ── CRUD ───────────────────────────────────────────────────

  /** PUT /api/Products/variants/:id — update variant details */
  submitEdit(): void {
    if (this.editForm.invalid || !this.selected) {
      this.editForm.markAllAsTouched(); return;
    }
    this.saving = true; this.cd.markForCheck();

    const fv = this.editForm.getRawValue();
    const payload: UpdateVariantPayload = {
      variantId:      this.selected.variantId,
      size:           fv.size,
      classType:      fv.classType,
      unitOfMeasure:  fv.unitOfMeasure,
      quantityInStock: Number(fv.quantityInStock),
      pricePerUnit:   Number(fv.pricePerUnit),
      pricePerLength: Number(fv.pricePerLength) || 0,
      lengthInFeet:   Number(fv.lengthInFeet)   || 0,
      reorderLevel:   Number(fv.reorderLevel),
      isActive:       fv.isActive,
      color:          fv.color    || '',
      location:       fv.location || '',
      notes:          fv.notes    || ''
    };

    this.svc.updateVariant(this.selected.variantId, payload)
      .pipe(finalize(() => { this.saving = false; this.cd.markForCheck(); }))
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.closeModal();
            this.showToast('Variant updated successfully!', 'success');
            this.loadData();
          } else {
            this.showToast(res.message || 'Update failed.', 'error');
          }
        },
        error: (err) => this.showToast('Update failed. Check API logs.', 'error')
      });
  }

  /** PATCH /api/Products/variants/:id/stock — quick stock adjustment */
  submitStockAdjust(): void {
    if (this.stockForm.invalid || !this.selected) {
      this.stockForm.markAllAsTouched(); return;
    }
    this.saving = true; this.cd.markForCheck();

    const fv = this.stockForm.getRawValue();

    this.svc.updateVariantStock(
      this.selected.variantId,
      Number(fv.newQuantity),
      fv.reason || 'Reorder stock received'
    )
      .pipe(finalize(() => { this.saving = false; this.cd.markForCheck(); }))
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.closeModal();
            this.showToast('Stock updated successfully!', 'success');
            this.loadData();
          } else {
            this.showToast(res.message || 'Stock update failed.', 'error');
          }
        },
        error: () => this.showToast('Stock update failed. Check API logs.', 'error')
      });
  }

  /** DELETE /api/Products/variants/:id — soft delete variant */
  confirmDelete(): void {
    if (!this.selected) return;
    this.saving = true; this.cd.markForCheck();

    this.svc.deleteVariant(this.selected.variantId)
      .pipe(finalize(() => { this.saving = false; this.cd.markForCheck(); }))
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.closeModal();
            this.showToast('Variant removed from reorder list.', 'success');
            this.loadData();
          } else {
            this.showToast(res.message || 'Delete failed.', 'error');
          }
        },
        error: () => this.showToast('Delete failed. Check API logs.', 'error')
      });
  }

  // ── Helpers ────────────────────────────────────────────────

  fmtCurrency(n: number): string {
    return new Intl.NumberFormat('en-PK', {
      style: 'currency', currency: 'PKR', maximumFractionDigits: 0
    }).format(n);
  }

  showToast(msg: string, type: 'success' | 'error'): void {
    this.toast = { msg, type }; this.cd.markForCheck();
    if (this.toastTimer) clearTimeout(this.toastTimer);
    this.toastTimer = setTimeout(() => { this.toast = null; this.cd.markForCheck(); }, 4000);
  }

  err(form: FormGroup, field: string): string {
    const c = form.get(field);
    if (!c?.touched || !c.errors) return '';
    if (c.errors['required']) return 'Required.';
    if (c.errors['min'])      return `Min value is ${c.errors['min'].min}.`;
    return '';
  }

  // ── Form builders ──────────────────────────────────────────

  private buildForms(): void {

    // Full variant edit form
    this.editForm = this.fb.group({
      size:           ['', Validators.required],
      color:          [''],
      classType:      ['', Validators.required],
      unitOfMeasure:  ['', Validators.required],
      pricePerUnit:   [0, [Validators.required, Validators.min(0.01)]],
      pricePerLength: [0],
      lengthInFeet:   [0],
      quantityInStock:[0, [Validators.required, Validators.min(0)]],
      reorderLevel:   [0, [Validators.required, Validators.min(0)]],
      location:       [''],
      notes:          [''],
      isActive:       [true]
    });

    // Quick stock adjustment form
    this.stockForm = this.fb.group({
      newQuantity: [0, [Validators.required, Validators.min(0)]],
      reason:      ['Reorder stock received', Validators.required]
    });
  }
}