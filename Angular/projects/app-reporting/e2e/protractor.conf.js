const { SpecReporter } = require('jasmine-spec-reporter');
const { JUnitXmlReporter } = require('jasmine-reporters');

module.exports = {
  allScriptsTimeout: 20000,
  specs: [],
  capabilities: {
    'browserName': 'chrome',
    'chromeOptions': {
      'args': ['incognito']
    },
    'max-duration': 180
  },
  directConnect: true,
  framework: 'jasmine',
  jasmineNodeOpts: {
    showColors: true,
    defaultTimeoutInterval: 30000,
    print: function () { }
  },
  params: {
    resultsFilePrefix: 'TESTS-E2E',
    testAccountPassword: '',
    baseUrl: 'http://localhost:4201/',
  },
  plugins: [{
    path: './helpers/protractor-console-plugin',
    logLevels: ['severe']
  },
  {
    package: 'protractor-screenshoter-plugin',
    screenshotPath: '../results/e2e/reporting-screenshots',
    screenshotOnExpect: 'failure',
    screenshotOnSpec: 'none',
    withLogs: true,
    writeReportFreq: 'asap',
    imageToAscii: 'none',
    clearFoldersBeforeTest: true
  }
  ],
  onPrepare() {
    require('ts-node').register({
      project: './projects/app-reporting/e2e/tsconfig.e2e.json'
    });
    jasmine.getEnv().addReporter(new SpecReporter({ spec: { displayStacktrace: true } }));
    jasmine.getEnv().addReporter(new JUnitXmlReporter({ savePath: '../results/e2e/reporting', filePrefix: browser.params.resultsFilePrefix }));
  }
};
