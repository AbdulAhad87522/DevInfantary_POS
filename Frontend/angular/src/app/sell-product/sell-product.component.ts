import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  SellProductService,
  Customer,
  Bill,
} from '../services/sell-product.service';
import { ProductService, Product } from '../services/product.service';
export interface SellItem {
  variantId: number;
  productName: string;
  size: string;
  unitOfMeasure: string;
  category: string;
  unitPrice: number;
  quantity: number;
  discount: number;
  total: number;
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

  // CUSTOMER
  customerType: string = 'walkin';
  allCustomers: Customer[] = [];
  customerSearchTerm: string = '';
  customerOptions: Customer[] = [];
  showCustomerDropdown: boolean = false;
  isCustomerLoading: boolean = false;
  selectedCustomer: Customer | null = null;

  // QUOTATION SEARCH
  quotationSearchTerm: string = '';
  isQuotationLoading: boolean = false;
  quotationError: string = '';
  loadedQuotation: any = null;

  // PRODUCT SEARCH
  productSearchTerm: string = '';
  allVariants: VariantOption[] = [];
  productOptions: VariantOption[] = [];
  showProductDropdown: boolean = false;
  isProductLoading: boolean = false;

  // GRID
  gridItems: SellItem[] = [];

  // DETAIL MODAL
  showBillModal: boolean = false;
  selectedItemForDetail: SellItem | null = null;

  // PAYMENT
  paidAmount: number = 0;
  isSubmitting: boolean = false;
  successMessage: string = '';
  errorMessage: string = '';
  paymentWarning: string = '';

  constructor(
    private sellService: SellProductService,
    private productService: ProductService,
  ) {}

  ngOnInit(): void {
    this.loadAllCustomers();
    this.loadAllProducts();
  }

  // ==========================================
  // LOAD CUSTOMERS
  // ==========================================
  loadAllCustomers() {
    this.sellService.getAllCustomers().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.allCustomers = res.data.map((c: any) => ({
            customerId: c.customerId,
            name: c.fullName,
            contact: c.phone || '',
            address: c.address || '',
          }));
        }
      },
      error: (err) => console.error('Customers load failed:', err),
    });
  }

  // ==========================================
  // LOAD ALL PRODUCTS
  // ==========================================
