import { Component, OnInit, ViewChild, ElementRef , OnDestroy  } from '@angular/core';
import { UiStateService } from '../services/ui-state.service';

import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { gsap } from 'gsap';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import {
  ProductService,
  Product,
  ProductVariant,
  Category,
  Supplier,
  CreateProductPayload,
  CreateVariantPayload,
  UpdateVariantPayload,
} from '../services/product.service';

interface InventoryItem {
  productId: number;
  variantId: number;
  description: string;
  supplier: string;
  active: boolean;
  product: string;
  size: string;
  unit: string;
  class_type: string;
  price_per_unit: number;
  price_per_length: number;
  lengthFt: number;
  stock: number;
  reorder: number;
  minQty: number;
  _rawProduct: Product;
  _rawVariant: ProductVariant | null;
}

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './inventory.component.html',
  styleUrl: './inventory.component.css',
})
export class InventoryComponent implements OnInit, OnDestroy {
  @ViewChild('modalContent') modalContent!: ElementRef;

  items: InventoryItem[] = [];
  allProducts: Product[] = [];
  allProductsSimple: Product[] = [];

  totalItems = 0;
  lowStock = 0;
  outOfStock = 0;
  inventoryValue = 0;

  isLoading = false;
  errorMessage = '';
  successMessage = '';

  ngOnDestroy(): void {
    this.uiState.setInventory({
      searchTerm: this.searchTerm,
      filter:     this.filter,
    });
  }

  searchTerm = '';
  filter = 'all';

  showAddProductForm = false;
  showAddVariantForm = false;
  editProductForm = false;
  editVariantForm = false;

  selectedProductId: number | null = null;
  selectedVariantId: number | null = null;

  categories: Category[] = [];
  suppliers: Supplier[] = [];
  units = ['FT', 'LENGTH', 'PCS', 'MTR', 'PACK', 'UNIT', 'BOTTLE', 'BOX', 'KG', 'LITER'];

  productForm!: FormGroup;
  variantForm!: FormGroup;

  // ─── Combobox State ──────────────────────────────────────────────────────────

  // Category
  categorySearch = '';
  showCategoryDropdown = false;
  selectedCategoryLabel = '';

  // Supplier
  supplierSearch = '';
  showSupplierDropdown = false;
  selectedSupplierLabel = '';

  // Product (variant form)
  productSearch = '';
  showProductDropdown = false;
  selectedProductLabel = '';

  constructor(
    private fb: FormBuilder,
    private productService: ProductService,
    private uiState: UiStateService,
  ) {}

  ngOnInit() {
    // State restore
    const s = this.uiState.getInventory();
    this.searchTerm = s.searchTerm || '';
    this.filter     = s.filter     || 'all';

    this.initForms();
    this.loadAll();
  }

