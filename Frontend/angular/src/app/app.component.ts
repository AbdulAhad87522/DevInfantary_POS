import { Component } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { SlideBarComponent } from './slide-bar/slide-bar.component';
import { CommonModule } from '@angular/common';
import { AuthService } from './services/auth.service';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, SlideBarComponent, CommonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  title = 'angular';
  isSidebarOpen = false;       // Mobile ke liye
  isSidebarCollapsed = false;  // ✅ Desktop collapse ke liye
  showSidebar = false;

  constructor(private authService: AuthService, private router: Router) {
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: any) => {
      const isLoginPage = event.urlAfterRedirects === '/login';

      if (isLoginPage) {
        this.showSidebar = false;
        this.isSidebarCollapsed = false; // ✅ Logout par reset
      } else {
        this.showSidebar = this.authService.isAuthenticated();
        if (!this.showSidebar) {
          this.router.navigate(['/login']);
        } else {
          this.isSidebarCollapsed = false; // ✅ Login ke baad hamesha expanded
        }
      }
    });
  }

  toggleSidebar() {
    this.isSidebarOpen = !this.isSidebarOpen; // Mobile
  }

  // ✅ Desktop sidebar collapse/expand
  toggleCollapse() {
    this.isSidebarCollapsed = !this.isSidebarCollapsed;
  }
}