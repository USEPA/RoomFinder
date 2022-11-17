
import { enableProdMode } from '@angular/core';
import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';
import { environment } from './environments/environment';
import { AppModule } from './app/app.module';

if (environment.production) {
  enableProdMode();
}

Office.onReady(hostinfo => {
  if (hostinfo.host === Office.HostType.Outlook) {
    document.getElementById('sideload-msg').style.display = 'none';
    platformBrowserDynamic()
      .bootstrapModule(AppModule)
      .catch(err => console.error(err));
  }
});
