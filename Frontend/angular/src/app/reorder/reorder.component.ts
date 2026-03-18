/**
 * reorder.component.ts
 * ─────────────────────────────────────────────────────────────
 * Purpose:
 *   1. Load all products with variants (getProductsWithDetails)
 *   2. Filter to only LOW STOCK and OUT OF STOCK variants
 *   3. Let the user CHECK items they want to reorder
 *   4. For each checked item, enter desired Quantity and Notes
 *   5. Click "Generate PDF" → opens a printable demand slip
 *
 * No Edit / Delete / API mutations — read-only + print only.
 * ─────────────────────────────────────────────────────────────
 */

import {
  Component, OnInit, OnDestroy,
  ChangeDetectionStrategy, ChangeDetectorRef
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil, finalize } from 'rxjs';
import { ProductService, Product, ProductVariant } from '../services/product.service';

/** Flat row shown in the checklist table */
export interface ReorderRow {
  // identifiers
  productId:     number;
  variantId:     number;

  // display fields
  productName:   string;
  supplierName:  string;
  categoryName:  string;
  size:          string;
  classType:     string;
  unitOfMeasure: string;
  color:         string;

  // stock info
  currentStock:  number;
  reorderLevel:  number;
  stockStatus:   'low' | 'out';

  // user inputs
  selected:      boolean;   // checkbox
  orderQty:      number;    // how many to order
  notes:         string;    // optional note for this line
}

