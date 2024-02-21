This is the Angular project that will display inside the Outlook add-in.

As of Feb 2024, this project is unlikely to build without adding the following enviornment variable:

```
$env:NODE_OPTIONS = "--openssl-legacy-provider"
```

Efforts to resolve this with a newer openssl provider package is reccomended.