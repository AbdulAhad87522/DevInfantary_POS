import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface ReturnItem {
  product: string;
  size: string;
  unit: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

@Component({
  selector: 'app-return-items',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './return-items.component.html',
  styleUrls: ['./return-items.component.css']
})
export class ReturnItemsComponent {
  // Dummy data for return items
  returnItems: ReturnItem[] = [
    { product: 'Widget A', size: 'Medium', unit: 'pcs', quantity: 5, unitPrice: 25.00, lineTotal: 125.00 },
    { product: 'Gadget B', size: 'Large', unit: 'pcs', quantity: 2, unitPrice: 150.00, lineTotal: 300.00 },
    { product: 'Tool C', size: 'Small', unit: 'box', quantity: 10, unitPrice: 12.50, lineTotal: 125.00 }
  ];

  billNumber: string = '';
  returnReason: string = '';
  notes: string = '';
  adjustedRefund: number = 550.00;

  // Calculate totals
  get totalQuantity(): number {
    return this.returnItems.reduce((sum, item) => sum + item.quantity, 0);
  }

  get totalAmount(): number {
    return this.returnItems.reduce((sum, item) => sum + item.lineTotal, 0);
  }

  get returnItemsCount(): number {
    return this.returnItems.length;
  }

  // Actions
  onSearchBill() {
    console.log('Searching bill:', this.billNumber);
    // Add your search logic here
  }

  onProcessReturn() {
    console.log('Processing return for bill:', this.billNumber);
    console.log('Return reason:', this.returnReason);
    console.log('Notes:', this.notes);
    console.log('Adjusted refund:', this.adjustedRefund);
    // Add your process logic here
  }

  onResetForm() {
    this.billNumber = '';
    this.returnReason = '';
    this.notes = '';
    this.adjustedRefund = this.totalAmount;
    console.log('Form reset');
  }
}