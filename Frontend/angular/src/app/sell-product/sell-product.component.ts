import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface QuotationItem {
  id: number;
  product: string;
  size: string;
  unit_of_measure: string;
  category_type: string;
  unitPrice: number;
  quantity: number;
  discount: number;
  total: number;
  final: number;
}
@Component({
  selector: 'app-sell-product',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sell-product.component.html',
  styleUrl: './sell-product.component.css'
})
export class SellProductComponent {
// Current date
  currentDate = new Date().toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  });

  // Search and type selection
  searchTerm: string = '';
  customerType: string = 'regular';

  // Dummy data for products
  products: QuotationItem[] = [
    { id: 1, product: 'Widget A', size: 'Medium', unit_of_measure: 'pcs', category_type: 'Electronics', unitPrice: 25.00, quantity: 10, discount: 5, total: 250.00, final: 237.50 },
    { id: 2, product: 'Gadget B', size: 'Large', unit_of_measure: 'pcs', category_type: 'Electronics', unitPrice: 150.00, quantity: 2, discount: 0, total: 300.00, final: 300.00 },
    { id: 3, product: 'Tool C', size: 'Small', unit_of_measure: 'box', category_type: 'Hardware', unitPrice: 12.50, quantity: 5, discount: 10, total: 62.50, final: 56.25 }
  ];

  // Paid amount
  paidAmount: number = 0;

  // Calculated getters
  get totalPrice(): number {
    return this.products.reduce((sum, item) => sum + item.total, 0);
  }

  get totalDiscount(): number {
    return this.products.reduce((sum, item) => sum + (item.total - item.final), 0);
  }

  get finalPrice(): number {
    return this.products.reduce((sum, item) => sum + item.final, 0);
  }

  get filteredProducts(): QuotationItem[] {
    if (!this.searchTerm) return this.products;
    return this.products.filter(p => 
      p.product.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
      p.category_type.toLowerCase().includes(this.searchTerm.toLowerCase())
    );
  }

  // Actions
  onSearch() {
    console.log('Searching:', this.searchTerm);
  }

  onCustomerTypeChange(type: string) {
    this.customerType = type;
    console.log('Customer type:', type);
  }

  onPrint() {
    console.log('Printing quotation...');
    window.print();
  }

  onProcessPayment() {
    console.log('Processing payment:', this.paidAmount);
  }
}
