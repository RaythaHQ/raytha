# Application Layer Reference

The Application Layer Reference documentation covers all of the functionality that is available in the source code's Raytha.Application project. This is effectively the project that handles all of the business logic, read/write actions between the Web and WebApi layer and the database.

The code is well organized and follows the popular [CleanArchitecture pattern](https://github.com/jasontaylordev/CleanArchitecture) (CQRS).

Each item in this sidebar covers an aspect of the Raytha platform such as `users`, `admins`, `templates`, `content` and more. In CQRS, an action can be either a Query (read only) or a Command (write) and they are organized as such in this documentation.

Raytha uses [Mediatr](https://github.com/jbogard/MediatR) to send commands and queries.