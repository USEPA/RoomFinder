const baseConfig = require('./protractor.conf');
baseConfig.specs = [
  './src/app.e2e-spec.ts'
];
baseConfig.params.resultsFilePrefix = 'TESTS-E2E-RoomFinder';
exports.config = baseConfig;
