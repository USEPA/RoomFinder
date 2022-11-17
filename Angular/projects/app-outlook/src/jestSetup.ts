import 'jest-preset-angular';
import './jestGlobalMocks';
import { ObjectMocks } from './app/testing/addin-mock-objects';
import { environment } from './environments/environment';

// mock AppConfig for testing service calls
new ObjectMocks().getAppConfig();

// workaround for replacing environment.ts with environment.test.ts
environment.disableAnimation = true;
environment.snackbarDuration = 1000;

// see https://github.com/telerik/kendo-angular/issues/1505
Object.defineProperty(window, 'getComputedStyle', {
  value: () => ({
    getPropertyValue: (prop: any) => {
      return prop;
    }
  })
});

// see https://github.com/mlaursen/react-md/issues/783
Object.defineProperty(window, 'matchMedia', {
  value: () => {
    return {
      matches: false,
      addListener: () => { },
      removeListener: () => { }
    };
  }
});
