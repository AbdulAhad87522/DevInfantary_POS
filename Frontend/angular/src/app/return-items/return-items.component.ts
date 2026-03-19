import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UiStateService } from '../services/ui-state.service';

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
export class ReturnItemsComponent implements OnInit, OnDestroy {  billNumber: string = '';
  returnReason: string = '';
  notes: string = 'No additional notes';   // ← default value
  adjustedRefund: number = 0;
  restoreStock: boolean = true;

  // ── Validation ──
  reasonTouched: boolean = false;          // ← track karo kab user ne field ko touch kiya

  currentBill: BillDetail | null = null;
  returnItems: BillItem[] = [];
  returnQuantities: { [billItemId: number]: number } = {};
  selectedItems: Set<number> = new Set();

  isSearching: boolean = false;
  isSubmitting: boolean = false;
  searchError: string = '';
  successMessage: string = '';

  constructor(private returnsService: ReturnsService,
      private uiState: UiStateService,   // ← ADD

  ) {}

  // ── Computed — sirf selected items pe calculate ──
  get selectedReturnItems(): BillItem[] {
    return this.returnItems.filter(
      (i) =>
        this.selectedItems.has(i.billItemId) &&
        (this.returnQuantities[i.billItemId] ?? 0) > 0
    );
  }
ngOnInit(): void {
  const s = this.uiState.getReturns();
  if (s.billNumber) {
    this.billNumber = s.billNumber;
  }
}
get totalLineAmount(): number {
  return this.selectedReturnItems.reduce((sum, item) => {
    const qty = this.returnQuantities[item.billItemId] ?? item.quantity;
    return sum + (qty * item.unitPrice);
  }, 0);
}
ngOnDestroy(): void {
  // Sirf billNumber save karo — baaki data sensitive hai
  this.uiState.setReturns({ billNumber: this.billNumber });
}
  get totalQuantity(): number {
    return this.selectedReturnItems.reduce((sum, item) => {
      return sum + (this.returnQuantities[item.billItemId] ?? item.quantity);
    }, 0);
  }

 get totalAmount(): number {
  const total = this.selectedReturnItems.reduce((sum, item) => {
    const qty = this.returnQuantities[item.billItemId] ?? item.quantity;
    return sum + qty * item.unitPrice;
  }, 0);

  const discount = this.currentBill?.discount_percentage ?? 0;

  return total - (total * discount / 100);
}
  get returnItemsCount(): number {
    return this.selectedReturnItems.length;
  }

  // ── Reason validation getter ──
  get isReasonInvalid(): boolean {
    return this.reasonTouched && !this.returnReason;
  }

  // ── Selection Helpers ──
  isAllSelected(): boolean {
    return (
      this.returnItems.length > 0 &&
      this.returnItems.every((i) => this.selectedItems.has(i.billItemId))
    );
  }

  toggleSelectAll() {
    if (this.isAllSelected()) {
      this.selectedItems.clear();
    } else {
      this.returnItems.forEach((i) => this.selectedItems.add(i.billItemId));
    }
    this.recalcRefund();
  }

  toggleItemSelection(billItemId: number) {
    if (this.selectedItems.has(billItemId)) {
      this.selectedItems.delete(billItemId);
    } else {
      this.selectedItems.add(billItemId);
    }
    this.recalcRefund();
  }

  // ── Clear All ──
  setAllQuantitiesZero() {
    this.returnItems.forEach((item) => {
      this.returnQuantities[item.billItemId] = 0;
      this.selectedItems.delete(item.billItemId);
    });
    this.recalcRefund();
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
    this.selectedItems = new Set();
    this.currentBill = null;
    this.reasonTouched = false;             // ← reset validation on new search

    this.returnsService.getBillByNumber(this.billNumber.trim()).subscribe({
      next: (response) => {
        this.isSearching = false;
        if (response.success && response.data) {
          this.currentBill = response.data;
          this.returnItems = response.data.items;
          this.returnQuantities = {};
          this.selectedItems = new Set();
          this.returnItems.forEach((item) => {
            this.returnQuantities[item.billItemId] = item.quantity;
            this.selectedItems.add(item.billItemId);
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
    if (val === null || val === undefined || val < 0) val = 0;
    if (val > item.quantity) val = item.quantity;
    this.returnQuantities[item.billItemId] = val;

    if (val === 0) {
      this.selectedItems.delete(item.billItemId);
    } else {
      this.selectedItems.add(item.billItemId);
    }
    this.recalcRefund();
  }

  recalcRefund() {
    this.adjustedRefund = this.totalAmount;
  }

  // ── Process Return ──
  onProcessReturn() {
    // Reason mandatory check
    this.reasonTouched = true;
    if (!this.returnReason) {
      return;
    }

    if (!this.currentBill) {
      this.searchError = 'Pehle bill search karo!';
      return;
    }

    if (this.selectedReturnItems.length === 0) {
      this.searchError =
        'Koi item select nahi hai ya sab ki quantity zero hai!';
      return;
    }

    this.isSubmitting = true;
    this.searchError = '';

    const requestData: CreateReturnRequest = {
      billId: this.currentBill.billId,
      refundAmount: this.adjustedRefund,
      reason: this.returnReason,
      notes: this.notes || 'No additional notes',
      restoreStock: this.restoreStock,
      items: this.selectedReturnItems.map((item) => ({
        variantId: item.variantId,
        productName: item.productName,
        size: item.size,
        unit: item.unitOfMeasure,
        quantity: this.returnQuantities[item.billItemId] ?? item.quantity,
        unitPrice: item.unitPrice,
        lineTotal:
          (this.returnQuantities[item.billItemId] ?? item.quantity) *
          item.unitPrice,
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
  this.notes = 'No additional notes';
  this.adjustedRefund = 0;
  this.returnItems = [];
  this.returnQuantities = {};
  this.selectedItems = new Set();
  this.currentBill = null;
  this.searchError = '';
  this.restoreStock = true;
  this.reasonTouched = false;
  this.uiState.clearReturns();   // ← YEH ADD KARO
}
}