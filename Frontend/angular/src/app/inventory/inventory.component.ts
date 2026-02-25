import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
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
} from '../Services/product.service';

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
export class InventoryComponent implements OnInit {
  @ViewChild('modalContent') modalContent!: ElementRef;

  items: InventoryItem[] = [];
  allProducts: Product[] = [];

  totalItems = 0;
  lowStock = 0;
  outOfStock = 0;
  inventoryValue = 0;

  isLoading = false;
  errorMessage = '';
  successMessage = '';

  searchTerm = '';
  filter = 'all';

  showAddProductForm = false;
  showAddVariantForm = false;
  editProductForm = false;
  editVariantForm = false;

  selectedProductId: number | null = null;
  selectedVariantId: number | null = null;

  categories: Category[] = [];
  suppliers: Supplier[] = []; // ✅ Supplier type use kar rahe hain
  units = ['Piece', 'Meter', 'Foot', 'Kg', 'Liter', 'Pack', 'Box', 'Roll'];

  productForm!: FormGroup;
  variantForm!: FormGroup;

  constructor(
    private fb: FormBuilder,
    private productService: ProductService,
  ) {}

  ngOnInit() {
    this.initForms();
    this.loadAll();
  }

  initForms() {
    this.productForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      description: [''],
      categoryId: ['', Validators.required],
      supplierId: ['', Validators.required],
      hasVariant: [false],
      isActive: [true],
      notes: [''],
    });

    this.variantForm = this.fb.group({
      productId: ['', Validators.required],
      size: ['', Validators.required],
      color: [''],
      classType: ['', Validators.required],
      unitOfMeasure: ['', Validators.required],
      pricePerUnit: [0, [Validators.required, Validators.min(0.01)]],
      pricePerLength: [0],
      lengthValue: [0],
      quantityInStock: [0, [Validators.required, Validators.min(0)]],
      reorderLevel: [0, [Validators.required, Validators.min(0)]],
      location: [''],
      notes: [''],
    });
  }

  loadAll() {
    this.loadProducts();
    this.loadCategoriesAndSuppliers();
    this.loadInventoryValue();
  }

  loadProducts() {
    this.isLoading = true;
    this.errorMessage = '';

    this.productService.getAllProducts().subscribe({
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
            lengthFt: variant.pricePerLength ? 1 : 0,
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
          size: '—',
          unit: '—',
          class_type: '—',
          price_per_unit: 0,
          price_per_length: 0,
          lengthFt: 0,
          stock: 0,
          reorder: 0,
          minQty: 0,
          _rawProduct: product,
          _rawVariant: null,
        });
      }
    });
  }

  // ✅ Yeh function fix hua - suppliers ab API se aate hain
  loadCategoriesAndSuppliers() {
    this.productService.getAllCategories().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.categories = res.data;
        }
      },
      error: (err) => console.error('Categories load failed:', err),
    });

    this.productService.getAllSuppliers().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.suppliers = res.data;
        }
      },
      error: (err) => console.error('Suppliers load failed:', err),
    });
  }

  loadInventoryValue() {
    this.productService.getInventoryValue().subscribe({
      next: (res) => {
        if (res.success) {
          this.inventoryValue = res.data;
        }
      },
      error: (err) => console.error('Inventory value load failed:', err),
    });
  }

  updateKPIs() {
    this.totalItems = this.items.length;
    this.lowStock = this.items.filter(
      (item) => item.stock > 0 && item.stock <= item.reorder,
    ).length;
    this.outOfStock = this.items.filter((item) => item.stock === 0).length;
  }

  get filteredItems(): InventoryItem[] {
    let filtered = this.items;

    if (this.searchTerm.trim()) {
      const term = this.searchTerm.toLowerCase();
      filtered = filtered.filter(
        (item) =>
          item.product.toLowerCase().includes(term) ||
          item.description.toLowerCase().includes(term) ||
          item.supplier.toLowerCase().includes(term) ||
          item.size.toLowerCase().includes(term),
      );
    }

    if (this.filter === 'low') {
      filtered = filtered.filter(
        (item) => item.stock > 0 && item.stock <= item.reorder,
      );
    } else if (this.filter === 'out') {
      filtered = filtered.filter((item) => item.stock === 0);
    }

    return filtered;
  }

  showAll() {
    this.filter = 'all';
    this.loadProducts();
  }

  showLowStock() {
    this.filter = 'low';
  }

  showOutStock() {
    this.filter = 'out';
  }

  addProduct() {
    this.editProductForm = false;
    this.selectedProductId = null;
    this.productForm.reset({ isActive: true, hasVariant: false });
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
    if (this.productForm.invalid) {
      this.productForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    const formValue = this.productForm.value;

    const payload = {
      name: formValue.name,
      description: formValue.description || '',
      categoryId: Number(formValue.categoryId),
      supplierId: Number(formValue.supplierId),
      notes: formValue.notes || '',
      variants: [
        {
          size: 'Default',
          color: '',
          classType: 'Standard',
          unitOfMeasure: 'Piece',
          quantityInStock: 1,
          pricePerUnit: 1,
          pricePerLength: 0,
          reorderLevel: 0,
          location: '',
          notes: '',
        },
      ],
    };

    if (this.editProductForm && this.selectedProductId) {
      this.productService
        .updateProduct(this.selectedProductId, payload)
        .subscribe({
          next: (res) => {
            this.isLoading = false;
            if (res.success) {
              this.showSuccess('Product update ho gaya!');
              this.closeAddProductForm();
              this.loadProducts();
            } else {
              this.errorMessage = res.message || 'Update nahi hua';
            }
          },
          error: (err) => {
            this.isLoading = false;
            this.errorMessage = 'Update fail ho gaya!';
            console.error('Full error:', err);
            console.error('Error details:', err.error);
          },
        });
    } else {
      this.productService.createProduct(payload).subscribe({
        next: (res) => {
          this.isLoading = false;
          if (res.success) {
            this.showSuccess('Naya product add ho gaya!');
            this.closeAddProductForm();
            this.loadProducts();
          } else {
            this.errorMessage = res.message || 'Product save nahi hua';
          }
        },
        error: (err) => {
          this.isLoading = false;
          this.errorMessage = 'Product save fail!';
          console.error('Full error:', err);
          console.error('Error details:', err.error);
        },
      });
    }
  }

  addVariant() {
    this.editVariantForm = false;
    this.selectedVariantId = null;
    this.variantForm.reset({
      pricePerUnit: 0,
      pricePerLength: 0,
      lengthValue: 0,
      quantityInStock: 0,
      reorderLevel: 0,
    });
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
    if (this.variantForm.invalid) {
      this.variantForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    const formValue = this.variantForm.value;

    const payload = {
      size: formValue.size,
      color: formValue.color || '',
      classType: formValue.classType,
      unitOfMeasure: formValue.unitOfMeasure,
      quantityInStock: Number(formValue.quantityInStock),
      pricePerUnit: Number(formValue.pricePerUnit),
      pricePerLength: Number(formValue.pricePerLength) || 0,
      reorderLevel: Number(formValue.reorderLevel),
      location: formValue.location || '',
      notes: formValue.notes || '',
    };

    if (this.editVariantForm && this.selectedVariantId) {
      this.productService
        .updateVariant(this.selectedVariantId, {
          variantId: this.selectedVariantId,
          ...payload,
          isActive: true,
        })
        .subscribe({
          next: (res) => {
            this.isLoading = false;
            if (res.success) {
              this.showSuccess('Variant update ho gaya!');
              this.closeAddVariantForm();
              this.loadProducts();
            } else {
              this.errorMessage = res.message || 'Update nahi hua';
            }
          },
          error: (err) => {
            this.isLoading = false;
            this.errorMessage = 'Variant update fail!';
            console.error(err);
          },
        });
    } else {
      const productId = Number(formValue.productId);
      this.productService.createVariant(productId, payload).subscribe({
        next: (res) => {
          this.isLoading = false;
          if (res.success) {
            this.showSuccess('Naya variant add ho gaya!');
            this.closeAddVariantForm();
            this.loadProducts();
          } else {
            this.errorMessage = res.message || 'Variant save nahi hua';
          }
        },
        error: (err) => {
          this.isLoading = false;
          this.errorMessage = 'Variant save fail!';
          console.error(err);
        },
      });
    }
  }

  editProduct(item: InventoryItem) {
    this.editProductForm = true;
    this.selectedProductId = item.productId;

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
      return;
    }

    this.editVariantForm = true;
    this.selectedVariantId = item._rawVariant.variantId;

    this.variantForm.patchValue({
      productId: item.productId,
      size: item._rawVariant.size,
      color: item._rawVariant.color || '',
      classType: item._rawVariant.classType,
      unitOfMeasure: item._rawVariant.unitOfMeasure,
      pricePerUnit: item._rawVariant.pricePerUnit,
      pricePerLength: item._rawVariant.pricePerLength || 0,
      quantityInStock: item._rawVariant.quantityInStock,
      reorderLevel: item._rawVariant.reorderLevel,
      location: item._rawVariant.location || '',
      notes: item._rawVariant.notes || '',
    });

    this.showAddVariantForm = true;
    this.animateModalOpen();
  }

  animateModalOpen() {
    setTimeout(() => {
      if (this.modalContent?.nativeElement) {
        gsap.fromTo(
          this.modalContent.nativeElement,
          { opacity: 0, scale: 0.8, y: 60 },
          { opacity: 1, scale: 1, y: 0, duration: 0.45, ease: 'back.out(1.4)' },
        );
      }
    }, 10);
  }

  animateModalClose(callback: () => void) {
    if (this.modalContent?.nativeElement) {
      gsap.to(this.modalContent.nativeElement, {
        opacity: 0,
        scale: 0.8,
        y: 60,
        duration: 0.3,
        ease: 'power2.in',
        onComplete: callback,
      });
    } else {
      callback();
    }
  }

  showSuccess(msg: string) {
    this.successMessage = msg;
    setTimeout(() => (this.successMessage = ''), 3000);
  }

  get name() {
    return this.productForm.get('name');
  }
  get category() {
    return this.productForm.get('categoryId');
  }
  get supplier() {
    return this.productForm.get('supplierId');
  }
  get productId() {
    return this.variantForm.get('productId');
  }
  get size() {
    return this.variantForm.get('size');
  }
  get classType() {
    return this.variantForm.get('classType');
  }
  get unitOfMeasure() {
    return this.variantForm.get('unitOfMeasure');
  }
  get pricePerUnit() {
    return this.variantForm.get('pricePerUnit');
  }
  get stockQuantity() {
    return this.variantForm.get('quantityInStock');
  }
  get reorderLevel() {
    return this.variantForm.get('reorderLevel');
  }

  get products() {
    return this.allProducts.map((p) => ({ id: p.productId, name: p.name }));
  }
}