loadAllProducts() {
  console.log('🔄 Products load ho rahe hain...');

  this.productService.getAllProducts().subscribe({
    next: (res) => {
      console.log('📦 API Response:', res);

      if (!res.success || !res.data) {
        console.error('❌ Products fetch failed');
        return;
      }

      const products = res.data;
      const variants: VariantOption[] = [];
      let completedRequests = 0;
      const totalProducts = products.length;

      if (totalProducts === 0) {
        this.allVariants = [];
        return;
      }

      // Har product ke liye alag variant API call
      products.forEach((product: any) => {
        this.productService.getVariantsByProduct(product.productId).subscribe({
          next: (varRes) => {
            console.log(`📦 ${product.name} variants:`, varRes.data);

            if (varRes.success && varRes.data && varRes.data.length > 0) {
              varRes.data.forEach((v: any) => {
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
              });
            }

            completedRequests++;

            // Jab saare products ke variants aa jayein
            if (completedRequests === totalProducts) {
              this.allVariants = variants;
              console.log(`✅ Total variants loaded: ${variants.length}`);
            }
          },
          error: (err) => {
            console.error(`❌ ${product.name} variants load failed:`, err);
            completedRequests++;
            if (completedRequests === totalProducts) {
              this.allVariants = variants;
            }
          } 
        });
      });
    },
    error: (err) => console.error('❌ Products load failed:', err)
  });
}
  // ==========================================
  // CUSTOMER TYPE
  // ==========================================
  onCustomerTypeChange(type: string) {
    this.customerType = type;
    this.selectedCustomer = null;
    this.customerSearchTerm = '';
    this.customerOptions = [];
    this.showCustomerDropdown = false;
    this.paymentWarning = '';
    this.paidAmount = this.finalPrice;
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

  // ==========================================
  // QUOTATION SEARCH
  // ==========================================
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
        const total = item.unitPrice * item.quantity;
        this.gridItems.push({
          variantId: item.variantId,
          productName: item.productName,
          size: item.size,
          unitOfMeasure: item.unitOfMeasure || '—',
          category: item.category || '—',
          unitPrice: item.unitPrice,
          quantity: item.quantity,
          discount: 0,
          total: total,
          final: total,
          selected: false,
        });
      }
    });

    this.recalcTotals();
  }

  // ==========================================
  // PRODUCT SEARCH - Local filter
  // ==========================================
  onProductSearch() {
    const term = this.productSearchTerm.trim().toLowerCase();
    console.log(
      `🔍 Search term: "${term}", allVariants count: ${this.allVariants.length}`,
    );

    if (term.length < 2) {
      this.productOptions = [];
      this.showProductDropdown = false;
      return;
    }

    this.productOptions = this.allVariants
      .filter(
        (v) =>
          v.productName.toLowerCase().includes(term) ||
          v.size.toLowerCase().includes(term) ||
          v.categoryName.toLowerCase().includes(term) ||
          v.classType.toLowerCase().includes(term),
      )
      .slice(0, 15);

    console.log(`Found ${this.productOptions.length} options`);
    this.showProductDropdown = true;
  }

  // ✅ YEH FUNCTION MISSING THA
  selectProductOption(variant: VariantOption) {
    const exists = this.gridItems.find(
      (g) => g.variantId === variant.variantId,
    );

    if (exists) {
      exists.quantity += 1;
      this.recalcItem(exists);
    } else {
      const total = variant.pricePerUnit * 1;
      this.gridItems.push({
        variantId: variant.variantId,
        productName: variant.productName,
        size: variant.size,
        unitOfMeasure: variant.unitOfMeasure,
        category: variant.categoryName,
        unitPrice: variant.pricePerUnit,
        quantity: 1,
        discount: 0,
        total: total,
        final: total,
        selected: false,
      });
    }

    this.productSearchTerm = '';
    this.productOptions = [];
    this.showProductDropdown = false;
    this.recalcTotals();
  }

  clearProductSearch() {
    this.productSearchTerm = '';
    this.productOptions = [];
    this.showProductDropdown = false;
  }

  // ==========================================
  // GRID OPERATIONS
  // ==========================================
  onQuantityChange(item: SellItem) {
    if (item.quantity < 1) item.quantity = 1;
    this.recalcItem(item);
    this.recalcTotals();
  }

  onDiscountChange(item: SellItem) {
    if (item.discount < 0) item.discount = 0;
    if (item.discount > 100) item.discount = 100;
    this.recalcItem(item);
    this.recalcTotals();
  }

  recalcItem(item: SellItem) {
    item.total = item.unitPrice * item.quantity;
    item.final = item.total - (item.total * item.discount) / 100;
  }

  recalcTotals() {
    this.paidAmount = this.finalPrice;
    this.paymentWarning = '';
  }

  onPaidAmountChange() {
    this.paymentWarning = '';
    if (this.customerType === 'walkin') {
      if (this.paidAmount < this.finalPrice) {
        this.paymentWarning = `Walk-in customer ko full amount dena zaroori hai! Baaki: PKR ${(this.finalPrice - this.paidAmount).toFixed(0)}`;
      }
    }
  }

  deleteSelected() {
    this.gridItems = this.gridItems.filter((item) => !item.selected);
    this.recalcTotals();
  }

  get hasSelectedItems(): boolean {
    return this.gridItems.some((item) => item.selected);
  }

  get selectedCount(): number {
    return this.gridItems.filter((item) => item.selected).length;
  }

  toggleSelectAll(event: any) {
    this.gridItems.forEach((item) => (item.selected = event.target.checked));
  }

  get allSelected(): boolean {
    return (
      this.gridItems.length > 0 && this.gridItems.every((item) => item.selected)
    );
  }

  onRowClick(item: SellItem) {
    this.selectedItemForDetail = item;
    this.showBillModal = true;
  }

  closeBillModal() {
    this.showBillModal = false;
    this.selectedItemForDetail = null;
  }

  // ==========================================
  // TOTALS
  // ==========================================
  get totalPrice(): number {
    return this.gridItems.reduce((sum, item) => sum + item.total, 0);
  }

  get totalDiscount(): number {
    return this.gridItems.reduce(
      (sum, item) => sum + (item.total - item.final),
      0,
    );
  }

  get finalPrice(): number {
    return this.gridItems.reduce((sum, item) => sum + item.final, 0);
  }

  // ==========================================
  // SUBMIT BILL
  // ==========================================
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

    if (this.customerType === 'walkin' && this.paidAmount < this.finalPrice) {
      this.paymentWarning = `Walk-in customer ko full amount dena zaroori hai! Bill: PKR ${this.finalPrice.toFixed(0)}, Paid: PKR ${this.paidAmount.toFixed(0)}`;
      return;
    }

    this.isSubmitting = true;

    const payload = {
      customerId:
        this.customerType === 'regular'
          ? this.selectedCustomer!.customerId
          : null,
      billDate: new Date().toISOString(),
      totalAmount: this.finalPrice,
      paidAmount: this.paidAmount,
      discountAmount: this.totalDiscount,
      items: this.gridItems.map((item) => ({
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
          this.showSuccess(
            `Bill ${res.data?.billNumber || ''} successfully ban gaya!`,
          );
          this.resetForm();
        } else {
          this.errorMessage = res.message || 'Bill nahi bana';
        }
      },
      error: (err) => {
        this.isSubmitting = false;
        const backendMsg = err.error?.errors?.[0] || err.error?.message || '';
        if (
          backendMsg.includes('foreign key') ||
          backendMsg.includes('customer')
        ) {
          this.errorMessage =
            'Walk-in bill error - backend se confirm karo null customerId chalega ya nahi';
        } else {
          this.errorMessage = backendMsg || 'Server error! Dobara try karo.';
        }
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
    this.loadedQuotation = null;
    this.paymentWarning = '';
    this.errorMessage = '';
  }

  onPrint() {
    window.print();
  }

  showSuccess(msg: string) {
    this.successMessage = msg;
    setTimeout(() => (this.successMessage = ''), 4000);
  }
}
