import { browser, by, element } from 'protractor';

export class AppPage {
  navigateTo() {
    return browser.get(browser.baseUrl) as Promise<any>;
  }

  getAddinText() {
    return element(by.id('sideload-msg')).getText() as Promise<string>;
  }
}
