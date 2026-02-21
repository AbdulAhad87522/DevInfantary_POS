import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { gsap } from 'gsap';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';


interface InventoryItem {
  description: string;
  supplier: string;
  active: boolean;
  product: string;
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

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule,FormsModule,ReactiveFormsModule],
  templateUrl: './inventory.component.html',
  styleUrl: './inventory.component.css'
})
export class InventoryComponent {

// Dummy data (1 item as requested; in real app, fetch from DB/service)
  items: InventoryItem[] = [
    {
      description: 'High-quality PVC Pipe',
      supplier: 'ABC Suppliers',
      active: true,
      product: 'PVC Pipe',
      size: '2 inch',
      unit: 'Piece',
      class: 'A',
      pricePerUnit: 50,
      pricePerLength: 10,
      lengthFt: 20,
      stock: 150,
      reorder: 50,
      minQty: 30
    }
  ];

  // KPIs (derived or from DB; hardcoded for demo)
  totalItems = 1;
  lowStock = 0;
  outOfStock = 0;

  // Search term
  searchTerm = '';

  // Filter mode (for buttons)
  filter = 'all'; // 'all', 'low', 'out'

  // Filtered items (computed in real app)
  get filteredItems() {
    // Placeholder: In real app, filter based on searchTerm and this.filter
    return this.items.filter(item => 
      item.description.toLowerCase().includes(this.searchTerm.toLowerCase())
    );
  }

  // Button actions (placeholders)
  addProduct() { console.log('Add Product');this.showAddProductForm=true;}
  addVariant() { console.log('Add Variant');this.showAddVariantForm=true; }
  showLowStock() { this.filter = 'low'; /* filter logic */ }
  showOutStock() { this.filter = 'out'; /* filter logic */ }
  showAll() { this.filter = 'all'; /* reset */ }

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

  ngOnInit() {
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

}
