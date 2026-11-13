# GitHub connection and repository workflow

The native Uno/.NET app exposes GitHub sign-in, repository selection/manual resolution, clone-as-project, and guarded commit/push in its Coding Agent workspace. It requires a real GitHub App Client ID configured by the distributor or entered in Settings → GitHub; the app owner must enable Device Flow and grant the minimum required repository permissions. Windows stores credentials with DPAPI; Linux uses Secret Service and requires an available desktop keyring plus `secret-tool`.

Skai Desktop connects directly to GitHub from the desktop app. The GitHub access and refresh tokens are kept in the operating system credential store. Repository browsing, cloning, and pushing happen only when the user asks for them.

## Configure the app once

The application publisher needs to register a GitHub App, enable **Device flow**, and configure **Metadata: read-only** plus **Contents: read and write**. Do not request account-wide repository administration. GitHub's device authorization grant is intended for desktop clients; device flow must be enabled in the app settings. The desktop client uses the GitHub App's **Client ID** (not its App ID or client secret); IDs may contain punctuation such as the `Iv1.` prefix.

For development builds, enter the GitHub App public Client ID in the native Settings → GitHub page or provide `SERVER_ROCK_GITHUB_APP_CLIENT_ID` at process launch. The client ID is not a secret; no GitHub private key or client secret belongs in this desktop app. Users must install the GitHub App on the account or organization and grant it access to the repositories they want to use.

The app requests offline_access so expiring GitHub App user tokens can be refreshed. The client ID must belong to a GitHub App with Device flow enabled.

## Use it

1. Open Settings → GitHub, enter the app client ID if the build has not preconfigured it, and choose Connect.
2. Approve the app on GitHub using the one-time device code.
3. Open GitHub repositories, choose a repository, and choose its local parent folder. Skai Desktop clones it and adds it as a Coding Agent project.
4. Ask the Coding Agent to make changes. Use Push to owner/repository or `/push <commit message>` to open a review of the current branch and changed file list, then explicitly commit and push after checking the paths and commit message. The reviewed status and contents are fingerprinted; if the repository changes before confirmation, the push is rejected and the review must be reopened.

The push action stages all changes Git sees in that repository, including untracked files not excluded by `.gitignore`. The review dialog shows changed paths, not a line-by-line diff. Review file contents and exclude local-only data before confirming. Commits are made locally before network push; if push fails, the local commit remains and the current branch can be retried after checking its status.

The flow follows GitHub's device-flow polling interval, expiry, `slow_down`, and optional `offline_access` refresh-token requirements. GitHub App user tokens use the app's fine-grained permissions and the user's own repository access; credentials are stored in OS-backed encrypted storage and are not written to ordinary settings. See GitHub's official documentation:

- https://docs.github.com/en/apps/creating-github-apps/authenticating-with-a-github-app/generating-a-user-access-token-for-a-github-app
- https://docs.github.com/en/apps/creating-github-apps/registering-a-github-app/registering-a-github-app
- https://docs.github.com/en/apps/creating-github-apps/registering-a-github-app/choosing-permissions-for-a-github-app