@Component({
  selector: 'app-reorder',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reorder.component.html',
  styleUrls: ['./reorder.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReorderComponent implements OnInit, OnDestroy {

  // ── State ─────────────────────────────────────────────────
  rows:         ReorderRow[] = [];   // all low/out-of-stock rows
  loading       = false;
  storeName     = 'DevInfantary Hardware Store';   // shown on PDF header
  today         = new Date();

  // ── Search / filter ───────────────────────────────────────
  searchTerm    = '';
  filterStatus: 'all' | 'low' | 'out' = 'all';

  private destroy$ = new Subject<void>();

  constructor(
    private svc: ProductService,
    private cd:  ChangeDetectorRef
  ) {}

  // ── Lifecycle ─────────────────────────────────────────────

  ngOnInit(): void { this.loadData(); }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Data ──────────────────────────────────────────────────

  loadData(): void {
    this.loading = true;
    this.cd.markForCheck();

    this.svc.getProductsWithDetails(false)
      .pipe(
        finalize(() => { this.loading = false; this.cd.markForCheck(); }),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.rows = this.mapToRows(res.data);
          }
        },
        error: () => this.cd.markForCheck()
      });
  }

  /** Flatten products → variants, keep only low/out-of-stock */
  private mapToRows(products: Product[]): ReorderRow[] {
    const rows: ReorderRow[] = [];

    products.forEach(p => {
      (p.variants ?? []).forEach(v => {
        const stock   = v.quantityInStock ?? 0;
        const reorder = v.reorderLevel    ?? 0;

        let status: 'low' | 'out' | null = null;
        if (stock === 0)           status = 'out';
        else if (stock <= reorder) status = 'low';

        if (!status) return;   // skip items with healthy stock

        rows.push({
          productId:    p.productId,
          variantId:    v.variantId,
          productName:  p.name,
          supplierName: p.supplierName || '—',
          categoryName: p.categoryName || '—',
          size:         v.size          || '—',
          classType:    v.classType     || '—',
          unitOfMeasure:v.unitOfMeasure || '—',
          color:        v.color         || '',
          currentStock: stock,
          reorderLevel: reorder,
          stockStatus:  status,
          selected:     false,
          orderQty:     Math.max(1, reorder - stock),   // smart default
          notes:        ''
        });
      });
    });

    // Sort: out-of-stock first, then by product name
    return rows.sort((a, b) => {
      if (a.stockStatus !== b.stockStatus)
        return a.stockStatus === 'out' ? -1 : 1;
      return a.productName.localeCompare(b.productName);
    });
  }

  // ── Filtered view ─────────────────────────────────────────

  get filteredRows(): ReorderRow[] {
    let list = this.rows;

    if (this.filterStatus !== 'all')
      list = list.filter(r => r.stockStatus === this.filterStatus);

    const t = this.searchTerm.toLowerCase().trim();
    if (t)
      list = list.filter(r =>
        r.productName.toLowerCase().includes(t)  ||
        r.supplierName.toLowerCase().includes(t) ||
        r.size.toLowerCase().includes(t)         ||
        r.classType.toLowerCase().includes(t)
      );

    return list;
  }

  // ── Selection helpers ─────────────────────────────────────

  get selectedRows(): ReorderRow[] {
    return this.rows.filter(r => r.selected && r.orderQty > 0);
  }

  get allVisibleSelected(): boolean {
    const vis = this.filteredRows;
    return vis.length > 0 && vis.every(r => r.selected);
  }

  toggleAll(checked: boolean): void {
    this.filteredRows.forEach(r => r.selected = checked);
    this.cd.markForCheck();
  }

  toggleRow(row: ReorderRow, checked: boolean): void {
    row.selected = checked;
    this.cd.markForCheck();
  }

  onSearchChange(val: string): void { this.searchTerm = val; this.cd.markForCheck(); }
  clearSearch():  void { this.searchTerm = ''; this.cd.markForCheck(); }
  setFilter(f: 'all' | 'low' | 'out'): void { this.filterStatus = f; this.cd.markForCheck(); }

  // ── KPIs ─────────────────────────────────────────────────

  get totalLow():  number { return this.rows.filter(r => r.stockStatus === 'low').length; }
  get totalOut():  number { return this.rows.filter(r => r.stockStatus === 'out').length; }
  get totalItems():number { return this.rows.length; }
  get selectedCount(): number { return this.selectedRows.length; }

  // ── PDF Generation ────────────────────────────────────────

  /**
   * Opens a new browser window with a styled demand slip and
   * triggers window.print().  No external library needed.
   */
  generatePDF(): void {
    const items = this.selectedRows;
    if (!items.length) return;

    // Group by supplier for a cleaner bill
    const bySupplier = new Map<string, ReorderRow[]>();
    items.forEach(r => {
      const key = r.supplierName;
      if (!bySupplier.has(key)) bySupplier.set(key, []);
      bySupplier.get(key)!.push(r);
    });

    const dateStr = this.today.toLocaleDateString('en-GB', {
      day: '2-digit', month: 'long', year: 'numeric'
    });

    const slipNo = `RO-${Date.now().toString().slice(-6)}`;

    // Build supplier sections
    let supplierSections = '';
    bySupplier.forEach((rows, supplier) => {
      const tableRows = rows.map((r, i) => `
        <tr>
          <td class="tc">${i + 1}</td>
          <td><strong>${r.productName}</strong></td>
          <td class="tc">${r.size}</td>
          <td class="tc">${r.classType !== '—' ? r.classType : '—'}</td>
          <td class="tc">${r.unitOfMeasure}</td>
          <td class="tc bold">${r.orderQty}</td>
          <td>${r.notes || '—'}</td>
        </tr>`).join('');

      supplierSections += `
        <div class="supplier-block">
          <div class="supplier-label">
            <span class="sup-tag">Supplier</span>
            <span class="sup-name">${supplier}</span>
          </div>
          <table>
            <thead>
              <tr>
                <th class="tc">#</th>
                <th>Product Name</th>
                <th class="tc">Size</th>
                <th class="tc">Class</th>
                <th class="tc">Unit</th>
                <th class="tc">Qty Required</th>
                <th>Notes</th>
              </tr>
            </thead>
            <tbody>${tableRows}</tbody>
          </table>
        </div>`;
    });

    const html = `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8"/>
  <title>Reorder Demand Slip — ${slipNo}</title>
  <style>
    @import url('https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;600;700&display=swap');

    *  { box-sizing: border-box; margin: 0; padding: 0; }

    body {
      font-family: 'DM Sans', sans-serif;
      background: #fff;
      color: #0f172a;
      padding: 2.5rem 2.8rem;
      font-size: 13px;
    }

    /* ── Header ── */
    .header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      padding-bottom: 1.25rem;
      border-bottom: 2px solid #1e3a5f;
      margin-bottom: 1.5rem;
    }

    .store-name {
      font-size: 22px;
      font-weight: 700;
      color: #1e3a5f;
      letter-spacing: -0.03em;
    }

    .store-sub {
      font-size: 12px;
      color: #64748b;
      margin-top: 3px;
    }

    .slip-info { text-align: right; }

    .slip-badge {
      display: inline-block;
      background: #1e3a5f;
      color: #fff;
      font-size: 11px;
      font-weight: 700;
      padding: 4px 12px;
      border-radius: 20px;
      letter-spacing: 0.05em;
      margin-bottom: 6px;
    }

    .slip-meta {
      font-size: 11.5px;
      color: #475569;
      line-height: 1.7;
    }

    .slip-meta strong { color: #0f172a; }

    /* ── Section title ── */
    .section-title {
      font-size: 11px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.07em;
      color: #64748b;
      margin-bottom: 1rem;
    }

    /* ── Supplier block ── */
    .supplier-block {
      margin-bottom: 1.75rem;
    }

    .supplier-label {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      margin-bottom: 0.5rem;
    }

    .sup-tag {
      font-size: 10px;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.06em;
      background: #eff6ff;
      color: #1d4ed8;
      padding: 2px 8px;
      border-radius: 4px;
    }

    .sup-name {
      font-size: 14px;
      font-weight: 700;
      color: #1e3a5f;
    }

    /* ── Table ── */
    table {
      width: 100%;
      border-collapse: collapse;
      font-size: 12.5px;
    }

    thead tr { background: #1e3a5f; }

    th {
      padding: 8px 10px;
      text-align: left;
      font-size: 10.5px;
      font-weight: 600;
      color: rgba(255,255,255,0.9);
      letter-spacing: 0.05em;
      text-transform: uppercase;
      border: none;
    }

    td {
      padding: 8px 10px;
      border-bottom: 1px solid #f1f5f9;
      vertical-align: middle;
    }

    tbody tr:nth-child(even) td { background: #f8fafc; }
    tbody tr:last-child td { border-bottom: none; }

    .tc   { text-align: center; }
    .bold { font-weight: 700; color: #1e3a5f; font-size: 13px; }

    /* ── Summary row ── */
    .summary {
      display: flex;
      justify-content: space-between;
      align-items: center;
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 10px 14px;
      margin-bottom: 1.75rem;
      font-size: 12.5px;
    }

    .summary-item { text-align: center; }
    .summary-label { font-size: 10px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.05em; color: #94a3b8; display: block; }
    .summary-val   { font-size: 16px; font-weight: 700; color: #1e3a5f; }

    /* ── Signature area ── */
    .signatures {
      display: flex;
      justify-content: space-between;
      margin-top: 2.5rem;
      padding-top: 1rem;
      border-top: 1px dashed #e2e8f0;
    }

    .sig-box { text-align: center; width: 28%; }
    .sig-line { height: 1px; background: #0f172a; margin-bottom: 6px; }
    .sig-label { font-size: 11px; color: #64748b; font-weight: 500; }

    /* ── Footer ── */
    .footer {
      margin-top: 2rem;
      padding-top: 0.75rem;
      border-top: 1px solid #e2e8f0;
      text-align: center;
      font-size: 10.5px;
      color: #94a3b8;
    }

    @media print {
      body { padding: 1.5rem; }
      @page { margin: 1cm; }
    }
  </style>
</head>
<body>

  <!-- Header -->
  <div class="header">
    <div>
      <div class="store-name">${this.storeName}</div>
      <div class="store-sub">Stock Reorder Demand Slip</div>
    </div>
    <div class="slip-info">
      <div class="slip-badge">REORDER DEMAND</div>
      <div class="slip-meta">
        <strong>Slip No:</strong> ${slipNo}<br/>
        <strong>Date:</strong> ${dateStr}<br/>
        <strong>Prepared by:</strong> Store Manager
      </div>
    </div>
  </div>

  <!-- Summary bar -->
  <div class="summary">
    <div class="summary-item">
      <span class="summary-label">Total Items</span>
      <span class="summary-val">${items.length}</span>
    </div>
    <div class="summary-item">
      <span class="summary-label">Suppliers</span>
      <span class="summary-val">${bySupplier.size}</span>
    </div>
    <div class="summary-item">
      <span class="summary-label">Total Units Required</span>
      <span class="summary-val">${items.reduce((s, r) => s + r.orderQty, 0)}</span>
    </div>
    <div class="summary-item">
      <span class="summary-label">Generated On</span>
      <span class="summary-val" style="font-size:12px">${dateStr}</span>
    </div>
  </div>

  <!-- Per-supplier tables -->
  <div class="section-title">Demand Details : </div>
  ${supplierSections}

  <!-- Signature area -->
  <div class="signatures">
    <div class="sig-box">
      <div class="sig-line"></div>
      <div class="sig-label">Prepared by</div>
    </div>
    <div class="sig-box">
      <div class="sig-line"></div>
      <div class="sig-label">Approved by</div>
    </div>
    <div class="sig-box">
      <div class="sig-line"></div>
      <div class="sig-label">Received by (Supplier)</div>
    </div>
  </div>

  <!-- Footer -->
  <div class="footer">
    ${this.storeName} &nbsp;·&nbsp; Reorder Slip ${slipNo} &nbsp;·&nbsp; ${dateStr} &nbsp;·&nbsp; Generated by DevInfantary POS
  </div>

  <script>window.onload = () => window.print();</script>
</body>
</html>`;

    const win = window.open('', '_blank');
    if (win) {
      win.document.write(html);
      win.document.close();
    }
  }
}