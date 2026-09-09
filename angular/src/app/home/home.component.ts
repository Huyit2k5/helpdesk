import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { AuthService, LocalizationPipe, PermissionService } from '@abp/ng.core';
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
  private permissionService = inject(PermissionService);
  private router = inject(Router);

  get hasLoggedIn(): boolean {
    return this.authService.isAuthenticated;
  }

  ngOnInit(): void {
    if (this.hasLoggedIn) {
      if (this.permissionService.getGrantedPolicy('Helpdesk.Dashboard')) {
        this.router.navigate(['/dashboard']);
      } else {
        this.router.navigate(['/portal']);
      }
    }
  }

  login() {
    this.authService.navigateToLogin();
  }
}
