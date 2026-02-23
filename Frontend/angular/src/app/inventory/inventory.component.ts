import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { gsap } from 'gsap';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { InventoryService, Product, ProductVariant, Category, ProductWithStock, ApiResponse } from '../services/inventory.service';



interface InventoryItem {
  description: string;
  supplier: string;
  supplierId?: number;
  active: boolean;
  product: string;
  productId?: number;
  variantId?: number;
  size: string;
  unit: string;
  class: string;
  pricePerUnit: number;
  pricePerLength: number;
  lengthFt: number;
  stock: number;
  reorder: number;
  minQty: number;
}

interface Supplier {
  supplierId: number;
  name: string;
}

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './inventory.component.html',
  styleUrl: './inventory.component.css'
})
export class InventoryComponent implements OnInit {
  
  // Real data from API
  items: InventoryItem[] = [];
  allProducts: Product[] = [];
  categories: Category[] = [];
  suppliers: Supplier[] = [];
  products: { id: number; name: string; }[] = [];
  
  // KPIs
  totalItems = 0;
  lowStock = 0;
  outOfStock = 0;

  // Search term
  searchTerm = '';

  // Filter mode
  filter = 'all';

  // Loading states
  isLoading = false;
  errorMessage = '';

  // Modal states
  @ViewChild('modalContent') modalContent!: ElementRef;
  showAddProductForm = false;
  showAddVariantForm = false;
  
  // Form states
  editProductForm = false;
  editVariantForm = false;
  currentEditId: number | null = null;

  // Forms
  productForm!: FormGroup;
  variantForm!: FormGroup;

  // Units for dropdown
  units = ['Piece', 'Meter', 'Foot', 'Kg', 'Liter', 'Pack', 'BOTTLE', 'BOX', 'LENGTH'];

  constructor(
    private fb: FormBuilder,
    private inventoryService: InventoryService
  ) {}

  ngOnInit() {
    this.initForms();
    this.loadInventoryData();
    this.loadCategories();
    this.loadSuppliers();
    this.loadProducts();
  }

