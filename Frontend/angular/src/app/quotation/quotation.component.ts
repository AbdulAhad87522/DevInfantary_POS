import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { QuotationService, Quotation, QuotationItem } from '../services/quotation.service';

@Component({
  selector: 'app-quotation',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './quotation.component.html',
  styleUrls: ['./quotation.component.css']
})
export class QuotationComponent implements OnInit {

  currentDate = new Date().toLocaleDateString('en-US', {
    year: 'numeric', month: 'long', day: 'numeric'
  });

  searchTerm: string = '';
  customerType: string = 'regular';
  paidAmount: number = 0;

  // API se aane wala data
  quotations: Quotation[] = [];
  selectedQuotation: Quotation | null = null;
  displayItems: QuotationItem[] = [];

  // States
  isLoading: boolean = false;
  errorMessage: string = '';

  constructor(private quotationService: QuotationService) {}

  ngOnInit(): void {
    this.loadQuotations();
  }

  loadQuotations(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.quotationService.getAllQuotations().subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.quotations = response.data;
          if (this.quotations.length > 0) {
            this.selectQuotation(this.quotations[0]);
          }
        } else {
          this.errorMessage = response.message || 'Data load nahi hua';
        }
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error:', err);
        this.errorMessage = 'Server se connect nahi ho pa raha!';
        this.isLoading = false;
      }
    });
  }

  selectQuotation(q: Quotation): void {
    this.selectedQuotation = q;
    this.displayItems = q.items;
    this.paidAmount = q.totalAmount;
  }

  // ✅ SIRF YAHI CHANGE HAI - API se quotation number search
  onSearch(): void {
    if (!this.searchTerm.trim()) {
      // Search clear hone pe original items wapas dikhao
      if (this.selectedQuotation) {
        this.displayItems = this.selectedQuotation.items;
      }
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.quotationService.getQuotationByNumber(this.searchTerm.trim()).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.selectQuotation(response.data);
        } else {
          this.errorMessage = 'Quotation nahi mili: ' + this.searchTerm;
          this.selectedQuotation = null;
          this.displayItems = [];
        }
        this.isLoading = false;
      },
      error: (err) => {
        if (err.status === 404) {
          this.errorMessage = `"${this.searchTerm}" yeh quotation number exist nahi karta!`;
        } else {
          this.errorMessage = 'Server error! Dobara try karo.';
        }
        this.selectedQuotation = null;
        this.displayItems = [];
        this.isLoading = false;
      }
    });
  }

  onCustomerTypeChange(type: string): void {
    this.customerType = type;
  }

  get totalPrice(): number {
    return this.selectedQuotation?.subtotal ?? 0;
  }

  get totalDiscount(): number {
    return this.selectedQuotation?.discountAmount ?? 0;
  }

  get finalPrice(): number {
    return this.selectedQuotation?.totalAmount ?? 0;
  }

  get filteredProducts(): QuotationItem[] {
    return this.displayItems;
  }

  onPrint(): void {
    window.print();
  }

  onProcessPayment(): void {
    console.log('Processing payment:', this.paidAmount);
  }
}