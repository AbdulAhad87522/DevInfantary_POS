import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  ReturnsService,
  CreateReturnRequest,
  BillItem,
  BillDetail,
} from '../services/returns.service';

@Component({
  selector: 'app-return-items',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './return-items.component.html',
  styleUrls: ['./return-items.component.css'],
})
export class ReturnItemsComponent {
  billNumber: string = '';
  returnReason: string = '';
  notes: string = '';
  adjustedRefund: number = 0;
  restoreStock: boolean = true;

  currentBill: BillDetail | null = null;
  returnItems: BillItem[] = [];
  returnQuantities: { [billItemId: number]: number } = {};

  isSearching: boolean = false;
  isSubmitting: boolean = false;
  searchError: string = '';
  successMessage: string = '';

  constructor(private returnsService: ReturnsService) {}

  // ── Computed ──
  get totalQuantity(): number {
    return this.returnItems.reduce((sum, item) => {
      return sum + (this.returnQuantities[item.billItemId] ?? item.quantity);
    }, 0);
  }

  get totalAmount(): number {
    return this.returnItems.reduce((sum, item) => {
      const qty = this.returnQuantities[item.billItemId] ?? item.quantity;
      return sum + qty * item.unitPrice;
    }, 0);
  }

  get returnItemsCount(): number {
    return this.returnItems.length;
  }

  // ── Search Bill ──
  onSearchBill() {
    if (!this.billNumber.trim()) {
      this.searchError = 'Bill number likhna zaroori hai!';
      return;
    }

    this.isSearching = true;
    this.searchError = '';
    this.successMessage = '';
    this.returnItems = [];
    this.returnQuantities = {};
    this.currentBill = null;

    this.returnsService.getBillByNumber(this.billNumber.trim()).subscribe({
      next: (response) => {
        this.isSearching = false;
        if (response.success && response.data) {
          this.currentBill = response.data;
          this.returnItems = response.data.items;
          this.returnQuantities = {};
          this.returnItems.forEach(item => {
            this.returnQuantities[item.billItemId] = item.quantity;
          });
          this.recalcRefund();
        } else {
          this.searchError = response.message || 'Bill nahi mila!';
        }
      },
      error: (err) => {
        this.isSearching = false;
        this.searchError =
          err.status === 404
            ? 'Yeh bill number exist nahi karta!'
            : 'Server se data nahi aaya. Dobara try karo.';
        console.error('Search Error:', err);
      },
    });
  }

  // ── Quantity Controls ──
  onQuantityChange(item: BillItem) {
    let val = this.returnQuantities[item.billItemId];
    if (!val || val < 1) val = 1;
    if (val > item.quantity) val = item.quantity;
    this.returnQuantities[item.billItemId] = val;
    this.recalcRefund();
  }

  recalcRefund() {
    this.adjustedRefund = this.totalAmount;
  }

  // ── Process Return ──
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
      items: this.returnItems.map((item) => ({
        variantId: item.variantId,
        productName: item.productName,
        size: item.size,
        unit: item.unitOfMeasure,
        quantity: this.returnQuantities[item.billItemId] ?? item.quantity,
        unitPrice: item.unitPrice,
        lineTotal: (this.returnQuantities[item.billItemId] ?? item.quantity) * item.unitPrice,
        maxQuantity: item.quantity,
      })),
    };

    this.returnsService.createReturn(requestData).subscribe({
      next: (response) => {
        this.isSubmitting = false;
        if (response.success) {
          this.successMessage = `Return successfully process ho gaya! Refund: ₨ ${this.adjustedRefund.toLocaleString()}`;
          this.onResetForm();
        } else {
          this.searchError = response.message || 'Return process nahi hua!';
        }
      },
      error: (err) => {
        this.isSubmitting = false;
        this.searchError =
          err.error?.message || 'Return submit nahi hua. Dobara try karo.';
        console.error('Return Error:', err);
      },
    });
  }

  // ── Reset ──
  onResetForm() {
    this.billNumber = '';
    this.returnReason = '';
    this.notes = '';
    this.adjustedRefund = 0;
    this.returnItems = [];
    this.returnQuantities = {};
    this.currentBill = null;
    this.searchError = '';
    this.restoreStock = true;
  }
}