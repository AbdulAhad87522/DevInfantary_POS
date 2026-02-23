import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReturnsService, CreateReturnRequest, BillItem, BillDetail } from '../services/returns.service';
// path adjust karo agar service alag folder mein hai

@Component({
  selector: 'app-return-items',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './return-items.component.html',
  styleUrls: ['./return-items.component.css']
})
export class ReturnItemsComponent {

  billNumber: string = '';
  returnReason: string = '';
  notes: string = '';
  adjustedRefund: number = 0;
  restoreStock: boolean = true;

  // Bill ka data (API se aayega)
  currentBill: BillDetail | null = null;
  returnItems: BillItem[] = [];

  // States
  isSearching: boolean = false;
  isSubmitting: boolean = false;
  searchError: string = '';
  successMessage: string = '';

  constructor(private returnsService: ReturnsService) {}

  // Computed values
  get totalQuantity(): number {
    return this.returnItems.reduce((sum, item) => sum + item.quantity, 0);
  }

  get totalAmount(): number {
    return this.returnItems.reduce((sum, item) => sum + item.lineTotal, 0);
  }

  get returnItemsCount(): number {
    return this.returnItems.length;
  }

  // Bill number search - API call
  onSearchBill() {
    if (!this.billNumber.trim()) {
      this.searchError = 'Bill number likhna zaroori hai!';
      return;
    }

    this.isSearching = true;
    this.searchError = '';
    this.successMessage = '';
    this.returnItems = [];
    this.currentBill = null;

    this.returnsService.getBillByNumber(this.billNumber.trim()).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.currentBill = response.data;
          this.returnItems = response.data.items;
          this.adjustedRefund = response.data.totalAmount;
        } else {
          this.searchError = response.message || 'Bill nahi mila!';
        }
        this.isSearching = false;
      },
      error: (err) => {
        console.error('Search Error:', err);
        if (err.status === 404) {
          this.searchError = 'Yeh bill number exist nahi karta!';
        } else {
          this.searchError = 'Server se data nahi aaya. Dobara try karo.';
        }
        this.isSearching = false;
      }
    });
  }

  // Return process karo - POST API
  onProcessReturn() {
    if (!this.currentBill) {
      this.searchError = 'Pehle bill search karo!';
      return;
    }

    if (this.returnItems.length === 0) {
      this.searchError = 'Return karne ke liye koi item nahi hai!';
      return;
    }

    this.isSubmitting = true;
    this.searchError = '';

    const requestData: CreateReturnRequest = {
      billId: this.currentBill.billId,
      refundAmount: this.adjustedRefund,
      reason: this.returnReason,
      notes: this.notes,
      restoreStock: this.restoreStock,
      items: this.returnItems.map(item => ({
        variantId: item.variantId,
        productName: item.productName,
        size: item.size,
        unit: item.unitOfMeasure,
        quantity: item.quantity,
        unitPrice: item.unitPrice,
        lineTotal: item.lineTotal,
        maxQuantity: item.quantity
      }))
    };

    this.returnsService.createReturn(requestData).subscribe({
      next: (response) => {
        if (response.success) {
          this.successMessage = 'Return successfully process ho gaya!';
          this.onResetForm();
        } else {
          this.searchError = response.message || 'Return process nahi hua!';
        }
        this.isSubmitting = false;
      },
      error: (err) => {
        console.error('Return Error:', err);
        this.searchError = err.error?.message || 'Return submit nahi hua. Dobara try karo.';
        this.isSubmitting = false;
      }
    });
  }

  onResetForm() {
    this.billNumber = '';
    this.returnReason = '';
    this.notes = '';
    this.adjustedRefund = 0;
    this.returnItems = [];
    this.currentBill = null;
    this.searchError = '';
    this.restoreStock = true;
  }
}