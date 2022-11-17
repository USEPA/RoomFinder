import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../shared/services/auth-service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit {
  deployedVersion: string;

  constructor(
    public authService: AuthService) { }

  ngOnInit() {
  }

  signIn() {
    this.authService.signIn();
  }

  isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
  }

  userDisplayName() {
    return this.authService.userDisplayName();
  }
}
