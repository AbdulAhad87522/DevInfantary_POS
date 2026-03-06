import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  SellProductService,
  Customer,
} from '../services/sell-product.service';
import { ProductService, PosProductResult, PosVariantResult } from '../services/product.service';

export interface SellItem {
  variantId: number;
  productId: number;
  productName: string;
  size: string;
  unitOfMeasure: string;
  category: string;
  unitPrice: number;
  quantity: number;
  discount: number;
  subtotal: number;
  final: number;
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

@Component({
  selector: 'app-sell-product',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sell-product.component.html',
  styleUrl: './sell-product.component.css',
})
export class SellProductComponent implements OnInit {
  currentDate = new Date().toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });

  // ── Customer ──────────────────────────────────────────────
  customerType: string = 'walkin';
  allCustomers: Customer[] = [];
  customerSearchTerm: string = '';
  customerOptions: Customer[] = [];
  showCustomerDropdown: boolean = false;
  selectedCustomer: Customer | null = null;

  // ── Quotation ─────────────────────────────────────────────
  quotationSearchTerm: string = '';
  isQuotationLoading: boolean = false;
  quotationError: string = '';
  loadedQuotation: any = null;

  // ── Product Search ────────────────────────────────────────
  productSearchTerm: string = '';
  productOptions: VariantOption[] = [];
  showProductDropdown: boolean = false;
  isProductLoading: boolean = false;
  searchDebounce: any = null;

  // ── Cart ──────────────────────────────────────────────────
  gridItems: SellItem[] = [];

  // ── Global Discount ───────────────────────────────────────
  globalDiscount: number = 0;

  // ── Detail Modal ──────────────────────────────────────────
  showBillModal: boolean = false;
  selectedItemForDetail: SellItem | null = null;

  // ── Payment ───────────────────────────────────────────────
  paidAmount: number = 0;
  isSubmitting: boolean = false;
  isPrinting: boolean = false;
  successMessage: string = '';
  lastBillNumber: string = '';
  lastBillId: number = 0;        // ← NEW
  errorMessage: string = '';
  paymentWarning: string = '';

  constructor(
    private sellService: SellProductService,
    private productService: ProductService,
  ) {}

  ngOnInit(): void {
    this.loadAllCustomers();
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

  onCustomerTypeChange(type: string) {
    this.customerType = type;
    this.selectedCustomer = null;
    this.customerSearchTerm = '';
    this.customerOptions = [];
    this.showCustomerDropdown = false;
    this.paymentWarning = '';
    this.paidAmount = this.netTotal;
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

  selectCustomer(customer: Customer) {
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
  // QUOTATION
  // ══════════════════════════════════════════════════════════
  onQuotationSearch() {
    const term = this.quotationSearchTerm.trim();
    if (!term) return;

    this.isQuotationLoading = true;
    this.quotationError = '';

    this.sellService.getQuotationByNumber(term).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.loadedQuotation = res.data;
          this.loadQuotationToGrid(res.data);
          this.quotationSearchTerm = '';
        } else {
          this.quotationError = `Quotation "${term}" nahi mili`;
        }
        this.isQuotationLoading = false;
      },
      error: (err) => {
        this.quotationError =
          err.status === 404
            ? `Quotation "${term}" exist nahi karti`
            : 'Server error, dobara try karo';
        this.isQuotationLoading = false;
      },
    });
  }

  loadQuotationToGrid(quotation: any) {
    if (!quotation.items || quotation.items.length === 0) return;
    quotation.items.forEach((item: any) => {
      const exists = this.gridItems.find((g) => g.variantId === item.variantId);
      if (!exists) {
        const subtotal = item.unitPrice * item.quantity;
        this.gridItems.push({
          variantId: item.variantId,
          productId: item.productId || 0,
          productName: item.productName,
          size: item.size,
          unitOfMeasure: item.unitOfMeasure || '—',
          category: item.category || '—',
          unitPrice: item.unitPrice,
          quantity: item.quantity,
          discount: 0,
          subtotal,
          final: subtotal,
          selected: false,
        });
      }
    });
    this.syncPaidAmount();
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

      this.productService.posSearch({
        searchTerm: term,
        maxResults: 15,
      }).subscribe({
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
      const subtotal = variant.pricePerUnit * 1;
      this.gridItems.push({
        variantId: variant.variantId,
        productId: variant.productId,
        productName: variant.productName,
        size: variant.size,
        unitOfMeasure: variant.unitOfMeasure,
        category: variant.categoryName,
        unitPrice: variant.pricePerUnit,
        quantity: 1,
        discount: 0,
        subtotal,
        final: subtotal,
        selected: false,
      });
    }
    this.productSearchTerm = '';
    this.productOptions = [];
    this.showProductDropdown = false;
    this.syncPaidAmount();
  }

  clearProductSearch() {
    this.productSearchTerm = '';
    this.productOptions = [];
    this.showProductDropdown = false;
  }

  // ══════════════════════════════════════════════════════════
  // CART OPERATIONS
  // ══════════════════════════════════════════════════════════
  onQuantityChange(item: SellItem) {
    if (item.quantity < 1) item.quantity = 1;
    this.recalcItem(item);
    this.syncPaidAmount();
  }

  onUnitPriceChange(item: SellItem) {
    if (item.unitPrice < 0) item.unitPrice = 0;
    this.recalcItem(item);
    this.syncPaidAmount();
  }

  onItemDiscountChange(item: SellItem) {
    if (item.discount < 0) item.discount = 0;
    if (item.discount > 100) item.discount = 100;
    this.recalcItem(item);
    this.syncPaidAmount();
  }

  onGlobalDiscountChange() {
    if (this.globalDiscount < 0) this.globalDiscount = 0;
    if (this.globalDiscount > 100) this.globalDiscount = 100;
    this.syncPaidAmount();
  }

  recalcItem(item: SellItem) {
    item.subtotal = item.unitPrice * item.quantity;
    item.final = item.subtotal - (item.subtotal * item.discount) / 100;
  }

  syncPaidAmount() {
    this.paidAmount = this.netTotal;
    this.paymentWarning = '';
  }

  deleteSelected() {
    this.gridItems = this.gridItems.filter((item) => !item.selected);
    this.syncPaidAmount();
  }

  removeItem(item: SellItem) {
    this.gridItems = this.gridItems.filter((g) => g !== item);
    this.syncPaidAmount();
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

  onRowClick(item: SellItem) {
    this.selectedItemForDetail = item;
    this.showBillModal = true;
  }

  closeBillModal() {
    this.showBillModal = false;
    this.selectedItemForDetail = null;
  }

  // ══════════════════════════════════════════════════════════
  // PRINT BILL — blob API call
  // ══════════════════════════════════════════════════════════
  printBill(billNumber: string): void {
    if (!this.lastBillId) {
      this.errorMessage = 'Bill ID nahi mili, dobara try karo';
      return;
    }
    this.isPrinting = true;
    this.sellService.getBillPdfById(this.lastBillId).subscribe({
      next: (blob: Blob) => {
        this.isPrinting = false;
        const url = URL.createObjectURL(blob);
        const printWindow = window.open(url, '_blank');
        if (printWindow) {
          printWindow.addEventListener('load', () => {
            printWindow.print();
            URL.revokeObjectURL(url);
          });
        }
      },
      error: (err: any) => {
        this.isPrinting = false;
        console.error('PDF Error:', err.status, err.error);
        this.errorMessage = 'PDF load nahi hua, dobara try karo';
      },
    });
  }

  // ══════════════════════════════════════════════════════════
  // TOTALS
  // ══════════════════════════════════════════════════════════
  get cartSubtotal(): number {
    return this.gridItems.reduce((sum, item) => sum + item.subtotal, 0);
  }

  get itemDiscountTotal(): number {
    return this.gridItems.reduce((sum, item) => sum + (item.subtotal - item.final), 0);
  }

  get afterItemDiscounts(): number {
    return this.gridItems.reduce((sum, item) => sum + item.final, 0);
  }

  get globalDiscountAmount(): number {
    return (this.afterItemDiscounts * this.globalDiscount) / 100;
  }

  get netTotal(): number {
    return this.afterItemDiscounts - this.globalDiscountAmount;
  }

  get totalDiscountAmount(): number {
    return this.itemDiscountTotal + this.globalDiscountAmount;
  }

  // ══════════════════════════════════════════════════════════
  // PAYMENT
  // ══════════════════════════════════════════════════════════
  onPaidAmountChange() {
    this.paymentWarning = '';
    if (this.customerType === 'walkin' && this.paidAmount < this.netTotal) {
      this.paymentWarning = `Walk-in customer ko full payment chahiye! Baaki: ₨ ${(this.netTotal - this.paidAmount).toFixed(0)}`;
    }
  }

  // ══════════════════════════════════════════════════════════
  // SUBMIT BILL
  // ══════════════════════════════════════════════════════════
  onProcessPayment() {
    this.errorMessage = '';
    this.paymentWarning = '';

    if (this.gridItems.length === 0) {
      this.errorMessage = 'Koi product add nahi kiya!';
      return;
    }
    if (this.customerType === 'regular' && !this.selectedCustomer) {
      this.errorMessage = 'Regular customer select karo pehle!';
      return;
    }
    if (this.customerType === 'walkin' && this.paidAmount < this.netTotal) {
      this.paymentWarning = `Walk-in ko full payment chahiye! Bill: ₨ ${this.netTotal.toFixed(0)}, Paid: ₨ ${this.paidAmount.toFixed(0)}`;
      return;
    }

    this.isSubmitting = true;

    const payload = {
      customerId: this.customerType === 'regular' ? this.selectedCustomer!.customerId : null,
      billDate: new Date().toISOString(),
      totalAmount: this.netTotal,
      paidAmount: this.paidAmount,
      discountAmount: this.totalDiscountAmount,
      items: this.gridItems.map((item) => ({
        variantId: item.variantId,
        productName: item.productName,
        size: item.size,
        quantity: item.quantity,
        unitPrice: item.unitPrice,
        discount: item.discount,
      })),
    };

    this.sellService.createBill(payload).subscribe({
      next: (res) => {
        this.isSubmitting = false;
        if (res.success) {
          const billNum = res.data?.bill?.billNumber || '';
          const billId  = res.data?.bill?.billId  || 0;   // ← billId save
          this.resetForm();
          this.lastBillNumber = billNum;
          this.lastBillId     = billId;                   // ← SET
          this.showSuccess(`Bill ${billNum} ban gaya! Ab Print Bill dabao.`);
        } else {
          this.errorMessage = res.message || 'Bill nahi bana';
        }
      },
      error: (err) => {
        this.isSubmitting = false;
        const msg = err.error?.errors?.[0] || err.error?.message || 'Server error!';
        this.errorMessage = msg;
        console.error('Bill error:', err.error);
      },
    });
  }

  resetForm() {
    this.gridItems = [];
    this.selectedCustomer = null;
    this.customerSearchTerm = '';
    this.customerType = 'walkin';
    this.quotationSearchTerm = '';
    this.productSearchTerm = '';
    this.paidAmount = 0;
    this.globalDiscount = 0;
    this.loadedQuotation = null;
    this.paymentWarning = '';
    this.errorMessage = '';
    // lastBillNumber aur lastBillId intentionally clear nahi — Print Bill kaam kare
  }

  onPrint() {
    window.print();
  }

  showSuccess(msg: string) {
    this.successMessage = msg;
    setTimeout(() => (this.successMessage = ''), 4000);
  }
}