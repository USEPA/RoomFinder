import { Injectable, Inject } from '@angular/core';
import { ReplaySubject, Observable, forkJoin } from 'rxjs';
import { DOCUMENT } from '@angular/common';

@Injectable({
  providedIn: 'root'
})
export class OfficeAddInService {
  private loadedLibraries: { [url: string]: ReplaySubject<any> } = {};

  constructor(@Inject(DOCUMENT) private readonly document: any) { }

  lazyLoadOfficeAddIn(): Observable<any> {
    return forkJoin([
      this.loadScript('https://appsforoffice.microsoft.com/lib/1.1/hosted/office.js')
    ]);
  }

  lazyLoadOfficeFabric(): Observable<any> {
    return forkJoin([
      this.loadStyle('https://static2.sharepointonline.com/files/fabric/office-ui-fabric-core/9.6.1/css/fabric.min.css'),
      this.loadStyle('https://static2.sharepointonline.com/files/fabric/office-ui-fabric-js/1.4.0/css/fabric.components.min.css'),
      this.loadScript('https://static2.sharepointonline.com/files/fabric/office-ui-fabric-js/1.4.0/js/fabric.min.js')
    ]);
  }

  private loadScript(url: string): Observable<any> {
    if (this.loadedLibraries[url]) {
      return this.loadedLibraries[url].asObservable();
    }

    this.loadedLibraries[url] = new ReplaySubject();

    const script = this.document.createElement('script');
    script.type = 'text/javascript';
    script.async = true;
    script.src = url;
    script.onload = () => {
      this.loadedLibraries[url].next();
      this.loadedLibraries[url].complete();
    };

    this.document.body.appendChild(script);

    return this.loadedLibraries[url].asObservable();
  }

  private loadStyle(url: string): Observable<any> {
    if (this.loadedLibraries[url]) {
      return this.loadedLibraries[url].asObservable();
    }

    this.loadedLibraries[url] = new ReplaySubject();

    const style = this.document.createElement('link');
    style.type = 'text/css';
    style.href = url;
    style.rel = 'stylesheet';
    style.onload = () => {
      this.loadedLibraries[url].next();
      this.loadedLibraries[url].complete();
    };

    const head = document.getElementsByTagName('head')[0];
    head.appendChild(style);

    return this.loadedLibraries[url].asObservable();
  }
}
