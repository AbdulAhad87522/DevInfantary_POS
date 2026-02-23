import { Component, OnInit, ViewChild, ElementRef, inject, Inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { gsap } from 'gsap';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ProductService } from '../Services/product.service';
import { HttpClient } from '@angular/common/http';


interface InventoryItem {
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
}

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule,FormsModule,ReactiveFormsModule],
  templateUrl: './inventory.component.html',
  styleUrl: './inventory.component.css'
})
export class InventoryComponent implements OnInit {
 productService = inject(ProductService);
  http = inject(HttpClient);

  items: InventoryItem[] = []; // Start with empty array, not dummy data
  
  // KPIs
  totalItems = 0;
  lowStock = 0;
  outOfStock = 0;

  // Loading states
  isLoading = false;
  errorMessage = '';

  // Search and filter
  searchTerm = '';
  filter = 'all'; // 'all', 'low', 'out'

  // ... rest of your existing code (forms, ViewChild, etc.)

  initForms() {
    this.productForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      category: ['', Validators.required],
      description: [''],
      supplier: ['', Validators.required],
      hasVariant: [false],
      isActive: [true]
    });

    this.variantForm = this.fb.group({
      productId: ['', Validators.required],
      size: ['', Validators.required],
      classType: ['', Validators.required],
      unitOfMeasure: ['', Validators.required],
      pricePerUnit: [0, [Validators.required, Validators.min(0.01)]],
      pricePerLength: [0, [Validators.min(0)]],
      lengthValue: [0, [Validators.min(0)]],
      stockQuantity: [0, [Validators.required, Validators.min(0)]],
      reorderLevel: [0, [Validators.required, Validators.min(0)]],
      minOrderQuantity: [0, [Validators.required, Validators.min(1)]]
    });
  }

  showAll() {
    // The complete function I provided above
    this.isLoading = true;
    this.errorMessage = '';
    
    this.productService.getAllProducts().subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success && response.data) {
          this.items = response.data.map((product: any) => {
            const firstVariant = product.variants && product.variants.length > 0 
              ? product.variants[0] 
              : null;
            
            return {
              description: product.description || product.name,
              supplier: product.supplierName || 'No Supplier',
              active: product.isActive,
              product: product.name,
              size: firstVariant?.size || 'N/A',
              unit: firstVariant?.unitOfMeasure || 'Piece',
              class_type: firstVariant?.classType || 'Standard',
              price_per_unit: firstVariant?.pricePerUnit || 0,
              price_per_length: firstVariant?.pricePerLength || 0,
              lengthFt: firstVariant?.pricePerLength ? 1 : 0,
              stock: firstVariant?.quantityInStock || 0,
              reorder: firstVariant?.reorderLevel || 10,
              minQty: firstVariant?.reorderLevel || 5
            };
          });
          
          this.updateKPIs();
          console.log('Products loaded:', this.items);
        } else {
          this.errorMessage = response.message || 'Failed to load products';
        }
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = 'Error connecting to server. Please try again.';
        console.error('Error loading products:', error);
      }
    });
  }

  updateKPIs() {
    this.totalItems = this.items.length;
    this.lowStock = this.items.filter(item => item.stock > 0 && item.stock <= item.reorder).length;
    this.outOfStock = this.items.filter(item => item.stock === 0).length;
  }

  get filteredItems() {
    let filtered = this.items;
    
    if (this.searchTerm) {
      filtered = filtered.filter(item => 
        item.product.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        item.description.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        item.supplier.toLowerCase().includes(this.searchTerm.toLowerCase())
      );
    }
    
    if (this.filter === 'low') {
      filtered = filtered.filter(item => item.stock > 0 && item.stock <= item.reorder);
    } else if (this.filter === 'out') {
      filtered = filtered.filter(item => item.stock === 0);
    }
    
    return filtered;
  }

  showLowStock() { 
    this.filter = 'low'; 
  }
  
  showOutStock() { 
    this.filter = 'out'; 
  }
  // Edit actions
  editProduct(item: InventoryItem) { console.log('Edit Product:', item);this.showAddProductForm=true; this.editProductForm=true;}
  editVariant(item: InventoryItem) { console.log('Edit Variant:', item); this.showAddVariantForm=true; this.editVariantForm=true; }

