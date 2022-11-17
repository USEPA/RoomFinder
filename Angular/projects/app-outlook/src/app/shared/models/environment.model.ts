export interface IEnvironment {
  name: string;
  production: boolean;
  snackbarDuration?: number;
  disableAnimation?: boolean;
  baseApiUrl?: string;
}
