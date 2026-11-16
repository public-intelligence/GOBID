# GOB-ID web application

Angular 20 application that hosts the GOB-ID sign-in pages used by every connected application, and the administration console for organizations, applications (including machine-to-machine credentials), users, roles, permissions, policies, sessions and staff.

See the [repository README](../README.md) for the overall architecture.

## Pages

- **Sign-in flow:** `/app/:application-id/login`, `forgot-password`, `change-password` and `register`. Applications send their users to the login page, and GOB-ID returns them to the application's registered `redirect_url` with an authorization code.
- **Administration console:** the rest of the routes. They are protected by the permissions in the user's token.

## Stack

Angular 20 (standalone components), Tailwind CSS 4, [`@public-intelligence/ui-core`](https://github.com/public-intelligence/UICORE), Karma and Jasmine.

## Run

`@public-intelligence/ui-core` is installed from GitHub Packages. Add the registry to your user `~/.npmrc`, with a GitHub token that has `read:packages`:

```
@public-intelligence:registry=https://npm.pkg.github.com
//npm.pkg.github.com/:_authToken=${NPM_TOKEN}
```

```bash
npm install
npm start          # http://localhost:4200
npm test           # Karma and Jasmine (only Angular's default spec for now)
npm run build      # production build in dist/
```

## Configuration

`src/environments/` holds one file per environment (`environment.ts`, `environment.development.ts` and `environment.production.ts`) with:

- `apiUrl`: base URL of the GOB-ID API;
- `appUrl`: public URL of this web application;
- `applicationId`: the GOB-ID application id this console signs in with.
