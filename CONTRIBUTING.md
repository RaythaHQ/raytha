# Contributing to Raytha

We welcome contributions to Raytha! Here are some guidelines to help you get started.

## Code of Conduct

We have a [Code of Conduct](https://github.com/RaythaHQ/raytha/blob/main/CODE_OF_CONDUCT.md) that all contributors are expected to follow. Please make sure you are familiar with its contents before contributing.

## Asking Questions

If you have a question about how to use Raytha, ask for help in the [GitHub discussions board](https://github.com/RaythaHQ/raytha/discussions).

## Reporting Bugs

If you think you have found a bug in Raytha, please create a new issue in the [GitHub issue tracker](https://github.com/RaythaHQ/raytha/issues). When creating a bug report, please include as much of the following information as possible:

- A description of the problem
- Steps to reproduce the problem
- The expected behavior
- The actual behavior
- Any relevant error messages
- The version of Raytha you are using
- Screenshots if possible

## Suggesting Features

We are always open to suggestions for new features or improvements to existing features. If you have an idea for a new feature, please post in the [GitHub discussions board](https://github.com/RaythaHQ/raytha/discussions) in the Ideas category.

## Contributing Code

We welcome code contributions to Raytha! Please note that any and all contributions you make are allowed under MIT open source license. If you want to contribute code, please follow these steps:

1. Fork the `dev` repository on GitHub.
2. Create a new branch for your changes.
3. Make your changes.
4. Commit your changes and push them to your fork.
5. Create a new pull request on GitHub to the `dev` repository.

We will review your code and give feedback as soon as possible. Code accepted into the `dev` branch is typically slated for the next major release.

## Raytha Branching Strategy

- `main` is the current production branch and should be kept in a clean state at all times.
- `release-X.X.X` is typically the next release branch. It is frozen in the sense that no new features will be added, but bug fixes and security upgrades that are intended for the next release will be applied to this branch.
- `dev` is the branch where new features are developed. For example, a Unit Tests branch may be created from dev, and a REST API branch may also be created from dev. When each of these features is complete, they are merged back into dev. When we have enough new features for the target milestone, a new release-X.X.X branch is created from dev. When we are ready to release, we merge release-X.X.X into main and tag the merge as a new release. After the release, dev should rebase on top of main and continue development.

## Thanks!

Thank you for contributing to Raytha! We appreciate your help and look forward to reviewing your code.
