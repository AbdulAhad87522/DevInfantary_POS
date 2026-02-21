import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ReturnItemsComponent } from './return-items.component';

describe('ReturnItemsComponent', () => {
  let component: ReturnItemsComponent;
  let fixture: ComponentFixture<ReturnItemsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReturnItemsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ReturnItemsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
