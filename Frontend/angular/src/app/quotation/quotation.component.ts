import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  QuotationService,
  Quotation,
  CreateQuotationRequest,
} from '../Services/quotation.service';
import { SellProductService } from '../Services/sell-product.service';
import { ProductService } from '../Services/product.service';

export interface VariantOption {
  variantId: number;
  productId: number;
  productName: string;
  categoryName: string;
  size: string;
  classType: string;
  unitOfMeasure: string;
  quantityInStock: number;
  pricePerUnit: number;
}

export interface CustomerOption {
  customerId: number;
  name: string;
  contact: string;
}

export interface NewQuotationItem {
  variantId: number;
  productName: string;
  size: string;
  unitOfMeasure: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
  notes: string;
}

export interface NewQuotationForm {
  selectedCustomer: CustomerOption | null;
  validUntil: string;
  notes: string;
  termsConditions: string;
  items: NewQuotationItem[];
}

@Component({
  selector: 'app-quotation',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './quotation.component.html',
  styleUrls: ['./quotation.component.css'],
})
export class QuotationComponent implements OnInit {

  // ── List ──
  quotations: Quotation[] = [];
  selectedQuotation: Quotation | null = null;

  // ── Search ──
  searchTerm: string = '';
  isSearching: boolean = false;

  // ── States ──
  isLoading: boolean = false;
  successMessage: string = '';
  errorMessage: string = '';
  isSubmitting: boolean = false;

  // ── Create Modal ──
  showCreateModal: boolean = false;

  customerSearchTerm: string = '';
  allCustomers: CustomerOption[] = [];
  customerOptions: CustomerOption[] = [];
  showCustomerDropdown: boolean = false;

  productSearchTerm: string = '';
  allVariants: VariantOption[] = [];
  productOptions: VariantOption[] = [];
  showProductDropdown: boolean = false;

  newQuotation: NewQuotationForm = this.getEmptyForm();

  constructor(
    private quotationService: QuotationService,
    private sellService: SellProductService,
    private productService: ProductService,
  ) {}

  ngOnInit(): void {
    this.loadQuotations();
    this.loadCustomers();
    this.loadProducts();
  }

