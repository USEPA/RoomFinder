import { Component, OnInit } from '@angular/core';
import { AppConfigService } from '../../shared/services/app.config.service';

@Component({
  selector: 'app-nav-footer',
  templateUrl: './nav-footer.component.html',
  styleUrls: ['./nav-footer.component.scss']
})
export class NavFooterComponent implements OnInit {
  // Should the collapsed nav show?
  showNav: boolean;
  deployedVersion: string;

  constructor(
    public configService: AppConfigService) { }

  ngOnInit() {
    this.showNav = false;
    this.deployedVersion = this.configService.getVersion();
  }

  // Used by the Bootstrap navbar-toggler button to hide/show
  // the nav in a collapsed state
  toggleNavBar(): void {
    this.showNav = !this.showNav;
  }
}
