import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  QuotationService,
  Quotation,
  CreateQuotationRequest,
} from '../services/quotation.service';
import { SellProductService } from '../services/sell-product.service';
import { ProductService, PosProductResult, PosVariantResult } from '../services/product.service';

export interface QuotationGridItem {
  variantId: number;
  productId: number;
  productName: string;
  size: string;
  unitOfMeasure: string;
  category: string;
  unitPrice: number;
  quantity: number;
  subtotal: number;
  selected: boolean;
}

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
  displayText: string;
  inStock: boolean;
}

export interface CustomerOption {
  customerId: number;
  name: string;
  contact: string;
  address: string;
}

@Component({
  selector: 'app-quotation',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './quotation.component.html',
  styleUrls: ['./quotation.component.css'],
})
export class QuotationComponent implements OnInit {

  // ── View Mode ──
  viewMode: 'list' | 'create' = 'list';

  currentDate = new Date().toLocaleDateString('en-US', {
    year: 'numeric', month: 'long', day: 'numeric',
  });

  // ══════════════════════════════════════════════════════════
  // LIST VIEW STATE
  // ══════════════════════════════════════════════════════════
  quotations: Quotation[] = [];
  selectedQuotation: Quotation | null = null;
  isLoading: boolean = false;

  // ══════════════════════════════════════════════════════════
  // CREATE VIEW STATE
  // ══════════════════════════════════════════════════════════

  // ── Customer Type ──
  customerType: 'walkin' | 'regular' = 'walkin';
globalDiscount: number = 0;

  // ── Customer ──
  allCustomers: CustomerOption[] = [];
  customerSearchTerm: string = '';
  customerOptions: CustomerOption[] = [];
  showCustomerDropdown: boolean = false;
  selectedCustomer: CustomerOption | null = null;

  // ── Product Search ──
  productSearchTerm: string = '';
  productOptions: VariantOption[] = [];
  showProductDropdown: boolean = false;
  isProductLoading: boolean = false;
  searchDebounce: any = null;

  // ── Items Grid ──
  gridItems: QuotationGridItem[] = [];

  get totalQty(): number {
    return this.gridItems.reduce((sum, i) => sum + i.quantity, 0);
  }

  // ── Search (list view) ──
  searchTerm: string = '';
  isSearching: boolean = false;

  // ── Quotation Meta ──
  validUntil: string = '';
  notes: string = '';
  termsConditions: string = '';

  // ── Item Detail Modal ──
  showDetailModal: boolean = false;
  selectedItemForDetail: QuotationGridItem | null = null;

  // ── Shared States ──
  isSubmitting: boolean = false;
  isPrinting: boolean = false;
  successMessage: string = '';
  errorMessage: string = '';
  lastQuotationId: number = 0;
  lastQuotationNumber: string = '';

  constructor(
    private quotationService: QuotationService,
    private sellService: SellProductService,
    private productService: ProductService,
  ) {}

  ngOnInit(): void {
    this.loadAllCustomers();
    this.loadQuotations();
  }

  // ══════════════════════════════════════════════════════════
  // VIEW SWITCHING
  // ══════════════════════════════════════════════════════════
  openCreateView() {
    this.resetCreateForm();
    this.viewMode = 'create';
  }

  closeCreateView() {
    this.viewMode = 'list';
    this.errorMessage = '';
    this.successMessage = '';
  }

  // ══════════════════════════════════════════════════════════
  // LIST VIEW
  // ══════════════════════════════════════════════════════════
  loadQuotations() {
    this.isLoading = true;
    this.quotationService.getAllQuotations().subscribe({
      next: (res) => {
        if (res.success && res.data) this.quotations = res.data;
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; },
    });
  }

