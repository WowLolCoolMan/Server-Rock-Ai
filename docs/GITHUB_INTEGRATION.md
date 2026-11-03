# GitHub connection and repository workflow

Server Rock: AI connects directly to GitHub from the desktop app. The GitHub access and refresh tokens are kept in the operating system credential store. Repository browsing, cloning, and pushing happen only when the user asks for them.

## Configure the app once

The application publisher needs to register a GitHub App and enable **Device flow**. Give the app **Metadata: read-only** and **Contents: read and write** permissions. Do not configure account-wide repository administration permissions. Users choose which repositories to install or authorize for the app.

For development builds, enter the GitHub App public Client ID in Settings → GitHub. A distributor can preconfigure it in electron/github-app.json or with SERVER_ROCK_GITHUB_APP_CLIENT_ID. The client ID is not a secret; no GitHub private key or client secret belongs in this desktop app.

The app requests offline_access so expiring GitHub App user tokens can be refreshed. The client ID must belong to a GitHub App with Device flow enabled.

## Use it

1. Open Provider settings, enter the app client ID if the build has not preconfigured it, and choose Connect.
2. Approve the app on GitHub using the one-time device code.
3. Open GitHub repositories, choose a repository, and choose its local parent folder. Server Rock clones it and adds it as a Coding Agent project.
4. Ask the Coding Agent to make changes. Use Push to owner/repository or /push <commit message> to commit workspace changes and push the current branch.

The push action stages all changes Git sees in that repository, including untracked files not excluded by .gitignore. Review the repository before pushing if it contains local-only data.

GitHub's official documentation recommends GitHub Apps for fine-grained permissions, describes device flow for desktop applications, and documents short-lived user access and refresh tokens:

- https://docs.github.com/en/apps/creating-github-apps/authenticating-with-a-github-app/generating-a-user-access-token-for-a-github-app
- https://docs.github.com/en/apps/maintaining-github-apps/modifying-a-github-app-registration
