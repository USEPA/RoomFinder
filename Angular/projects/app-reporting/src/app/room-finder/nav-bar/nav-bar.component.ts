import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../shared/services/auth-service';
import { faHome, faSignInAlt } from '@fortawesome/free-solid-svg-icons';

@Component({
  selector: 'app-nav-bar',
  templateUrl: './nav-bar.component.html',
  styleUrls: ['./nav-bar.component.scss']
})
export class NavBarComponent implements OnInit {
  // Should the collapsed nav show?
  showNav: boolean;
  faHomeIcon = faHome;
  faSignInIcon = faSignInAlt;
  constructor(public authService: AuthService) { }

  ngOnInit() {
    this.showNav = false;
  }

  // Used by the Bootstrap navbar-toggler button to hide/show
  // the nav in a collapsed state
  toggleNavBar(): void {
    this.showNav = !this.showNav;
  }

  signIn() {
    this.authService.signIn();
  }

  signOut(): void {
    this.authService.signOut();
  }

  isAuthenticated(): boolean {
    return (this.authService.isAuthenticated());
  }

  userDisplayName() {
    return this.authService.userDisplayName();
  }

  userEmail() {
    return this.authService.userEmail();
  }

  navigateToAddin() {
    window.open(this.authService.getOutlookEndpoint(), '_blank');
  }
}