 selectQuotation(q: Quotation) {
  this.selectedQuotation = q; // pehle summary show karo
  
  // phir full details load karo with items
  this.quotationService.getQuotationById(q.quotationId).subscribe({
    next: (res) => {
      if (res.success && res.data) {
        this.selectedQuotation = res.data;
      }
    },
    error: (err) => console.error('Detail load failed:', err)
  });
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

    this.quotationService.searchQuotations({
      quotationNumber: term,
    }).subscribe({
      next: (res) => {
        if (res.success && res.data && res.data.length > 0) {
          this.quotations = res.data;
          this.selectedQuotation = res.data[0];
        } else {
          this.quotations = [];
          this.selectedQuotation = null;
          this.errorMessage = `"${term}" nahi mili`;
        }
        this.isSearching = false;
      },
      error: () => {
        this.errorMessage = 'Server error! Dobara try karo';
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

  // ══════════════════════════════════════════════════════════
  // PRINT
  // ══════════════════════════════════════════════════════════

  // Base64 string → PDF blob → print window
  private openPdfBase64(base64: string, fileName: string = 'quotation.pdf'): void {
    const byteCharacters = atob(base64);
    const byteNumbers = new Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
      byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    const byteArray = new Uint8Array(byteNumbers);
    const blob = new Blob([byteArray], { type: 'application/pdf' });
    const url = URL.createObjectURL(blob);

    const printWindow = window.open(url, '_blank');
    if (printWindow) {
      printWindow.addEventListener('load', () => {
        printWindow.print();
        URL.revokeObjectURL(url);
      });
    } else {
      // Popup blocked fallback — direct download
      const a = document.createElement('a');
      a.href = url;
      a.download = fileName;
      a.click();
      setTimeout(() => URL.revokeObjectURL(url), 1000);
    }
  }

  // Last saved quotation print (save ke baad badge/button)
  printLastQuotation(): void {
    if (!this.lastQuotationId) {
      this.errorMessage = 'Pehle quotation save karo!';
      return;
    }
    this.isPrinting = true;
    this.errorMessage = '';

    this.quotationService.getQuotationPdfById(this.lastQuotationId).subscribe({
      next: (res) => {
        this.isPrinting = false;
        if (res.success && res.data?.pdfBytes) {
          this.openPdfBase64(res.data.pdfBytes, res.data.pdfFileName || 'quotation.pdf');
        } else {
          this.errorMessage = 'PDF data nahi mila';
        }
      },
      error: (err: any) => {
        this.isPrinting = false;
        this.errorMessage = err.status === 404
          ? 'PDF nahi mili'
          : 'PDF load nahi hua, dobara try karo';
      },
    });
  }

  // List view mein selected quotation print
  printSelectedQuotation(): void {
    if (!this.selectedQuotation?.quotationId) {
      this.errorMessage = 'Koi quotation select nahi';
      return;
    }
    this.isPrinting = true;
    this.errorMessage = '';

    this.quotationService.getQuotationPdfById(this.selectedQuotation.quotationId).subscribe({
      next: (res) => {
        this.isPrinting = false;
        if (res.success && res.data?.pdfBytes) {
          this.openPdfBase64(res.data.pdfBytes, res.data.pdfFileName || 'quotation.pdf');
        } else {
          this.errorMessage = 'PDF data nahi mila';
        }
      },
      error: (err: any) => {
        this.isPrinting = false;
        this.errorMessage = err.status === 404
          ? 'Is quotation ki PDF nahi mili'
          : 'PDF load nahi hua, dobara try karo';
      },
    });
  }

  // ══════════════════════════════════════════════════════════
  // CUSTOMER TYPE
  // ══════════════════════════════════════════════════════════
  onCustomerTypeChange(type: 'walkin' | 'regular') {
    this.customerType = type;
    this.selectedCustomer = null;
    this.customerSearchTerm = '';
    this.customerOptions = [];
    this.showCustomerDropdown = false;
  }

  // ══════════════════════════════════════════════════════════
  // CUSTOMERS
  // ══════════════════════════════════════════════════════════
  loadAllCustomers() {
    this.sellService.getAllCustomers().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.allCustomers = res.data.map((c: any) => ({
            customerId: c.customerId,
            name: c.fullName || c.name,
            contact: c.phone || c.contact || '',
            address: c.address || '',
          }));
        }
      },
      error: (err) => console.error('Customers load failed:', err),
    });
  }

