import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CustomerBillsComponent } from './customer-bills.component';

describe('CustomerBillsComponent', () => {
  let component: CustomerBillsComponent;
  let fixture: ComponentFixture<CustomerBillsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CustomerBillsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CustomerBillsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
