import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class UiStateService {

  // ══════════════════════════════════════════
  // SELL PRODUCT STATE
  // ══════════════════════════════════════════
  private _sell = new BehaviorSubject<any>({
    gridItems: [],
    globalDiscount: 0,
    customerType: 'walkin',
    selectedCustomer: null,
    customerSearchTerm: '',
    paidAmount: 0,
  });

  getSell()                 { return this._sell.getValue(); }
  setSell(s: Partial<any>)  { this._sell.next({ ...this._sell.getValue(), ...s }); }
  clearSell()               { this._sell.next({ gridItems: [], globalDiscount: 0, customerType: 'walkin', selectedCustomer: null, customerSearchTerm: '', paidAmount: 0 }); }

  // ══════════════════════════════════════════
  // QUOTATION STATE
  // ══════════════════════════════════════════
  private _quotation = new BehaviorSubject<any>({
    gridItems: [],
    globalDiscount: 0,
    customerType: 'walkin',
    selectedCustomer: null,
    customerSearchTerm: '',
    validUntil: '',
    notes: '',
    termsConditions: '',
  });

  getQuotation()                { return this._quotation.getValue(); }
  setQuotation(s: Partial<any>) { this._quotation.next({ ...this._quotation.getValue(), ...s }); }
  clearQuotation()              { this._quotation.next({ gridItems: [], globalDiscount: 0, customerType: 'walkin', selectedCustomer: null, customerSearchTerm: '', validUntil: '', notes: '', termsConditions: '' }); }

  // ══════════════════════════════════════════
  // CUSTOMER BILLS STATE
  // ══════════════════════════════════════════
  private _customerBills = new BehaviorSubject<any>({
    searchTerm: '',
    activeFilter: 'all',
    detailTab: 'bills',
    billFilter: 'all',
    paymentFilterType: 'all',
    paymentFilterFrom: '',
    paymentFilterTo: '',
  });

  getCustomerBills()                { return this._customerBills.getValue(); }
  setCustomerBills(s: Partial<any>) { this._customerBills.next({ ...this._customerBills.getValue(), ...s }); }

  // ══════════════════════════════════════════
  // SUPPLIER BILLS STATE
  // ══════════════════════════════════════════
  private _supplierBills = new BehaviorSubject<any>({
    searchTerm: '',
    activeFilter: 'all',
    detailTab: 'batches',
    batchFilter: 'all',
    paymentFilterType: 'all',
    paymentFilterFrom: '',
    paymentFilterTo: '',
  });

  getSupplierBills()                { return this._supplierBills.getValue(); }
  setSupplierBills(s: Partial<any>) { this._supplierBills.next({ ...this._supplierBills.getValue(), ...s }); }

  // ══════════════════════════════════════════
  // BATCH (PURCHASE) STATE
  // ══════════════════════════════════════════
  private _batch = new BehaviorSubject<any>({
    searchTerm: '',
  });

  getBatch()                { return this._batch.getValue(); }
  setBatch(s: Partial<any>) { this._batch.next({ ...this._batch.getValue(), ...s }); }

  // ══════════════════════════════════════════
  // INVENTORY STATE
  // ══════════════════════════════════════════
  private _inventory = new BehaviorSubject<any>({
    searchTerm: '',
    filter: 'all',
  });

  getInventory()                { return this._inventory.getValue(); }
  setInventory(s: Partial<any>) { this._inventory.next({ ...this._inventory.getValue(), ...s }); }

  // ══════════════════════════════════════════
  // RETURNS STATE
  // ══════════════════════════════════════════
  private _returns = new BehaviorSubject<any>({
    billNumber: '',
  });

  getReturns()                { return this._returns.getValue(); }
  setReturns(s: Partial<any>) { this._returns.next({ ...this._returns.getValue(), ...s }); }
  clearReturns()              { this._returns.next({ billNumber: '' }); }
  // ══════════════════════════════════════════
// SUPPLIER MANAGEMENT STATE
// ══════════════════════════════════════════
private _supplierMgmt = new BehaviorSubject<any>({
  searchTerm: '',
  includeInactive: false,
});

getSupplierMgmt()                { return this._supplierMgmt.getValue(); }
setSupplierMgmt(s: Partial<any>) { this._supplierMgmt.next({ ...this._supplierMgmt.getValue(), ...s }); }

// ══════════════════════════════════════════
// CUSTOMER MANAGEMENT STATE
// ══════════════════════════════════════════
private _customerMgmt = new BehaviorSubject<any>({
  searchTerm: '',
  includeInactive: false,
});

getCustomerMgmt()                { return this._customerMgmt.getValue(); }
setCustomerMgmt(s: Partial<any>) { this._customerMgmt.next({ ...this._customerMgmt.getValue(), ...s }); }
}