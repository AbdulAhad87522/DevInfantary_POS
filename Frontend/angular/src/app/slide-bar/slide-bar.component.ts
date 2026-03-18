import { Component, AfterViewInit, ElementRef, ViewChild, Input, Output, EventEmitter, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLinkActive, RouterLink } from '@angular/router';
import { gsap } from 'gsap';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-slide-bar',
  standalone: true,
  imports: [CommonModule, RouterLinkActive, RouterLink],
  templateUrl: './slide-bar.component.html',
  styleUrls: ['./slide-bar.component.css']
})
export class SlideBarComponent implements AfterViewInit,OnInit {
  authService:AuthService=inject(AuthService);
  // ✅ Parent se collapsed state receive karo
  @Input() isCollapsed = false;

  // ✅ Parent ko toggle signal bhejo
  @Output() toggleCollapse = new EventEmitter<void>();

  isMobileMenuOpen = false;
  @ViewChild('sidebar') sidebarRef!: ElementRef;
  @ViewChild('menuItems') menuItemsRef!: ElementRef;

  onToggleCollapse() {
    this.toggleCollapse.emit();
  }

  toggleMobileMenu() {
    this.isMobileMenuOpen = !this.isMobileMenuOpen;

    if (this.isMobileMenuOpen) {
      gsap.to(this.sidebarRef.nativeElement, {
        x: 0, duration: 0.5, ease: 'power3.out'
      });
      gsap.fromTo(
        this.menuItemsRef.nativeElement.querySelectorAll('li'),
        { opacity: 0, x: -30 },
        { opacity: 1, x: 0, duration: 0.6, stagger: 0.07, ease: 'power2.out', delay: 0.2 }
      );
    } else {
      gsap.to(this.sidebarRef.nativeElement, {
        x: '-100%', duration: 0.4, ease: 'power3.in'
      });
    }
  }

  ngAfterViewInit() {
    if (window.innerWidth <= 768) {
      gsap.set(this.sidebarRef.nativeElement, { x: '-100%' });
    }
  }
  // ============================================================
// Add this property and method to your sidebar component class
// ============================================================

// Settings dropdown open/close state
isSettingsOpen: boolean = false;

// Toggle settings dropdown
toggleSettings(): void {
  // If sidebar is collapsed, first expand it then open dropdown
  if (this.isCollapsed) {
    this.onToggleCollapse();
    // Small delay so sidebar animates open before dropdown appears
    setTimeout(() => {
      this.isSettingsOpen = !this.isSettingsOpen;
    }, 320);
  } else {
    this.isSettingsOpen = !this.isSettingsOpen;
  }
}
 logout() {
    console.log('Logout clicked');
    // Navigate to login page (adjust the path as needed)
    this.authService.logout();
  }

  currentUser:any

  ngOnInit(){
   this.currentUser = this.authService.getCurrentUser();
  }

  hasRole(role: string): boolean {
  return this.currentUser?.role === role;
}
}