import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DailyExpensesComponent } from './daily-expenses.component';

describe('DailyExpensesComponent', () => {
  let component: DailyExpensesComponent;
  let fixture: ComponentFixture<DailyExpensesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DailyExpensesComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DailyExpensesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