  onCustomerSearch() {
    const term = this.customerSearchTerm.trim().toLowerCase();
    if (term.length < 1) {
      this.customerOptions = [];
      this.showCustomerDropdown = false;
      return;
    }
    this.customerOptions = this.allCustomers.filter(
      (c) =>
        c.name.toLowerCase().includes(term) ||
        (c.contact && c.contact.toLowerCase().includes(term)),
    );
    this.showCustomerDropdown = true;
  }

  selectCustomer(customer: CustomerOption) {
    this.selectedCustomer = customer;
    this.customerSearchTerm = customer.name;
    this.showCustomerDropdown = false;
    this.customerOptions = [];
  }

  clearCustomerSearch() {
    this.customerSearchTerm = '';
    this.selectedCustomer = null;
    this.customerOptions = [];
    this.showCustomerDropdown = false;
  }

  // ══════════════════════════════════════════════════════════
  // PRODUCT SEARCH
  // ══════════════════════════════════════════════════════════
  onProductSearch() {
    const term = this.productSearchTerm.trim();
    if (term.length < 1) {
      this.productOptions = [];
      this.showProductDropdown = false;
      return;
    }
    clearTimeout(this.searchDebounce);
    this.searchDebounce = setTimeout(() => {
      this.isProductLoading = true;
      this.showProductDropdown = true;

      this.productService.posSearch({ searchTerm: term, maxResults: 15 }).subscribe({
        next: (res) => {
          this.isProductLoading = false;
          if (res.success && res.data) {
            const options: VariantOption[] = [];
            res.data.forEach((product: PosProductResult) => {
              product.variants.forEach((v: PosVariantResult) => {
                options.push({
                  variantId: v.variantId,
                  productId: product.productId,
                  productName: product.productName,
                  categoryName: product.categoryName,
                  size: v.size,
                  classType: v.classType,
                  unitOfMeasure: v.unitOfMeasure,
                  quantityInStock: v.quantityInStock,
                  pricePerUnit: v.pricePerUnit,
                  displayText: v.displayText,
                  inStock: v.inStock,
                });
              });
            });
            this.productOptions = options;
          } else {
            this.productOptions = [];
          }
        },
        error: (err) => {
          console.error('POS search failed:', err);
          this.isProductLoading = false;
          this.productOptions = [];
        },
      });
    }, 300);
  }

  selectProductOption(variant: VariantOption) {
    const exists = this.gridItems.find((g) => g.variantId === variant.variantId);
    if (exists) {
      exists.quantity += 1;
      this.recalcItem(exists);
    } else {
      this.gridItems.push({
        variantId: variant.variantId,
        productId: variant.productId,
        productName: variant.productName,
        size: variant.size,
        unitOfMeasure: variant.unitOfMeasure,
        category: variant.categoryName,
        unitPrice: variant.pricePerUnit,
        quantity: 1,
        subtotal: variant.pricePerUnit,
        selected: false,
      });
    }
    this.productSearchTerm = '';
    this.productOptions = [];
    this.showProductDropdown = false;
  }

  clearProductSearch() {
    this.productSearchTerm = '';
    this.productOptions = [];
    this.showProductDropdown = false;
  }

  // ══════════════════════════════════════════════════════════
  // GRID OPERATIONS
  // ══════════════════════════════════════════════════════════
  onQuantityChange(item: QuotationGridItem) {
    if (item.quantity < 1) item.quantity = 1;
    this.recalcItem(item);
  }

  onUnitPriceChange(item: QuotationGridItem) {
    if (item.unitPrice < 0) item.unitPrice = 0;
    this.recalcItem(item);
  }

  recalcItem(item: QuotationGridItem) {
    item.subtotal = item.unitPrice * item.quantity;
  }

  deleteSelected() {
    this.gridItems = this.gridItems.filter((item) => !item.selected);
  }

  removeItem(item: QuotationGridItem) {
    this.gridItems = this.gridItems.filter((g) => g !== item);
  }