  // ── Load Data ──
  loadQuotations(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.quotationService.getAllQuotations().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.quotations = res.data;
        } else {
          this.errorMessage = res.message || 'Quotations load nahi huin';
        }
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Quotations error:', err);
        this.errorMessage = 'Server se connect nahi ho pa raha!';
        this.isLoading = false;
      },
    });
  }

  loadCustomers(): void {
    this.sellService.getAllCustomers().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.allCustomers = res.data.map((c: any) => ({
            customerId: c.customerId,
            name: c.fullName,
            contact: c.phone || '',
          }));
        }
      },
    });
  }

  loadProducts(): void {
    this.productService.getAllProducts().subscribe({
      next: (res) => {
        if (!res.success || !res.data) return;

        const variants: VariantOption[] = [];
        res.data.forEach((product: any) => {
          if (product.variants?.length) {
            product.variants.forEach((v: any) => {
              if (v.isActive !== false) {
                variants.push({
                  variantId: v.variantId,
                  productId: product.productId,
                  productName: product.name,
                  categoryName: product.categoryName || '—',
                  size: v.size || '—',
                  classType: v.classType || '',
                  unitOfMeasure: v.unitOfMeasure || 'Piece',
                  quantityInStock: v.quantityInStock || 0,
                  pricePerUnit: v.pricePerUnit || 0,
                });
              }
            });
          }
        });
        this.allVariants = variants;
      },
      error: (err) => console.error('Products load failed:', err),
    });
  }

  // ── Quotation List ──
  selectQuotation(q: Quotation): void {
    this.selectedQuotation = q;
  }

  getStatusClass(status: string): string {
    if (!status) return 'status-default';
    const s = status.toLowerCase();
    if (s.includes('pending'))   return 'status-pending';
    if (s.includes('approved'))  return 'status-approved';
    if (s.includes('rejected'))  return 'status-rejected';
    if (s.includes('converted')) return 'status-converted';
    return 'status-default';
  }

  // ── Search ──
  onSearch(): void {
    const term = this.searchTerm.trim();
    if (!term) { this.loadQuotations(); return; }

    this.isSearching = true;
    this.errorMessage = '';

    this.quotationService.getQuotationByNumber(term).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.quotations = [res.data];
          this.selectedQuotation = res.data;
        } else {
          this.errorMessage = `"${term}" quotation nahi mili`;
          this.quotations = [];
          this.selectedQuotation = null;
        }
        this.isSearching = false;
      },
      error: (err) => {
        this.errorMessage = err.status === 404
          ? `"${term}" exist nahi karti`
          : 'Server error! Dobara try karo';
        this.quotations = [];
        this.selectedQuotation = null;
        this.isSearching = false;
      },
    });
  }

  onSearchInput(): void {
    if (!this.searchTerm.trim()) this.loadQuotations();
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.loadQuotations();
  }

  onPrint(): void { window.print(); }

  // ── Create Modal ──
  openCreateModal(): void {
    this.newQuotation = this.getEmptyForm();
    this.customerSearchTerm = '';
    this.productSearchTerm = '';
    this.customerOptions = [];
    this.productOptions = [];
    this.showCustomerDropdown = false;
    this.showProductDropdown = false;
    this.showCreateModal = true;
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
  }

  getEmptyForm(): NewQuotationForm {
    const d = new Date();
    d.setDate(d.getDate() + 30);
    return {
      selectedCustomer: null,
      validUntil: d.toISOString().split('T')[0],
      notes: '',
      termsConditions: '',
      items: [],
    };
  }

  // ── Customer Search ──
  onCustomerSearch(): void {
    const term = this.customerSearchTerm.trim().toLowerCase();
    if (term.length < 1) { this.customerOptions = []; this.showCustomerDropdown = false; return; }
    this.customerOptions = this.allCustomers.filter(
      (c) => c.name.toLowerCase().includes(term) || c.contact.toLowerCase().includes(term),
    );
    this.showCustomerDropdown = true;
  }

  selectCustomer(c: CustomerOption): void {
    this.newQuotation.selectedCustomer = c;
    this.customerSearchTerm = c.name;
    this.showCustomerDropdown = false;
    this.customerOptions = [];
  }

  clearCustomerSearch(): void {
    this.customerSearchTerm = '';
    this.newQuotation.selectedCustomer = null;
    this.customerOptions = [];
    this.showCustomerDropdown = false;
  }

  // ── Product Search ──
  onProductSearchInModal(): void {
    const term = this.productSearchTerm.trim().toLowerCase();
    if (term.length < 2) { this.productOptions = []; this.showProductDropdown = false; return; }
    this.productOptions = this.allVariants
      .filter((v) =>
        v.productName.toLowerCase().includes(term) ||
        v.size.toLowerCase().includes(term) ||
        v.categoryName.toLowerCase().includes(term),
      )
      .slice(0, 15);
    this.showProductDropdown = this.productOptions.length > 0;
  }

  addItemToQuotation(variant: VariantOption): void {
    const exists = this.newQuotation.items.find((i) => i.variantId === variant.variantId);
    if (exists) {
      exists.quantity += 1;
      exists.lineTotal = exists.unitPrice * exists.quantity;
    } else {
      this.newQuotation.items.push({
        variantId: variant.variantId,
        productName: variant.productName,
        size: variant.size,
        unitOfMeasure: variant.unitOfMeasure,
        unitPrice: variant.pricePerUnit,
        quantity: 1,
        lineTotal: variant.pricePerUnit,
        notes: '',
      });
    }
    this.productSearchTerm = '';
    this.productOptions = [];
    this.showProductDropdown = false;
  }

  recalcModalItem(item: NewQuotationItem): void {
    if (item.quantity < 1) item.quantity = 1;
    item.lineTotal = item.unitPrice * item.quantity;
  }

  removeModalItem(index: number): void {
    this.newQuotation.items.splice(index, 1);
  }

  // ── Computed Getters ──
  get modalTotal(): number {
    return this.newQuotation.items.reduce((sum, i) => sum + i.lineTotal, 0);
  }

  get modalTotalQty(): number {
    return this.newQuotation.items.reduce((sum, i) => sum + i.quantity, 0);
  }

  // ── Create Quotation ──
  onCreateQuotation(): void {
    this.errorMessage = '';

    if (!this.newQuotation.selectedCustomer) {
      this.errorMessage = 'Customer select karo pehle!';
      return;
    }
    if (this.newQuotation.items.length === 0) {
      this.errorMessage = 'Kam az kam ek item add karo!';
      return;
    }
    if (!this.newQuotation.validUntil) {
      this.errorMessage = 'Valid Until date dalo!';
      return;
    }

    this.isSubmitting = true;

    const payload: CreateQuotationRequest = {
      customerId: this.newQuotation.selectedCustomer.customerId,
      quotationDate: new Date().toISOString(),
      validUntil: new Date(this.newQuotation.validUntil).toISOString(),
      totalAmount: this.modalTotal,
      discountAmount: 0,
      notes: this.newQuotation.notes,
      termsConditions: this.newQuotation.termsConditions,
      items: this.newQuotation.items.map((i) => ({
        productName: i.productName,
        size: i.size,
        quantity: i.quantity,
        unitPrice: i.unitPrice,
        notes: i.notes,
      })),
    };

    this.quotationService.createQuotation(payload).subscribe({
      next: (res) => {
        this.isSubmitting = false;
        if (res.success) {
          this.showSuccess(`Quotation ${res.data?.quotationNumber || ''} ban gayi!`);
          this.closeCreateModal();
          this.loadQuotations();
          if (res.data) this.selectedQuotation = res.data;
        } else {
          this.errorMessage = res.message || 'Quotation nahi bani';
        }
      },
      error: (err) => {
        this.isSubmitting = false;
        const msg = err.error?.errors?.[0] || err.error?.message || '';
        this.errorMessage = msg || 'Server error! Dobara try karo.';
        console.error('Create quotation error:', err);
      },
    });
  }

  showSuccess(msg: string): void {
    this.successMessage = msg;
    setTimeout(() => (this.successMessage = ''), 4000);
  }
}