# Test Plan: Task Manager API

| | |
|---|---|
| **Project** | Task Manager API (ASP.NET Core Web API, .NET 10, EF Core, SQLite) |
| **Author** | Sara Bajrić |
| **Version** | 1.0 |
| **Status** | Unit and API tests implemented and passing (17/17). UI tests planned. |

## 1. Purpose

This document describes what is tested in the Task Manager API, how it is tested, and what counts as a pass. The goal is to confirm that the CRUD functionality for tasks behaves as specified, including invalid input and missing data.

## 2. Scope

**In scope**

- The REST endpoints under `/api/tasks` (create, read, update, delete)
- Input validation (empty title)
- HTTP status codes and response format (JSON, enum values as text)
- Data persistence (changes are really stored in the database)

**Out of scope (for now)**

- Authentication and authorization (not implemented in the API)
- Performance and load testing
- Browser UI tests (planned as a separate phase, see section 9)

## 3. Test levels

| Level | Tool | What it checks | Count |
|---|---|---|---|
| Unit | xUnit, SQLite in-memory | `TasksController` methods in isolation | 8 |
| API (integration) | xUnit, `WebApplicationFactory` | Real HTTP requests against the whole app, temporary database | 9 |
| UI | Playwright (planned) | Key user scenarios in the browser | planned |

## 4. Test environment

- .NET SDK 10, xUnit
- Unit tests use an in-memory SQLite database, created fresh for every test.
- API tests start the real application in memory with a temporary SQLite file in the OS temp folder. A guard check fails the test run if the temporary database is not in use, so the real `taskmanager.db` can never be touched.
- Tests are independent: each test creates its own data and does not rely on test order.

## 5. API test cases

| ID | Endpoint | Scenario | Expected result |
|---|---|---|---|
| API-01 | GET /api/tasks | No tasks exist | 200 OK, empty list |
| API-02 | POST /api/tasks | Valid task (title + description) | 201 Created, `Location` header set, `id` > 0, status `ToDo` |
| API-03 | POST /api/tasks | Check JSON of the response | `status` is the text `"ToDo"`, not a number (regression test) |
| API-04 | POST /api/tasks | Title is only whitespace | 400 Bad Request |
| API-05 | GET /api/tasks/{id} | Task exists | 200 OK, same task that was created |
| API-06 | GET /api/tasks/{id} | Task does not exist (id 999) | 404 Not Found |
| API-07 | PUT /api/tasks/{id} | Existing task, new title and status | 204 No Content, a following GET shows the new values |
| API-08 | DELETE /api/tasks/{id} | Existing task | 204 No Content, a following GET returns 404 |
| API-09 | DELETE /api/tasks/{id} | Task does not exist (id 999) | 404 Not Found |

## 6. Unit test cases

| ID | Method | Scenario | Expected result |
|---|---|---|---|
| UT-01 | GetAll | No tasks exist | OK, empty list |
| UT-02 | Create | Valid title | CreatedAtAction, task saved with `ToDo` status |
| UT-03 | Create | Empty title | BadRequest, nothing saved |
| UT-04 | GetById | Task does not exist | NotFound |
| UT-05 | Update | Existing task | NoContent, fields changed in the database |
| UT-06 | Update | Task does not exist | NotFound |
| UT-07 | Delete | Existing task | NoContent, task removed |
| UT-08 | Delete | Task does not exist | NotFound |

## 7. Pass / fail criteria

- A test passes when the actual result matches the expected result in the tables above.
- The build is considered healthy when **all** tests pass (`failed: 0`).
- Any failing test must be fixed or analysed before new features are merged.

## 8. How to run

```powershell
dotnet test
```

Latest result: `total: 17; failed: 0; succeeded: 17; skipped: 0`.

## 9. Planned next steps

- UI tests with Playwright for a few key scenarios (create and complete a task)
- Run the whole test suite automatically on every push (CI pipeline)
- Additional negative tests: title longer than 200 characters, invalid status value, malformed JSON
- Consider DTO classes so the API accepts only the fields a client is allowed to send

## 10. Known limitations and risks

- The API does not validate the maximum title length itself; the limit (200 characters) is enforced only by the database configuration. An additional negative test is planned for this.
- Tests run against SQLite. The production environment will use Azure SQL, so database-specific behaviour will be re-checked after deployment.