  toggleSelectAll(event: any) {
    this.gridItems.forEach((item) => (item.selected = event.target.checked));
  }

  get hasSelectedItems(): boolean {
    return this.gridItems.some((item) => item.selected);
  }

  get selectedCount(): number {
    return this.gridItems.filter((item) => item.selected).length;
  }

  get allSelected(): boolean {
    return this.gridItems.length > 0 && this.gridItems.every((item) => item.selected);
  }

  onRowClick(item: QuotationGridItem) {
    this.selectedItemForDetail = item;
    this.showDetailModal = true;
  }

  closeDetailModal() {
    this.showDetailModal = false;
    this.selectedItemForDetail = null;
  }

  // ══════════════════════════════════════════════════════════
  // TOTALS
  // ══════════════════════════════════════════════════════════
  get cartSubtotal(): number {
  return this.gridItems.reduce((sum, i) => sum + i.subtotal, 0);
}

get globalDiscountAmount(): number {
  return (this.cartSubtotal * this.globalDiscount) / 100;
}

get netTotal(): number {
  return this.cartSubtotal - this.globalDiscountAmount;
}
onGlobalDiscountChange() {
  if (this.globalDiscount < 0) this.globalDiscount = 0;
  if (this.globalDiscount > 100) this.globalDiscount = 100;
}

  // ══════════════════════════════════════════════════════════
  // SAVE QUOTATION
  // ══════════════════════════════════════════════════════════
  onSaveQuotation() {
    this.errorMessage = '';

    if (this.customerType === 'regular' && !this.selectedCustomer) {
      this.errorMessage = 'Customer select karo pehle!';
      return;
    }
    if (this.gridItems.length === 0) {
      this.errorMessage = 'Koi product add nahi kiya!';
      return;
    }
    if (!this.validUntil) {
      this.errorMessage = 'Valid Until date dalo!';
      return;
    }

    this.isSubmitting = true;

    const payload: CreateQuotationRequest = {
      customerId: this.customerType === 'walkin' ? 1 : this.selectedCustomer!.customerId,
      quotationDate: new Date().toISOString(),
      validUntil: new Date(this.validUntil).toISOString(),
      totalAmount: this.netTotal,               // pehle cartSubtotal tha
discountAmount: this.globalDiscountAmount, // pehle 0 tha
      notes: this.notes,
      termsConditions: this.termsConditions,
      items: this.gridItems.map((item) => ({
        productName: item.productName,
        size: item.size,
        quantity: item.quantity,
        unitPrice: item.unitPrice,
        notes: '',
      })),
    };

    this.quotationService.createQuotation(payload).subscribe({
      next: (res) => {
        this.isSubmitting = false;
        if (res.success) {
          const qtNum = res.data?.quotationNumber || '';
          const qtId  = res.data?.quotationId || 0;
          this.loadQuotations();
          if (res.data) this.selectedQuotation = res.data;
          this.lastQuotationNumber = qtNum;
this.lastQuotationId = qtId;
this.gridItems = [];
this.showSuccess(`Quotation ${qtNum} save ho gayi! Ab Print dabao.`);
        } else {
          this.errorMessage = res.message || 'Quotation save nahi hui';
        }
      },
      error: (err) => {
        this.isSubmitting = false;
        const msg = err.error?.errors?.[0] || err.error?.message || 'Server error!';
        this.errorMessage = msg;
      },
    });
  }

  resetCreateForm() {
    this.globalDiscount = 0;
    this.gridItems = [];
    this.customerType = 'walkin';
    this.selectedCustomer = null;
    this.customerSearchTerm = '';
    this.productSearchTerm = '';
    this.notes = '';
    this.termsConditions = '';
    this.errorMessage = '';
    this.showDetailModal = false;
    this.selectedItemForDetail = null;
    const d = new Date();
    d.setDate(d.getDate() + 30);
    this.validUntil = d.toISOString().split('T')[0];
  }

  showSuccess(msg: string) {
    this.successMessage = msg;
    setTimeout(() => (this.successMessage = ''), 4000);
  }
}