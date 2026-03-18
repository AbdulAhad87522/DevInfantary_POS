/**
 * lookup.component.ts — FULLY FIXED
 * ─────────────────────────────────────────────────────────────
 * ROOT CAUSE OF BLANK ROWS (from your screenshot):
 *   KPIs showed 9 records but rows were empty.
 *   The API was returning camelCase JSON:
 *     { lookupId, value, isActive, ... }
 *   But the HTML template was reading PascalCase:
 *     c.Value, c.IsActive  ← all undefined → blank
 *
 * FIX — normalise() method:
 *   After receiving data, every record is normalised so BOTH
 *   camelCase and PascalCase properties exist on every object.
 *   The template can safely use either. No more blank rows.
 * ─────────────────────────────────────────────────────────────
 */

import {
  Component, OnInit, OnDestroy,
  ChangeDetectionStrategy, ChangeDetectorRef
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormsModule, ReactiveFormsModule,
  FormBuilder, FormGroup, Validators
} from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, takeUntil, finalize } from 'rxjs';
import { LookupService, Category, CategoryDto, CategoryUpdateDto } from '../Services/lookup.service';

type ModalMode = 'view' | 'add' | 'edit' | 'delete' | null;

@Component({
  selector: 'app-lookup',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './lookup.component.html',
  styleUrls: ['./lookup.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LookupComponent implements OnInit, OnDestroy {

  // ── State ─────────────────────────────────────────────────────
  categoryList: Category[] = [];
  filtered:     Category[] = [];
  selected:     Category | null = null;

  loading  = false;
  saving   = false;
  includeInactive = false;
  searchTerm = '';
  modalMode: ModalMode = null;
  toast: { msg: string; type: 'success' | 'error' } | null = null;
  private toastTimer: ReturnType<typeof setTimeout> | null = null;

  addForm!:  FormGroup;
  editForm!: FormGroup;

  private search$  = new Subject<string>();
  private destroy$ = new Subject<void>();

  constructor(
    private svc: LookupService,
    private fb: FormBuilder,
    private cd: ChangeDetectorRef
  ) {}

  // ── Lifecycle ─────────────────────────────────────────────────

  ngOnInit(): void {
    this.buildForms();
    this.loadAll();

    this.search$.pipe(
      debounceTime(350),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(term =>
      term.trim() ? this.runSearch(term) : this.applyLocalFilter()
    );
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    if (this.toastTimer) clearTimeout(this.toastTimer);
  }

  // ── CORE FIX 1: extractArray ──────────────────────────────────
  /**
   * The API may return:
   *   a) A plain array:           [{ lookupId:1, value:'X' }, ...]
   *   b) A paginated object:      { data: [...] }  or  { Data: [...] }
   *   c) A single object:         { lookupId:1, value:'X' }  (getById)
   *
   * This always returns a safe Category[].
   */
  private extractArray(raw: any): Category[] {
    console.log('[LookupComponent] Raw API response:', raw);

    if (!raw) return [];

    // Case (a): plain array
    if (Array.isArray(raw)) return raw;

    // Case (b): paginated wrapper — try all common property names
    for (const key of ['data', 'Data', 'items', 'Items', 'result', 'Result']) {
      if (Array.isArray(raw[key])) return raw[key];
    }

    // Case (c): single object — wrap in array
    if (typeof raw === 'object') return [raw];

    console.error('[LookupComponent] Cannot extract array from:', raw);
    return [];
  }

  // ── CORE FIX 2: normalise ─────────────────────────────────────
  /**
   * .NET may send camelCase OR PascalCase depending on Program.cs config.
   * This copies every property under both casings so the template
   * never gets undefined regardless of what the API sends.
   *
   * Example input:  { lookupId: 1, value: 'Power Tools', isActive: false }
   * Example output: { lookupId: 1, LookupId: 1, value: 'Power Tools',
   *                   Value: 'Power Tools', isActive: false, IsActive: false, ... }
   */
  private normalise(raw: any): Category {
    const id    = raw.lookupId    ?? raw.LookupId    ?? 0;
    const val   = raw.value       ?? raw.Value       ?? '';
    const desc  = raw.description ?? raw.Description ?? null;
    const order = raw.displayOrder?? raw.DisplayOrder?? null;
    const active= raw.isActive    ?? raw.IsActive    ?? false;
    const type  = raw.type        ?? raw.Type        ?? 'category';
    const cAt   = raw.createdAt   ?? raw.CreatedAt   ?? null;

    return {
      // camelCase
      lookupId: id, value: val, description: desc,
      displayOrder: order, isActive: active, type, createdAt: cAt,
      // PascalCase — identical values
      LookupId: id, Value: val, Description: desc,
      DisplayOrder: order, IsActive: active, Type: type, CreatedAt: cAt
    };
  }

  // ── Data loading ──────────────────────────────────────────────

  loadAll(): void {
    this.loading = true;
    this.cd.markForCheck();

    this.svc.getAll(this.includeInactive)
      .pipe(finalize(() => { this.loading = false; this.cd.markForCheck(); }))
      .subscribe({
        next: (raw: any) => {
          // Extract array then normalise every record
          this.categoryList = this.extractArray(raw).map(r => this.normalise(r));
          console.log('[LookupComponent] Normalised list:', this.categoryList);
          this.applyLocalFilter();
        },
        error: (err: Error) => this.showToast(err.message, 'error')
      });
  }

  applyLocalFilter(): void {
    const list = Array.isArray(this.categoryList) ? this.categoryList : [];
    const term = this.searchTerm.toLowerCase().trim();
    this.filtered = term
      ? list.filter(c =>
          (c.Value ?? '').toLowerCase().includes(term) ||
          (c.Description ?? '').toLowerCase().includes(term)
        )
      : [...list];
    this.cd.markForCheck();
  }

  runSearch(term: string): void {
    this.loading = true;
    this.cd.markForCheck();

    this.svc.search(term, this.includeInactive)
      .pipe(finalize(() => { this.loading = false; this.cd.markForCheck(); }))
      .subscribe({
        next: (raw: any) => {
          this.filtered = this.extractArray(raw).map(r => this.normalise(r));
          this.cd.markForCheck();
        },
        error: (err: Error) => this.showToast(err.message, 'error')
      });
  }

  onSearchChange(val: string): void { this.searchTerm = val; this.search$.next(val); }
  clearSearch(): void { this.searchTerm = ''; this.applyLocalFilter(); }
  toggleInactive(): void { this.includeInactive = !this.includeInactive; this.loadAll(); }

  // ── KPI getters (safe — never crash) ─────────────────────────

  get totalCategories():    number { return this.categoryList.length; }
  get activeCategories():   number { return this.categoryList.filter(c => c.IsActive || c.isActive).length; }
  get inactiveCategories(): number { return this.categoryList.filter(c => !(c.IsActive ?? c.isActive)).length; }
  get maxOrder(): number {
    return this.categoryList.reduce((mx, c) =>
      Math.max(mx, c.DisplayOrder ?? c.displayOrder ?? 0), 0);
  }

  // ── Modal openers ─────────────────────────────────────────────

  openAdd(): void {
    this.addForm.reset({ DisplayOrder: this.maxOrder + 1 });
    this.modalMode = 'add';
  }

  openView(c: Category): void   { this.selected = c; this.modalMode = 'view'; }

  openEdit(c: Category): void {
    this.selected = c;
    this.editForm.patchValue({
      Value:        c.Value ?? c.value,
      Description:  c.Description ?? c.description,
      DisplayOrder: c.DisplayOrder ?? c.displayOrder
    });
    this.modalMode = 'edit';
  }

  openDelete(c: Category): void { this.selected = c; this.modalMode = 'delete'; }
  closeModal(): void { this.modalMode = null; this.selected = null; }

  // ── CRUD ──────────────────────────────────────────────────────

  submitAdd(): void {
    if (this.addForm.invalid) { this.addForm.markAllAsTouched(); return; }
    this.saving = true; this.cd.markForCheck();

    const r = this.addForm.getRawValue();
    const dto: CategoryDto = {
      Value:        r.Value,
      Description:  r.Description  || undefined,
      DisplayOrder: r.DisplayOrder  ? Number(r.DisplayOrder) : undefined
    };

    this.svc.create(dto)
      .pipe(finalize(() => { this.saving = false; this.cd.markForCheck(); }))
      .subscribe({
        next: () => { this.closeModal(); this.showToast('Category created!', 'success'); this.loadAll(); },
        error: (err: Error) => this.showToast(err.message, 'error')
      });
  }

  submitEdit(): void {
    if (this.editForm.invalid || !this.selected) { this.editForm.markAllAsTouched(); return; }
    this.saving = true; this.cd.markForCheck();

    const r   = this.editForm.getRawValue();
    const id  = this.selected.LookupId ?? this.selected.lookupId ?? 0;
    const dto: CategoryUpdateDto = {
      Value:        r.Value,
      Description:  r.Description  || undefined,
      DisplayOrder: r.DisplayOrder  ? Number(r.DisplayOrder) : undefined
    };

    this.svc.update(id, dto)
      .pipe(finalize(() => { this.saving = false; this.cd.markForCheck(); }))
      .subscribe({
        next: (ok: any) => {
          if (ok !== false) {
            this.closeModal(); this.showToast('Category updated!', 'success'); this.loadAll();
          } else {
            this.showToast('Update failed — not found or inactive.', 'error');
          }
        },
        error: (err: Error) => this.showToast(err.message, 'error')
      });
  }

  confirmDelete(): void {
    if (!this.selected) return;
    this.saving = true; this.cd.markForCheck();
    const id = this.selected.LookupId ?? this.selected.lookupId ?? 0;

    this.svc.delete(id)
      .pipe(finalize(() => { this.saving = false; this.cd.markForCheck(); }))
      .subscribe({
        next: () => { this.closeModal(); this.showToast('Category deactivated.', 'success'); this.loadAll(); },
        error: (err: Error) => this.showToast(err.message, 'error')
      });
  }

  restore(c: Category, event: Event): void {
    event.stopPropagation();
    const id = c.LookupId ?? c.lookupId ?? 0;
    this.svc.restore(id).subscribe({
      next: () => { this.showToast(`"${c.Value ?? c.value}" restored.`, 'success'); this.loadAll(); },
      error: (err: Error) => this.showToast(err.message, 'error')
    });
  }

  // ── Helpers ───────────────────────────────────────────────────

  /** Returns display value — works for both casing styles */
  display(c: Category, field: 'value' | 'description' | 'displayOrder' | 'createdAt'): any {
    switch (field) {
      case 'value':        return c.Value        ?? c.value        ?? '—';
      case 'description':  return c.Description  ?? c.description  ?? null;
      case 'displayOrder': return c.DisplayOrder ?? c.displayOrder ?? null;
      case 'createdAt':    return c.CreatedAt    ?? c.createdAt    ?? null;
    }
  }

  isActive(c: Category): boolean {
    return c.IsActive ?? c.isActive ?? false;
  }

  getId(c: Category): number {
    return c.LookupId ?? c.lookupId ?? 0;
  }

  fmtDate(d: string | null | undefined): string {
    if (!d) return '—';
    try { return new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }); }
    catch { return d; }
  }

  getColor(value: string): string {
    const colors = ['#2a577a','#0f6e56','#854f0b','#7c3aed','#b45309','#ea580c','#be185d','#1d4ed8'];
    let h = 0;
    for (let i = 0; i < value.length; i++) h = value.charCodeAt(i) + ((h << 5) - h);
    return colors[Math.abs(h) % colors.length];
  }

  showToast(msg: string, type: 'success' | 'error'): void {
    this.toast = { msg, type }; this.cd.markForCheck();
    if (this.toastTimer) clearTimeout(this.toastTimer);
    this.toastTimer = setTimeout(() => { this.toast = null; this.cd.markForCheck(); }, 4000);
  }

  err(form: FormGroup, field: string): string {
    const c = form.get(field);
    if (!c?.touched || !c.errors) return '';
    if (c.errors['required'])  return 'Required.';
    if (c.errors['minlength']) return `Min ${c.errors['minlength'].requiredLength} characters.`;
    if (c.errors['min'])       return 'Must be ≥ 0.';
    return '';
  }

  private buildForms(): void {
    this.addForm = this.fb.group({
      Value:        ['', [Validators.required, Validators.minLength(2)]],
      Description:  [''],
      DisplayOrder: [null, [Validators.min(0)]]
    });
    this.editForm = this.fb.group({
      Value:        ['', [Validators.required, Validators.minLength(2)]],
      Description:  [''],
      DisplayOrder: [null, [Validators.min(0)]]
    });
  }
}