import { Component } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, CommonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  title = 'PG_Frontend';

  constructor(public authService: AuthService, private router: Router) { }

  isLoginPage(): boolean {
    return this.router.url === '/login' || this.router.url === '/';
  }

  logout() {
    this.authService.logout();
  }
}