  initForms() {
    this.productForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      description: [''],
      categoryId: ['', Validators.required],
      isActive: [true],
      notes: [''],
    });

    this.variantForm = this.fb.group({
      productId: ['', Validators.required],
      size: ['', Validators.required],
      color: [''],
      classType: [''],
      unitOfMeasure: ['', Validators.required],
      pricePerUnit: [0, [Validators.required, Validators.min(0.01)]],
      pricePerLength: [0],
      lengthInFeet: [0],
      quantityInStock: [0, [Validators.required, Validators.min(0)]],
      reorderLevel: [0, [Validators.required, Validators.min(0)]],
      location: [''],
      notes: [''],
    });
  }

  loadAll() {
    this.loadProducts();
    this.loadProductsSimple();
    this.loadCategoriesAndSuppliers();
    this.loadInventoryValue();
  }

  loadProductsSimple() {
    this.productService.getAllProducts().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.allProductsSimple = response.data;
        }
      },
      error: (err) => console.error('Simple products load failed:', err),
    });
  }

  loadProducts() {
    this.isLoading = true;
    this.errorMessage = '';

    this.productService.getProductsWithDetails().subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success && response.data) {
          this.allProducts = response.data;
          this.mapProductsToInventoryItems(response.data);
          this.updateKPIs();
        } else {
          this.errorMessage = response.message || 'Products load nahi hue';
        }
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = 'Server se connect nahi ho pa raha!';
        console.error(err);
      },
    });
  }

  mapProductsToInventoryItems(products: Product[]) {
    this.items = [];
    products.forEach((product) => {
      if (product.variants && product.variants.length > 0) {
        product.variants.forEach((variant) => {
          this.items.push({
            productId: product.productId,
            variantId: variant.variantId,
            description: product.description || product.name,
            supplier: product.supplierName || '—',
            active: product.isActive,
            product: product.name,
            size: variant.size || '—',
            unit: variant.unitOfMeasure || '—',
            class_type: variant.classType || '—',
            price_per_unit: variant.pricePerUnit || 0,
            price_per_length: variant.pricePerLength || 0,
            lengthFt: variant.lengthInFeet || 0,
            stock: variant.quantityInStock || 0,
            reorder: variant.reorderLevel || 10,
            minQty: variant.reorderLevel || 5,
            _rawProduct: product,
            _rawVariant: variant,
          });
        });
      } else {
        this.items.push({
          productId: product.productId,
          variantId: 0,
          description: product.description || product.name,
          supplier: product.supplierName || '—',
          active: product.isActive,
          product: product.name,
          size: '—', unit: '—', class_type: '—',
          price_per_unit: 0, price_per_length: 0, lengthFt: 0,
          stock: 0, reorder: 0, minQty: 0,
          _rawProduct: product, _rawVariant: null,
        });
      }
    });
  }

  loadCategoriesAndSuppliers() {
    this.productService.getAllCategories().subscribe({
      next: (res) => { if (res.success && res.data) this.categories = res.data; },
      error: (err) => console.error('Categories load failed:', err),
    });
    this.productService.getAllSuppliers().subscribe({
      next: (res) => { if (res.success && res.data) this.suppliers = res.data; },
      error: (err) => console.error('Suppliers load failed:', err),
    });
  }

  loadInventoryValue() {
    this.productService.getInventoryValue().subscribe({
      next: (res) => { if (res.success) this.inventoryValue = res.data; },
      error: (err) => console.error('Inventory value load failed:', err),
    });
  }

  updateKPIs() {
    this.totalItems = this.items.length;
    this.lowStock = this.items.filter(i => i.stock > 0 && i.stock <= i.reorder).length;
    this.outOfStock = this.items.filter(i => i.stock === 0).length;
  }

  get filteredItems(): InventoryItem[] {
    let filtered = this.items;
    if (this.searchTerm.trim()) {
      const term = this.searchTerm.toLowerCase();
      filtered = filtered.filter(item =>
        item.product.toLowerCase().includes(term) ||
        item.description.toLowerCase().includes(term) ||
        item.supplier.toLowerCase().includes(term) ||
        item.size.toLowerCase().includes(term),
      );
    }
    if (this.filter === 'low') filtered = filtered.filter(i => i.stock > 0 && i.stock <= i.reorder);
    else if (this.filter === 'out') filtered = filtered.filter(i => i.stock === 0);
    return filtered;
  }

  showAll()      { this.filter = 'all'; this.loadProducts(); }
  showLowStock() { this.filter = 'low'; }
  showOutStock() { this.filter = 'out'; }

  // ─── Combobox Filtered Lists ─────────────────────────────────────────────────

  get filteredCategories(): Category[] {
    if (!this.categorySearch.trim()) return this.categories;
    const t = this.categorySearch.toLowerCase();
    return this.categories.filter(c => c.value.toLowerCase().includes(t));
  }

  get filteredSuppliers(): Supplier[] {
    if (!this.supplierSearch.trim()) return this.suppliers;
    const t = this.supplierSearch.toLowerCase();
    return this.suppliers.filter(s => s.name.toLowerCase().includes(t));
  }

  get filteredProductsForVariant(): { id: number; name: string }[] {
    const all = this.allProductsSimple.map(p => ({ id: p.productId, name: p.name }));
    if (!this.productSearch.trim()) return all;
    const t = this.productSearch.toLowerCase();
    return all.filter(p => p.name.toLowerCase().includes(t));
  }

  // ─── Category Combobox ────────────────────────────────────────────────────────

  onCategoryFocus(event: FocusEvent) {
    if (this.selectedCategoryLabel) {
      this.categorySearch = this.selectedCategoryLabel;
      setTimeout(() => (event.target as HTMLInputElement).select(), 0);
    }
    this.showCategoryDropdown = true;
  }

  onCategoryInput() {
    if (this.categorySearch && this.selectedCategoryLabel) {
      this.productForm.patchValue({ categoryId: '' });
      this.selectedCategoryLabel = '';
    }
    this.showCategoryDropdown = true;
  }

  onCategoryBlur() {
    setTimeout(() => {
      this.showCategoryDropdown = false;
      this.categorySearch = this.selectedCategoryLabel;
    }, 150);
  }

  selectCategory(cat: Category, event: MouseEvent) {
    event.preventDefault();
    this.productForm.patchValue({ categoryId: cat.lookupId });
    this.selectedCategoryLabel = cat.value;
    this.categorySearch = cat.value;
    this.showCategoryDropdown = false;
  }

  clearCategory(event: MouseEvent) {
    event.preventDefault();
    this.productForm.patchValue({ categoryId: '' });
    this.selectedCategoryLabel = '';
    this.categorySearch = '';
    this.showCategoryDropdown = false;
  }

  // ─── Supplier Combobox ────────────────────────────────────────────────────────

  onSupplierFocus(event: FocusEvent) {
    if (this.selectedSupplierLabel) {
      this.supplierSearch = this.selectedSupplierLabel;
      setTimeout(() => (event.target as HTMLInputElement).select(), 0);
    }
    this.showSupplierDropdown = true;
  }

  onSupplierInput() {
    if (this.supplierSearch && this.selectedSupplierLabel) {
      this.productForm.patchValue({ supplierId: '' });
      this.selectedSupplierLabel = '';
    }
    this.showSupplierDropdown = true;
  }

  onSupplierBlur() {
    setTimeout(() => {
      this.showSupplierDropdown = false;
      this.supplierSearch = this.selectedSupplierLabel;
    }, 150);
  }

  selectSupplier(sup: Supplier, event: MouseEvent) {
    event.preventDefault();
    this.productForm.patchValue({ supplierId: sup.supplierId });
    this.selectedSupplierLabel = sup.name;
    this.supplierSearch = sup.name;
    this.showSupplierDropdown = false;
  }

  clearSupplier(event: MouseEvent) {
    event.preventDefault();
    this.productForm.patchValue({ supplierId: '' });
    this.selectedSupplierLabel = '';
    this.supplierSearch = '';
    this.showSupplierDropdown = false;
  }

  // ─── Product Combobox (Variant Form) ─────────────────────────────────────────

  onProductFocus(event: FocusEvent) {
    if (this.selectedProductLabel) {
      this.productSearch = this.selectedProductLabel;
      setTimeout(() => (event.target as HTMLInputElement).select(), 0);
    }
    this.showProductDropdown = true;
  }

  onProductInput() {
    if (this.productSearch && this.selectedProductLabel) {
      this.variantForm.patchValue({ productId: '' });
      this.selectedProductLabel = '';
    }
    this.showProductDropdown = true;
  }

  onProductBlur() {
    setTimeout(() => {
      this.showProductDropdown = false;
      this.productSearch = this.selectedProductLabel;
    }, 150);
  }

  selectProduct(p: { id: number; name: string }, event: MouseEvent) {
    event.preventDefault();
    this.variantForm.patchValue({ productId: p.id });
    this.selectedProductLabel = p.name;
    this.productSearch = p.name;
    this.showProductDropdown = false;
  }

  clearProduct(event: MouseEvent) {
    event.preventDefault();
    this.variantForm.patchValue({ productId: '' });
    this.selectedProductLabel = '';
    this.productSearch = '';
    this.showProductDropdown = false;
  }

  // ─── Reset helpers ────────────────────────────────────────────────────────────

  private resetProductDropdownState() {
    this.selectedCategoryLabel = '';
    this.selectedSupplierLabel = '';
    this.categorySearch = '';
    this.supplierSearch = '';
    this.showCategoryDropdown = false;
    this.showSupplierDropdown = false;
  }

  private resetVariantDropdownState() {
    this.selectedProductLabel = '';
    this.productSearch = '';
    this.showProductDropdown = false;
  }

  // ─── Product Modal ────────────────────────────────────────────────────────────

  addProduct() {
    this.editProductForm = false;
    this.selectedProductId = null;
    this.productForm.reset({ isActive: true });
    this.resetProductDropdownState();
    this.showAddProductForm = true;
    this.animateModalOpen();
  }

  closeAddProductForm() {
    this.animateModalClose(() => {
      this.showAddProductForm = false;
      this.editProductForm = false;
    });
  }

  onSubmit() {
    if (this.productForm.invalid) { this.productForm.markAllAsTouched(); return; }

    this.isLoading = true;
    const formValue = this.productForm.value;

    const payload: CreateProductPayload = {
      name: formValue.name,
      description: formValue.description || '',
      categoryId: Number(formValue.categoryId),
      supplierId: Number(formValue.supplierId),
    };

    if (this.editProductForm && this.selectedProductId) {
      this.productService.updateProduct(this.selectedProductId, payload).subscribe({
        next: (res) => {
          this.isLoading = false;
          if (res.success) {
            this.showSuccess('Product update ho gaya!');
            this.closeAddProductForm();
            // ✅ FIX: Refresh BOTH lists so variant dropdown stays in sync
            this.loadProducts();
            this.loadProductsSimple();
          } else {
            this.errorMessage = res.message || 'Update nahi hua';
          }
        },
        error: (err) => { this.isLoading = false; this.errorMessage = 'Update fail ho gaya!'; console.error(err); },
      });
    } else {
      this.productService.createProduct(payload).subscribe({
        next: (res) => {
          this.isLoading = false;
          if (res.success) {
            this.showSuccess('Naya product add ho gaya!');
            this.closeAddProductForm();
            // ✅ FIX: Refresh BOTH lists so new product immediately appears in variant dropdown
            this.loadProducts();
            this.loadProductsSimple();
          } else {
            this.errorMessage = res.message || 'Product save nahi hua';
          }
        },
        error: (err) => { this.isLoading = false; this.errorMessage = 'Product save fail!'; console.error(err); },
      });
    }
          if(this.editProductForm){
    return;
          }
          else{
                  this.showAddVariantForm=true;
          }
  }

  // ─── Variant Modal ────────────────────────────────────────────────────────────

  addVariant() {
    this.editVariantForm = false;
    this.selectedVariantId = null;
    this.variantForm.reset({ pricePerUnit: 0, pricePerLength: 0, lengthInFeet: 0, quantityInStock: 0, reorderLevel: 0 });
    this.resetVariantDropdownState();
    this.showAddVariantForm = true;
    this.animateModalOpen();
  }

  closeAddVariantForm() {
    this.animateModalClose(() => {
      this.showAddVariantForm = false;
      this.editVariantForm = false;
    });
  }

  onSubmitVariant() {
    if (this.variantForm.invalid) { this.variantForm.markAllAsTouched(); return; }

    this.isLoading = true;
    const fv = this.variantForm.value;

    const payload: CreateVariantPayload = {
      size: fv.size,
      classType: fv.classType,
      unitOfMeasure: fv.unitOfMeasure,
      quantityInStock: Number(fv.quantityInStock),
      pricePerUnit: Number(fv.pricePerUnit),
      pricePerLength: Number(fv.pricePerLength) || 0,
      lengthInFeet: Number(fv.lengthInFeet) || 0,
      reorderLevel: Number(fv.reorderLevel),
      color: fv.color || '',
      location: fv.location || '',
      notes: fv.notes || '',
    };

    if (this.editVariantForm && this.selectedVariantId) {
      const updatePayload: UpdateVariantPayload = { variantId: this.selectedVariantId, ...payload, isActive: true };
      this.productService.updateVariant(this.selectedVariantId, updatePayload).subscribe({
        next: (res) => {
          this.isLoading = false;
          if (res.success) { this.showSuccess('Variant update ho gaya!'); this.closeAddVariantForm(); this.loadProducts(); }
          else this.errorMessage = res.message || 'Update nahi hua';
        },
        error: (err) => { this.isLoading = false; this.errorMessage = 'Variant update fail!'; console.error(err); },
      });
    } else {
      this.productService.createVariant(Number(fv.productId), payload).subscribe({
        next: (res) => {
          this.isLoading = false;
          if (res.success) { this.showSuccess('Naya variant add ho gaya!'); this.closeAddVariantForm(); this.loadProducts(); }
          else this.errorMessage = res.message || 'Variant save nahi hua';
        },
        error: (err) => { this.isLoading = false; this.errorMessage = 'Variant save fail!'; console.error(err); },
      });
    }
  }

  // ─── Edit Handlers ────────────────────────────────────────────────────────────

  editProduct(item: InventoryItem) {
    this.editProductForm = true;
    this.selectedProductId = item.productId;

    const cat = this.categories.find(c => c.lookupId === item._rawProduct.categoryId);
    const sup = this.suppliers.find(s => s.supplierId === item._rawProduct.supplierId);
    this.selectedCategoryLabel = cat?.value || '';
    this.selectedSupplierLabel = sup?.name || '';
    this.categorySearch = this.selectedCategoryLabel;
    this.supplierSearch = this.selectedSupplierLabel;

    this.productForm.patchValue({
      name: item._rawProduct.name,
      description: item._rawProduct.description || '',
      categoryId: item._rawProduct.categoryId,
      supplierId: item._rawProduct.supplierId,
      isActive: item._rawProduct.isActive,
      notes: item._rawProduct.notes || '',
    });

    this.showAddProductForm = true;
    this.animateModalOpen();
  }

  editVariant(item: InventoryItem) {
    if (!item._rawVariant) {
      this.addVariant();
      this.variantForm.patchValue({ productId: item.productId });
      this.selectedProductLabel = item.product;
      return;
    }

    this.editVariantForm = true;
    this.selectedVariantId = item._rawVariant.variantId;
    this.selectedProductLabel = item.product;
    this.productSearch = item.product;

    this.variantForm.patchValue({
      productId: item.productId,
      size: item._rawVariant.size,
      color: item._rawVariant.color || '',
      classType: item._rawVariant.classType,
      unitOfMeasure: item._rawVariant.unitOfMeasure,
      pricePerUnit: item._rawVariant.pricePerUnit,
      pricePerLength: item._rawVariant.pricePerLength || 0,
      lengthInFeet: item._rawVariant.lengthInFeet || 0,
      quantityInStock: item._rawVariant.quantityInStock,
      reorderLevel: item._rawVariant.reorderLevel,
      location: item._rawVariant.location || '',
      notes: item._rawVariant.notes || '',
    });

    this.showAddVariantForm = true;
    this.animateModalOpen();
  }

  // ─── Animations ───────────────────────────────────────────────────────────────

  animateModalOpen() {
    setTimeout(() => {
      if (this.modalContent?.nativeElement) {
        gsap.fromTo(this.modalContent.nativeElement,
          { opacity: 0, scale: 0.8, y: 60 },
          { opacity: 1, scale: 1, y: 0, duration: 0.45, ease: 'back.out(1.4)' },
        );
      }
    }, 10);
  }

  animateModalClose(callback: () => void) {
    if (this.modalContent?.nativeElement) {
      gsap.to(this.modalContent.nativeElement, {
        opacity: 0, scale: 0.8, y: 60, duration: 0.3, ease: 'power2.in', onComplete: callback,
      });
    } else {
      callback();
    }
  }

  showSuccess(msg: string) {
    this.successMessage = msg;
    setTimeout(() => (this.successMessage = ''), 3000);
  }

  // ─── Form Getters ─────────────────────────────────────────────────────────────

  get name()          { return this.productForm.get('name'); }
  get category()      { return this.productForm.get('categoryId'); }
  get supplier()      { return this.productForm.get('supplierId'); }
  get productId()     { return this.variantForm.get('productId'); }
  get size()          { return this.variantForm.get('size'); }
  get classType()     { return this.variantForm.get('classType'); }
  get unitOfMeasure() { return this.variantForm.get('unitOfMeasure'); }
  get pricePerUnit()  { return this.variantForm.get('pricePerUnit'); }
  get stockQuantity() { return this.variantForm.get('quantityInStock'); }
  get reorderLevel()  { return this.variantForm.get('reorderLevel'); }

  get products() {
    return this.allProductsSimple.map(p => ({ id: p.productId, name: p.name }));
  }
}