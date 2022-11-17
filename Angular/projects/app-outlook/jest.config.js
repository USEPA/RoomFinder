module.exports = {
  preset: "jest-preset-angular",
  setupFiles: ["jest-localstorage-mock"],
  setupFilesAfterEnv: ["<rootDir>/projects/app-outlook/src/jestSetup.ts"],
  transformIgnorePatterns: [
    "node_modules/(?!msal|@azure/msal-angular|adal-angular4|ngx-cookie-service)"
  ],
  testPathIgnorePatterns: [
    "<rootDir>/projects/app-outlook/src/environments/"
  ],

  // unit test
  reporters: ["default", ["jest-junit", {
    outputDirectory: "../results/jest-unittest",
    outputName: `jest-outlook-${Date.now()}.xml`
  }]],

  // coverage
  coveragePathIgnorePatterns: ["jestGlobalMocks.ts", "jestSetup.ts"],
  coverageReporters: ['cobertura', 'lcov'],
  coverageDirectory: "../results/coverage"
};
