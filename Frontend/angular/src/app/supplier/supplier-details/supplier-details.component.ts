import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from "@angular/router";
import { SupplierService, Supplier } from '../../services/supplier.service';

@Component({
  selector: 'app-supplier-details',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './supplier-details.component.html',
  styleUrl: './supplier-details.component.css'
})
export class SupplierDetailsComponent implements OnInit {
  
  suppliers: Supplier[] = [];
  filteredSuppliers: Supplier[] = [];
  
  // KPIs
  totalSuppliers = 0;
  activeSuppliers = 0;

  // Search
  searchTerm: string = '';
  
  // Loading states
  isLoading = false;
  errorMessage = '';
  includeInactive = false;

  // UI flags
  showViewDetails = false;
  showEditForm = false;
  selectedSupplier: Supplier | null = null;
  editingSupplier: any = {};

  constructor(private supplierService: SupplierService) {}

  ngOnInit(): void {
    this.loadSuppliers();
  }

  loadSuppliers(): void {
    this.isLoading = true;
    this.errorMessage = '';
    
    this.supplierService.getAllSuppliers(this.includeInactive).subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success && response.data) {
          this.suppliers = response.data;
          this.filteredSuppliers = response.data;
          this.updateKPIs();
          console.log('Suppliers loaded:', this.suppliers);
        } else {
          this.errorMessage = response.message || 'Failed to load suppliers';
        }
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = 'Error connecting to server. Please try again.';
        console.error('Error loading suppliers:', error);
      }
    });
  }

  updateKPIs(): void {
    this.totalSuppliers = this.suppliers.length;
    this.activeSuppliers = this.suppliers.filter(s => s.isActive).length;
  }

  onSearch() {
    if (!this.searchTerm.trim()) {
      this.filteredSuppliers = this.suppliers;
      return;
    }
    
    const term = this.searchTerm.toLowerCase();
    this.filteredSuppliers = this.suppliers.filter(supplier =>
      supplier.name.toLowerCase().includes(term) ||
      (supplier.contact && supplier.contact.toLowerCase().includes(term)) ||
      (supplier.address && supplier.address.toLowerCase().includes(term))
    );
  }

  onClearSearch() {
    this.searchTerm = '';
    this.filteredSuppliers = this.suppliers;
  }

  toggleIncludeInactive(): void {
    this.loadSuppliers();
  }

  // Top button actions
  onAdd() {
    console.log('Add clicked');
  }

  onEdit() {
    console.log('Edit clicked');
    if (this.filteredSuppliers.length > 0) {
      this.openEditSupplier(this.filteredSuppliers[0]);
    }
  }

  onDelete() {
    console.log('Delete clicked');
    if (this.filteredSuppliers.length > 0) {
      this.deleteSupplier(this.filteredSuppliers[0]);
    }
  }

  // Row actions
  onEditRow(supplier: Supplier) {
    console.log('Edit row for:', supplier);
    this.openEditSupplier(supplier);
  }

  onDeleteRow(supplier: Supplier) {
    console.log('Delete row for:', supplier);
    this.deleteSupplier(supplier);
  }

  viewSupplier(supplier: Supplier) {
    console.log('View supplier:', supplier);
    this.selectedSupplier = supplier;
    this.showViewDetails = true;
  }

  closeViewDetails() {
    this.showViewDetails = false;
    this.selectedSupplier = null;
  }

  openEditSupplier(supplier: any) {
    this.editingSupplier = { ...supplier };
    this.showEditForm = true;
  }

  closeEditForm() {
    this.showEditForm = false;
    this.editingSupplier = {};
  }

  onSubmitEdit(form: any) {
    if (form.invalid) {
      Object.keys(form.controls).forEach(key => form.controls[key].markAsTouched());
      return;
    }

    this.isLoading = true;
    
    this.supplierService.updateSupplier(this.editingSupplier.supplierId, this.editingSupplier).subscribe({
      next: (response) => {
        this.isLoading = false;
        if (response.success) {
          console.log('Supplier updated:', response.data);
          this.loadSuppliers();
          this.closeEditForm();
        } else {
          this.errorMessage = response.message || 'Failed to update supplier';
        }
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = 'Error updating supplier. Please try again.';
        console.error('Error updating supplier:', error);
      }
    });
  }

  deleteSupplier(supplier: Supplier) {
    if (confirm(`Delete ${supplier.name}?`)) {
      this.isLoading = true;
      
      this.supplierService.deleteSupplier(supplier.supplierId).subscribe({
        next: (response) => {
          this.isLoading = false;
          if (response.success) {
            console.log('Supplier deleted:', supplier.supplierId);
            this.loadSuppliers();
          } else {
            this.errorMessage = response.message || 'Failed to delete supplier';
          }
        },
        error: (error) => {
          this.isLoading = false;
          this.errorMessage = 'Error deleting supplier. Please try again.';
          console.error('Error deleting supplier:', error);
        }
      });
    }
  }

  restoreSupplier(supplier: Supplier) {
    if (confirm(`Restore ${supplier.name}?`)) {
      this.isLoading = true;
      
      this.supplierService.restoreSupplier(supplier.supplierId).subscribe({
        next: (response) => {
          this.isLoading = false;
          if (response.success) {
            console.log('Supplier restored:', supplier.supplierId);
            this.loadSuppliers();
          } else {
            this.errorMessage = response.message || 'Failed to restore supplier';
          }
        },
        error: (error) => {
          this.isLoading = false;
          this.errorMessage = 'Error restoring supplier. Please try again.';
          console.error('Error restoring supplier:', error);
        }
      });
    }
  }

  refresh() {
    this.loadSuppliers();
  }
}