module.exports = {
  preset: "jest-preset-angular",
  setupFiles: ["jest-localstorage-mock"],
  setupFilesAfterEnv: ["<rootDir>/projects/app-reporting/src/jestSetup.ts"],
  transformIgnorePatterns: [
    "node_modules/(?!msal|@azure/msal-angular|adal-angular4|ngx-cookie-service)"
  ],
  testPathIgnorePatterns: [
    "<rootDir>/projects/app-reporting/src/environments/"
  ],

  // unit test
  reporters: ["default", ["jest-junit", {
    outputDirectory: "../results/jest-unittest",
    outputName: `jest-reporting-${Date.now()}.xml`
  }]],

  // coverage
  coveragePathIgnorePatterns: ["jestGlobalMocks.ts", "jestSetup.ts"],
  coverageReporters: ['cobertura', 'lcov'],
  coverageDirectory: "../results/coverage"
};