//---------------------------
  // Cpde for ADD PRODUCT
  //-------------------------
  @ViewChild('modalContent') modalContent!: ElementRef;

  showAddProductForm = false;
  productForm!: FormGroup;

  // Dummy categories & suppliers (replace with real data from service)
  categories = ['Pipes', 'Fittings', 'Adhesives', 'Electrical', 'Tools'];
  suppliers = ['General Supplies', 'ABC Traders', 'PVC Masters', 'Hardware Hub'];

  constructor(private fb: FormBuilder) {}

  ngOnInit() {  this.showAll(); // Load products when component initializes
    this.initForms();
  }

  openAddProductForm() {
    this.showAddProductForm = true;
    this.productForm.reset({ isActive: true, hasVariant: false });

    // GSAP open animation
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

  onSubmit() {
    if (this.productForm.invalid) {
      this.productForm.markAllAsTouched();
      return;
    }

    console.log('New product submitted:', this.productForm.value);

    // In real app:
    // this.productService.createProduct(this.productForm.value).subscribe(...)

    // Reset & close
    this.closeAddProductForm();
  }

  get name() { return this.productForm.get('name'); }
  get category() { return this.productForm.get('category'); }
  get supplier() { return this.productForm.get('supplier'); }


  //------------------------=========================
  // Code for ADD VARIANT
  //------------------------============================

  @ViewChild('modalContent') modalContents!: ElementRef;

  showAddVariantForm = false;
  variantForm!: FormGroup;

  // Dummy data (in real app → from service / selected product)
  products = [
    { id: 1, name: 'UPVC Pipe' },
    { id: 2, name: 'PVC Elbow' },
    { id: 3, name: 'Adhesive 500ml' },
    { id: 4, name: 'Electrical Wire 2.5mm' }
  ];

  units = ['Piece', 'Meter', 'Foot', 'Kg', 'Liter', 'Pack'];

  openAddVariantForm() {
    this.showAddVariantForm = true;
    this.variantForm.reset({
      pricePerUnit: 0,
      pricePerLength: 0,
      lengthValue: 0,
      stockQuantity: 0,
      reorderLevel: 0,
      minOrderQuantity: 1
    });

    // GSAP open animation
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

  onSubmitVariant() {
    if (this.variantForm.invalid) {
      this.variantForm.markAllAsTouched();
      return;
    }

    console.log('New variant submitted:', this.variantForm.value);

    // In real app:
    // this.variantService.createVariant(this.variantForm.value).subscribe(...)

    this.closeAddVariantForm();
  }

  // Helpers for easier template access
  get productId() { return this.variantForm.get('productId'); }
  get size() { return this.variantForm.get('size'); }
  get classType() { return this.variantForm.get('classType'); }
  get unitOfMeasure() { return this.variantForm.get('unitOfMeasure'); }
  get pricePerUnit() { return this.variantForm.get('pricePerUnit'); }
  get stockQuantity() { return this.variantForm.get('stockQuantity'); }
  get reorderLevel() { return this.variantForm.get('reorderLevel'); }
  get minOrderQuantity() { return this.variantForm.get('minOrderQuantity'); }

//   ===================================================
//   EDIT PRODUCT
// ===================================================

editProductForm:boolean=false;
editVariantForm:boolean=false;

// Add these methods to your InventoryComponent class
addProduct() {
  console.log('Add Product');
  this.openAddProductForm(); // Call your existing form opening method
}

addVariant() {
  console.log('Add Variant');
  this.openAddVariantForm(); // Call your existing form opening method
}
}