  initForms() {
    this.productForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      categoryId: ['', Validators.required],
      description: [''],
      supplierId: ['', Validators.required],
      hasVariant: [false],
      isActive: [true],
      notes: ['']
    });

    this.variantForm = this.fb.group({
      productId: ['', Validators.required],
      size: ['', Validators.required],
      classType: ['', Validators.required],
      unitOfMeasure: ['', Validators.required],
      pricePerUnit: [0, [Validators.required, Validators.min(0.01)]],
      pricePerLength: [0, [Validators.min(0)]],
      lengthValue: [0, [Validators.min(0)]],
      quantityInStock: [0, [Validators.required, Validators.min(0)]],
      reorderLevel: [0, [Validators.required, Validators.min(0)]],
      location: [''],
      notes: ['']
    });
  }

  loadInventoryData() {
    this.isLoading = true;
    this.inventoryService.getAllProducts().subscribe({
      next: (response: ApiResponse<Product[]>) => {
        this.isLoading = false;
        if (response.success && response.data) {
          this.allProducts = response.data;
          this.transformProductsToItems(response.data);
          this.calculateKPIs();
        }
      },
      error: (error: any) => {
        this.isLoading = false;
        this.errorMessage = 'Error loading inventory data';
        console.error('Error:', error);
      }
    });
  }

  loadCategories() {
    this.inventoryService.getCategories().subscribe({
      next: (response: ApiResponse<Category[]>) => {
        if (response.success && response.data) {
          this.categories = response.data;
        }
      },
      error: (error: any) => console.error('Error loading categories:', error)
    });
  }

  loadSuppliers() {
    // Placeholder suppliers - you can replace with actual API call
    this.suppliers = [
      { supplierId: 1, name: 'Popular Pipes Ltd.' },
      { supplierId: 2, name: 'Diamond Fittings Co.' },
      { supplierId: 3, name: 'Elite Electricals' },
      { supplierId: 4, name: 'General Supplies' }
    ];
  }

  loadProducts() {
    this.inventoryService.getAllProducts().subscribe({
      next: (response: ApiResponse<Product[]>) => {
        if (response.success && response.data) {
          this.products = response.data.map((p: Product) => ({
            id: p.productId,
            name: p.name
          }));
        }
      },
      error: (error: any) => console.error('Error loading products:', error)
    });
  }

  transformProductsToItems(products: Product[]) {
    this.items = [];
    products.forEach((product: Product) => {
      if (product.variants && product.variants.length > 0) {
        product.variants.forEach((variant: ProductVariant) => {
          this.items.push({
            description: product.description || product.name,
            supplier: product.supplierName || 'Unknown',
            supplierId: product.supplierId || undefined,
            active: product.isActive,
            product: product.name,
            productId: product.productId,
            variantId: variant.variantId,
            size: variant.size || 'Standard',
            unit: variant.unitOfMeasure,
            class: variant.classType || 'N/A',
            pricePerUnit: variant.pricePerUnit,
            pricePerLength: variant.pricePerLength || 0,
            lengthFt: 0,
            stock: variant.quantityInStock,
            reorder: variant.reorderLevel,
            minQty: 1
          });
        });
      }
    });
  }

  calculateKPIs() {
    this.totalItems = this.items.length;
    this.lowStock = this.items.filter(i => i.stock > 0 && i.stock <= i.reorder).length;
    this.outOfStock = this.items.filter(i => i.stock === 0).length;
  }

  get filteredItems(): InventoryItem[] {
    let filtered = this.items;

    if (this.searchTerm) {
      filtered = filtered.filter((item: InventoryItem) => 
        item.description.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        item.product.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        item.supplier.toLowerCase().includes(this.searchTerm.toLowerCase())
      );
    }

    if (this.filter === 'low') {
      filtered = filtered.filter((item: InventoryItem) => item.stock > 0 && item.stock <= item.reorder);
    } else if (this.filter === 'out') {
      filtered = filtered.filter((item: InventoryItem) => item.stock === 0);
    }

    return filtered;
  }

  // Button actions
  addProduct() { 
    this.editProductForm = false;
    this.currentEditId = null;
    this.productForm.reset({ isActive: true, hasVariant: false });
    this.openAddProductForm();
  }

  addVariant() { 
    this.editVariantForm = false;
    this.currentEditId = null;
    this.variantForm.reset({
      pricePerUnit: 0,
      pricePerLength: 0,
      lengthValue: 0,
      quantityInStock: 0,
      reorderLevel: 0
    });
    this.openAddVariantForm();
  }

  showLowStock() { 
    this.filter = 'low';
  }

  showOutStock() { 
    this.filter = 'out';
  }

  showAll() { 
    this.filter = 'all';
  }

  editProduct(item: InventoryItem) {
    this.editProductForm = true;
    this.currentEditId = item.productId || null;
    
    const product = this.allProducts.find(p => p.productId === item.productId);
    if (product) {
      this.productForm.patchValue({
        name: product.name,
        categoryId: product.categoryId,
        description: product.description,
        supplierId: product.supplierId,
        hasVariant: (product.variants?.length || 0) > 1,
        isActive: product.isActive,
        notes: product.notes
      });
    }
    
    this.openAddProductForm();
  }

  editVariant(item: InventoryItem) {
    this.editVariantForm = true;
    this.currentEditId = item.variantId || null;
    
    this.variantForm.patchValue({
      productId: item.productId,
      size: item.size,
      classType: item.class,
      unitOfMeasure: item.unit,
      pricePerUnit: item.pricePerUnit,
      pricePerLength: item.pricePerLength,
      lengthValue: item.lengthFt,
      quantityInStock: item.stock,
      reorderLevel: item.reorder,
      notes: ''
    });
    
    this.openAddVariantForm();
  }

  openAddProductForm() {
    this.showAddProductForm = true;
    setTimeout(() => {
      gsap.fromTo(
        this.modalContent.nativeElement,
        { opacity: 0, scale: 0.8, y: 60 },
        { opacity: 1, scale: 1, y: 0, duration: 0.45, ease: "back.out(1.4)" }
      );
    }, 10);
  }

  closeAddProductForm() {
    gsap.to(this.modalContent.nativeElement, {
      opacity: 0,
      scale: 0.8,
      y: 60,
      duration: 0.3,
      ease: "power2.in",
      onComplete: () => {
        this.showAddProductForm = false;
      }
    });
  }

  openAddVariantForm() {
    this.showAddVariantForm = true;
    setTimeout(() => {
      gsap.fromTo(
        this.modalContent.nativeElement,
        { opacity: 0, scale: 0.82, y: 50 },
        { opacity: 1, scale: 1, y: 0, duration: 0.45, ease: "back.out(1.3)" }
      );
    }, 0);
  }

  closeAddVariantForm() {
    gsap.to(this.modalContent.nativeElement, {
      opacity: 0,
      scale: 0.82,
      y: 50,
      duration: 0.3,
      ease: "power2.in",
      onComplete: () => {
        this.showAddVariantForm = false;
      }
    });
  }

 onSubmit() {
  if (this.productForm.invalid) {
    this.productForm.markAllAsTouched();
    return;
  }

  this.isLoading = true;
  const formValue = this.productForm.value;
  
  // ADD THIS LOG
  console.log('Submitting product data:', JSON.stringify(formValue, null, 2));

  if (this.editProductForm && this.currentEditId) {
    // ... rest of your code {
      this.inventoryService.updateProduct(this.currentEditId, formValue).subscribe({
        next: (response: ApiResponse<boolean>) => {
          this.isLoading = false;
          if (response.success) {
            this.loadInventoryData();
            this.closeAddProductForm();
          }
        },
        error: (error: any) => {
          this.isLoading = false;
          this.errorMessage = 'Error updating product';
          console.error('Error:', error);
        }
      });
    } else {
      // Create new product - MUST include at least one variant
const createDto = {
  name: formValue.name,
  description: formValue.description,
  categoryId: Number(formValue.categoryId),  // Convert to number
  supplierId: Number(formValue.supplierId),  // Convert to number
  notes: formValue.notes,
  variants: [{
    size: "Standard",
    unitOfMeasure: "Piece",
    quantityInStock: 0,
    pricePerUnit: 100,
    reorderLevel: 0
  }]
};

console.log('Sending to API:', JSON.stringify(createDto, null, 2));

this.inventoryService.createProduct(createDto).subscribe({
          next: (response: ApiResponse<Product>) => {
          this.isLoading = false;
          if (response.success) {
            this.loadInventoryData();
            this.closeAddProductForm();
          }
        },
        error: (error: any) => {
          this.isLoading = false;
          this.errorMessage = 'Error creating product';
          console.error('Error:', error);
        }
      });
    }
  }

  onSubmitVariant() {
    if (this.variantForm.invalid) {
      this.variantForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    const formValue = this.variantForm.value;

    if (this.editVariantForm && this.currentEditId) {
      const updateDto = {
        variantId: this.currentEditId,
        size: formValue.size,
        classType: formValue.classType,
        unitOfMeasure: formValue.unitOfMeasure,
        quantityInStock: formValue.quantityInStock,
        pricePerUnit: formValue.pricePerUnit,
        pricePerLength: formValue.pricePerLength,
        reorderLevel: formValue.reorderLevel,
        location: formValue.location,
        isActive: true,
        notes: formValue.notes
      };

      this.inventoryService.updateVariant(this.currentEditId, updateDto).subscribe({
        next: (response: ApiResponse<boolean>) => {
          this.isLoading = false;
          if (response.success) {
            this.loadInventoryData();
            this.closeAddVariantForm();
          }
        },
        error: (error: any) => {
          this.isLoading = false;
          this.errorMessage = 'Error updating variant';
          console.error('Error:', error);
        }
      });
    } else {
      const createDto = {
        size: formValue.size,
        classType: formValue.classType,
        unitOfMeasure: formValue.unitOfMeasure,
        quantityInStock: formValue.quantityInStock,
        pricePerUnit: formValue.pricePerUnit,
        pricePerLength: formValue.pricePerLength,
        reorderLevel: formValue.reorderLevel,
        location: formValue.location,
        notes: formValue.notes
      };

      this.inventoryService.addVariant(formValue.productId, createDto).subscribe({
        next: (response: ApiResponse<ProductVariant>) => {
          this.isLoading = false;
          if (response.success) {
            this.loadInventoryData();
            this.closeAddVariantForm();
          }
        },
        error: (error: any) => {
          this.isLoading = false;
          this.errorMessage = 'Error creating variant';
          console.error('Error:', error);
        }
      });
    }
  }

  // Form getters
  get name() { return this.productForm.get('name'); }
  get category() { return this.productForm.get('categoryId'); }
  get supplier() { return this.productForm.get('supplierId'); }
  get productId() { return this.variantForm.get('productId'); }
  get size() { return this.variantForm.get('size'); }
  get classType() { return this.variantForm.get('classType'); }
  get unitOfMeasure() { return this.variantForm.get('unitOfMeasure'); }
  get pricePerUnit() { return this.variantForm.get('pricePerUnit'); }
  get stockQuantity() { return this.variantForm.get('quantityInStock'); }
  get reorderLevel() { return this.variantForm.get('reorderLevel'); }
}