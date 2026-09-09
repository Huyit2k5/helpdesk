import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { AuthService, LocalizationPipe } from '@abp/ng.core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
  changeDetection: ChangeDetectionStrategy.Eager,
  imports: [LocalizationPipe]
})
export class HomeComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);

  get hasLoggedIn(): boolean {
    return this.authService.isAuthenticated;
  }

  ngOnInit(): void {
    if (this.hasLoggedIn) {
      this.router.navigate(['/dashboard']);
    }
  }

  login() {
    this.authService.navigateToLogin();
  }
}
