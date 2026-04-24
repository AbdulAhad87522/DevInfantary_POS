/**
 * ============================================================
 * staff.component.ts
 * ============================================================
 * Standalone Angular 18 component — full Staff CRUD.
 *
 * FIXES in this version:
 *  - All property access is camelCase (s.name, s.isActive, s.staffId)
 *  - staffList initialised as [] (never undefined) with Array.isArray guard
 *  - Safe fallback in every getter so filter() never throws
 *  - Form control names are camelCase to match DTOs
 * ============================================================
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
import {
  Subject, debounceTime, distinctUntilChanged,
  takeUntil, finalize
} from 'rxjs';
import {
  StaffService,
  Staff,
  StaffDto,
  StaffUpdateDto,
  StaffChangePasswordDto
} from '../services/staff.service';

/** Which modal panel is currently open */
type ModalMode = 'view' | 'add' | 'edit' | 'delete' | 'password' | null;

@Component({
  selector: 'app-staff',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './staff.component.html',
  styleUrls: ['./staff.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StaffComponent implements OnInit, OnDestroy {

  // ── Data ─────────────────────────────────────────────────────
  /** Master list received from API — ALWAYS an array (never undefined) */
  staffList: Staff[] = [];

  /** Filtered slice shown in the table */
  filtered: Staff[] = [];

  /** Currently selected record (used by modals) */
  selected: Staff | null = null;

  // ── UI flags ─────────────────────────────────────────────────
  loading         = false;  // true while any HTTP GET is running
  saving          = false;  // true while a mutation (POST/PUT/DELETE) is running
  includeInactive = false;  // toggle to show soft-deleted records
  searchTerm      = '';

  /** Controls which modal is visible */
  modalMode: ModalMode = null;

  /** Toast notification; null = hidden */
  toast: { msg: string; type: 'success' | 'error' } | null = null;
  private toastTimer: ReturnType<typeof setTimeout> | null = null;

  // ── Reactive forms ───────────────────────────────────────────
  addForm!:  FormGroup;   // POST /api/staff
  editForm!: FormGroup;   // PUT  /api/staff/:id
  pwForm!:   FormGroup;   // PATCH /api/staff/:id/change-password

  /** Roles must match lookup table values in your database */
  readonly roles = ['Admin', 'Manager', 'Cashier', 'Inventory', 'Sales'];

  // ── RxJS helpers ─────────────────────────────────────────────
  private search$  = new Subject<string>();   // debounced search stream
  private destroy$ = new Subject<void>();     // triggers unsubscribe on destroy

  constructor(
    private svc: StaffService,
    private fb:  FormBuilder,
    private cd:  ChangeDetectorRef
  ) {}

  // ── Lifecycle ─────────────────────────────────────────────────

  ngOnInit(): void {
    this.buildForms();
    this.loadAll();   // initial data fetch

    // Wire up debounced search
    this.search$.pipe(
      debounceTime(350),          // wait 350ms after last keystroke
      distinctUntilChanged(),     // skip if value unchanged
      takeUntil(this.destroy$)    // auto-unsubscribe on destroy
    ).subscribe(term => {
      if (term.trim().length >= 1) {
        this.runSearch(term);    // call server
      } else {
        this.applyLocalFilter(); // empty → show all from staffList
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    if (this.toastTimer) clearTimeout(this.toastTimer);
  }

  // ── Data loading ──────────────────────────────────────────────

  /**
   * GET /api/staff
   * The service unwraps ApiResponse<Staff[]> and returns Staff[].
   * We guard with Array.isArray just in case.
   */
  loadAll(): void {
    this.loading = true;
    this.cd.markForCheck();

    this.svc.getAll(this.includeInactive)
      .pipe(
        finalize(() => {
          this.loading = false;
          this.cd.markForCheck();
        })
      )
      .subscribe({
        next: (data: Staff[]) => {
          // Safety guard — ensure we always store a plain array
          this.staffList = Array.isArray(data) ? data : [];

          console.log(
            `[StaffComponent] Loaded ${this.staffList.length} records.`,
            this.staffList[0] ?? 'no records'
          );

          this.applyLocalFilter();
        },
        error: (err: Error) => {
          this.staffList = [];
          this.filtered  = [];
          this.showToast(err.message, 'error');
        }
      });
  }

  /**
   * Client-side filter applied instantly as the user types.
   * Server search kicks in after 350ms debounce.
   */
  applyLocalFilter(): void {
    // Guard: if staffList is somehow not an array, reset it
    if (!Array.isArray(this.staffList)) {
      this.staffList = [];
    }

    const term = this.searchTerm.toLowerCase().trim();

    this.filtered = term
      ? this.staffList.filter(s =>
          s.name.toLowerCase().includes(term)          ||
          (s.email    ?? '').toLowerCase().includes(term) ||
          (s.contact  ?? '').toLowerCase().includes(term) ||
          (s.username ?? '').toLowerCase().includes(term) ||
          (s.roleName ?? '').toLowerCase().includes(term)
        )
      : [...this.staffList];

    this.cd.markForCheck();
  }

  /** Server-side search via GET /api/staff/search */
  runSearch(term: string): void {
    this.loading = true;
    this.cd.markForCheck();

    this.svc.search(term, this.includeInactive)
      .pipe(
        finalize(() => {
          this.loading = false;
          this.cd.markForCheck();
        })
      )
      .subscribe({
        next: (data: Staff[]) => {
          this.filtered = Array.isArray(data) ? data : [];
          this.cd.markForCheck();
        },
        error: (err: Error) => this.showToast(err.message, 'error')
      });
  }

  /** Called on every keystroke in the search input */
  onSearchChange(val: string): void {
    this.searchTerm = val;
    this.search$.next(val);
  }

  /** Clear button next to the search input */
  clearSearch(): void {
    this.searchTerm = '';
    this.applyLocalFilter();
  }

  /** Show/hide inactive (soft-deleted) staff */
  toggleInactive(): void {
    this.includeInactive = !this.includeInactive;
    this.loadAll();
  }

  // ── KPI computed properties ───────────────────────────────────
  // Each getter guards against staffList being empty/not-array.

  get totalStaff():    number {
    return Array.isArray(this.staffList) ? this.staffList.length : 0;
  }

  get activeStaff():   number {
    return Array.isArray(this.staffList)
      ? this.staffList.filter(s => s.isActive).length
      : 0;
  }

  get inactiveStaff(): number {
    return Array.isArray(this.staffList)
      ? this.staffList.filter(s => !s.isActive).length
      : 0;
  }

  get adminCount():    number {
    return Array.isArray(this.staffList)
      ? this.staffList.filter(
          s => s.roleName?.toLowerCase() === 'admin' && s.isActive
        ).length
      : 0;
  }

  // ── Modal openers ─────────────────────────────────────────────

  openAdd(): void {
    this.addForm.reset({ roleName: 'Cashier' });
    this.modalMode = 'add';
  }

  openView(s: Staff): void {
    this.selected  = s;
    this.modalMode = 'view';
  }

  openEdit(s: Staff): void {
    this.selected = s;
    // hireDate from API: "2024-01-15T00:00:00" → slice to "2024-01-15" for <input type="date">
    this.editForm.patchValue({
      name:     s.name,
      email:    s.email,
      contact:  s.contact,
      cnic:     s.cnic,
      address:  s.address,
      roleName: s.roleName,
      username: s.username,
      hireDate: s.hireDate ? s.hireDate.substring(0, 10) : null
    });
    this.modalMode = 'edit';
  }

  openDelete(s: Staff): void {
    this.selected  = s;
    this.modalMode = 'delete';
  }

  openPassword(s: Staff): void {
    this.selected  = s;
    this.pwForm.reset();
    this.modalMode = 'password';
  }

  closeModal(): void {
    this.modalMode = null;
    this.selected  = null;
  }

  // ── CRUD operations ───────────────────────────────────────────

  /** POST /api/staff — create new staff member */
  submitAdd(): void {
    if (this.addForm.invalid) { this.addForm.markAllAsTouched(); return; }

    this.saving = true;
    this.cd.markForCheck();

    const raw = this.addForm.getRawValue();
    const dto: StaffDto = {
      name:     raw.name,
      email:    raw.email    || undefined,
      contact:  raw.contact  || undefined,
      cnic:     raw.cnic     || undefined,
      address:  raw.address  || undefined,
      roleName: raw.roleName,
      username: raw.username,
      password: raw.password,
      hireDate: raw.hireDate || undefined
    };

    this.svc.create(dto)
      .pipe(finalize(() => { this.saving = false; this.cd.markForCheck(); }))
      .subscribe({
        next: () => {
          this.closeModal();
          this.showToast('Staff member added successfully!', 'success');
          this.loadAll();
        },
        error: (err: Error) => this.showToast(err.message, 'error')
      });
  }

  /** PUT /api/staff/:id — update existing staff member */
  submitEdit(): void {
    if (this.editForm.invalid || !this.selected) {
      this.editForm.markAllAsTouched(); return;
    }

    this.saving = true;
    this.cd.markForCheck();

    const raw = this.editForm.getRawValue();
    const dto: StaffUpdateDto = {
      name:     raw.name,
      email:    raw.email    || undefined,
      contact:  raw.contact  || undefined,
      cnic:     raw.cnic     || undefined,
      address:  raw.address  || undefined,
      roleName: raw.roleName,
      username: raw.username || undefined,
      hireDate: raw.hireDate || undefined
    };

    this.svc.update(this.selected.staffId, dto)
      .pipe(finalize(() => { this.saving = false; this.cd.markForCheck(); }))
      .subscribe({
        next: (ok: boolean) => {
          if (ok) {
            this.closeModal();
            this.showToast('Staff updated successfully!', 'success');
            this.loadAll();
          } else {
            this.showToast('Update failed — staff not found or inactive.', 'error');
          }
        },
        error: (err: Error) => this.showToast(err.message, 'error')
      });
  }

  /** DELETE /api/staff/:id — soft delete */
  confirmDelete(): void {
    if (!this.selected) return;

    this.saving = true;
    this.cd.markForCheck();

    this.svc.delete(this.selected.staffId)
      .pipe(finalize(() => { this.saving = false; this.cd.markForCheck(); }))
      .subscribe({
        next: () => {
          this.closeModal();
          this.showToast('Staff member deactivated.', 'success');
          this.loadAll();
        },
        error: (err: Error) => this.showToast(err.message, 'error')
      });
  }

  /** PATCH /api/staff/:id/restore — re-activate staff */
  restore(s: Staff): void {
    this.svc.restore(s.staffId).subscribe({
      next: () => {
        this.showToast(`${s.name} restored successfully.`, 'success');
        this.loadAll();
      },
      error: (err: Error) => this.showToast(err.message, 'error')
    });
  }

  /** PATCH /api/staff/:id/change-password */
  submitPassword(): void {
    if (this.pwForm.invalid || !this.selected) {
      this.pwForm.markAllAsTouched(); return;
    }

    this.saving = true;
    this.cd.markForCheck();

    const raw = this.pwForm.getRawValue();
    const dto: StaffChangePasswordDto = {
      currentPassword: raw.currentPassword,
      newPassword:     raw.newPassword
    };

    this.svc.changePassword(this.selected.staffId, dto)
      .pipe(finalize(() => { this.saving = false; this.cd.markForCheck(); }))
      .subscribe({
        next: (ok: boolean) => {
          if (ok) {
            this.closeModal();
            this.showToast('Password changed successfully!', 'success');
          } else {
            // API returns false (not HTTP 4xx) when current password is wrong
            this.showToast('Current password is incorrect.', 'error');
          }
        },
        error: (err: Error) => this.showToast(err.message, 'error')
      });
  }

  // ── Template helpers ──────────────────────────────────────────

  /** Extract up to 2 initials from a display name */
  getInitials(name: string): string {
    if (!name) return '?';
    return name.split(' ')
      .filter(Boolean)
      .map(n => n[0])
      .slice(0, 2)
      .join('')
      .toUpperCase();
  }

  /**
   * Format an ISO date string → "15 Jan 2024".
   * Returns '—' for null/empty values.
   */
  fmtDate(d: string | null | undefined): string {
    if (!d) return '—';
    try {
      return new Date(d).toLocaleDateString('en-GB', {
        day: '2-digit', month: 'short', year: 'numeric'
      });
    } catch {
      return d;
    }
  }

  /**
   * Show a toast notification.
   * Auto-dismisses after 4 seconds.
   */
  showToast(msg: string, type: 'success' | 'error'): void {
    this.toast = { msg, type };
    this.cd.markForCheck();
    if (this.toastTimer) clearTimeout(this.toastTimer);
    this.toastTimer = setTimeout(() => {
      this.toast = null;
      this.cd.markForCheck();
    }, 4000);
  }

  /**
   * Returns the first validation error message for a given field,
   * or empty string when valid / untouched.
   */
  err(form: FormGroup, field: string): string {
    const ctrl = form.get(field);
    if (!ctrl?.touched || !ctrl.errors) return '';
    if (ctrl.errors['required'])  return 'This field is required.';
    if (ctrl.errors['minlength']) return `Min ${ctrl.errors['minlength'].requiredLength} characters.`;
    if (ctrl.errors['email'])     return 'Enter a valid email address.';
    return '';
  }

  // ── Form builders (called once in ngOnInit) ───────────────────

  private buildForms(): void {

    // Add Staff — all camelCase control names
    this.addForm = this.fb.group({
      name:     ['', [Validators.required, Validators.minLength(2)]],
      email:    ['', [Validators.email]],
      contact:  [''],
      cnic:     [''],
      address:  [''],
      roleName: ['Cashier', Validators.required],
      username: ['', [Validators.required, Validators.minLength(3)]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      hireDate: ['']
    });

    // Edit Staff — no password field (use Change Password modal)
    this.editForm = this.fb.group({
      name:     ['', [Validators.required, Validators.minLength(2)]],
      email:    ['', [Validators.email]],
      contact:  [''],
      cnic:     [''],
      address:  [''],
      roleName: ['', Validators.required],
      username: [''],
      hireDate: ['']
    });

    // Change Password
    this.pwForm = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword:     ['', [Validators.required, Validators.minLength(6)]]
    });
  }
}