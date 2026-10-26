# Appointment Booking API

A RESTful appointment booking API built with ASP.NET Core and PostgreSQL.

The application provides functionality for managing employees, services, employee-service assignments, and appointments. It includes input validation, centralized error handling, pagination, temporary appointment holds, appointment confirmation, Swagger/OpenAPI documentation, Docker support, and unit tests.

## Features

- Employee management
- Service management
- Employee-service assignments
- Appointment booking
- Temporary appointment holds
- Appointment confirmation
- Appointment cancellation
- Appointment completion
- Appointment filtering and pagination
- Request validation
- Centralized exception handling
- ProblemDetails error responses
- Swagger/OpenAPI documentation
- PostgreSQL database
- Docker and Docker Compose support
- Unit tests

## Tech Stack

- .NET 10
- ASP.NET Core Web API
- PostgreSQL 18
- Npgsql
- Dapper
- Docker
- Docker Compose
- OpenAPI
- Swagger UI
- xUnit
- Moq
- FluentAssertions

## Architecture

The application follows a layered architecture:

    Client
       |
       v
    Controller
       |
       v
    Service
       |
       v
    Repository
       |
       v
    PostgreSQL

### Controllers

Handle HTTP requests, validate input, and return appropriate HTTP responses.

### Services

Contain application and business logic and coordinate operations between repositories.

### Repositories

Handle database access and execute SQL queries against PostgreSQL.

### DTOs

Define the request and response models exposed by the API.

### Background Services

Handle automatic appointment processing, including:

- Removing expired appointment holds
- Automatically completing appointments that have ended

## Project Structure

    AppointmentBooking.Api/
    │
    ├── BackgroundServices/
    ├── Configuration/
    ├── Controllers/
    ├── Database/
    ├── DTOs/
    ├── Exceptions/
    ├── Extension/
    ├── Models/
    ├── Repositories/
    ├── Services/
    ├── Dockerfile
    ├── Program.cs
    └── appsettings.json
    │
    └── AppointmentBooking.Api.Tests/

## Getting Started

### Prerequisites

Make sure you have the following installed:

- .NET 10 SDK
- Docker Desktop

### Clone the Repository

    git clone https://github.com/davud-djerzic/appointment-booking-api.git
    cd AppointmentBooking

### Configuration

The project uses environment variables for PostgreSQL configuration.

Create a `.env` file based on the provided example.

Linux/macOS:

    cp .env.example .env

Windows PowerShell:

    Copy-Item .env.example .env

The `.env.example` file contains the required configuration:

    POSTGRES_DB=appointment_booking
    POSTGRES_USER=appointment_user
    POSTGRES_PASSWORD=change_me
    POSTGRES_PORT=5432
    REDIS_PORT=6379
    API_PORT=8080
    JWT_SECRET_KEY=use-a-random-secret-of-at-least-32-characters

The actual `.env` file is intentionally excluded from version control.

## Running with Docker Compose

The easiest way to run the complete application is with Docker Compose.

Start the API, PostgreSQL database, and Redis:

    docker compose up --build

Docker Compose starts:

- PostgreSQL
- Redis
- Appointment Booking API

On first initialization, Docker Compose applies the numbered schema and seed scripts
mounted from `AppointmentBooking.Api/Database/` in order. `014_seed.sql` adds the
configured services and, when an active employee already exists, that employee's working
hours and service links. Existing PostgreSQL volumes are not reinitialized automatically
when a script changes; use a new empty test volume to apply the scripts from scratch.

The API is available at:

    http://localhost:8080

To stop the application:

    docker compose down

To stop the application and remove the PostgreSQL volume:

    docker compose down -v

> Removing the volume also removes the database data.

## Running Locally

To run the API without Docker, make sure PostgreSQL is running and configure the database connection string in the appropriate application configuration.

Restore dependencies:

    dotnet restore

Build the solution:

    dotnet build

Run the API:

    dotnet run --project AppointmentBooking.Api

## Swagger / OpenAPI

When running in the Development environment, the API exposes an OpenAPI document and Swagger UI.

OpenAPI document:

    /openapi/v1.json

Swagger UI:

    /swagger

Swagger UI provides an interactive interface for exploring and testing the API endpoints.

## API Functionality

The API provides endpoints for managing:

### Employees

Employee-related operations include creating, retrieving, updating, activating, deactivating, and listing employees.

### Services

Service-related operations include creating, retrieving, updating, activating, deactivating, and listing bookable services.

### Employee-Service Assignments

Employees can be assigned to services they provide. Assignments can also be activated or deactivated.

### Appointments

Appointment functionality includes:

- Creating temporary appointment holds
- Confirming held appointments
- Cancelling appointments
- Completing appointments
- Retrieving appointments
- Filtering appointments
- Pagination

The exact endpoints and request/response schemas are available through Swagger UI.

## Appointment Lifecycle

Appointments follow a defined lifecycle:

    Held
      |
      +----> Confirmed / Scheduled
      |
      +----> Hold expires
                  |
                  v
               Removed

    Scheduled
        |
        +----> Cancelled
        |
        +----> Completed

A temporary hold prevents the selected appointment slot from being immediately booked by another customer.

If the hold expires before confirmation, Redis expires its hold data automatically. A later hold request also removes expired references from that employee's Redis indexes.

## Appointment Holds

When an appointment hold is created, the API generates a unique hold token and expiration time.

The hold duration is configurable through application settings.

Example configuration:

    {
      "Bookings": {
        "HoldDurationInMinutes": 1,
        "AvailabilitySlotIntervalMinutes": 30
      }
    }

The values can be changed according to the environment.

## Validation and Error Handling

The API uses request validation and centralized exception handling.

Errors are returned using the `ProblemDetails` format.

Examples of handled situations include:

- Invalid request parameters
- Invalid pagination parameters
- Employee not found
- Service not found
- Inactive employee
- Inactive service
- Invalid employee-service assignment
- Expired appointment hold
- Invalid appointment state
- Duplicate resources
- Appointments created in the past
- Appointments booked too far in advance

The API uses meaningful HTTP status codes, including:

    200 OK
    201 Created
    204 No Content
    400 Bad Request
    404 Not Found
    409 Conflict

## Pagination

Listing endpoints support pagination where applicable.

Example:

    GET /appointments?page=1&pageSize=20

The paginated response contains:

    {
      "items": [],
      "page": 1,
      "pageSize": 20,
      "totalCount": 0
    }

Appointment retrieval also supports filtering by relevant appointment properties such as employee, service, status, and date.

## Background Services

The API currently has no background services. Appointment completion is performed manually through the staff workflow. Redis applies the expiration time to temporary booking-hold data.

## Database

The application uses PostgreSQL 18.

Database initialization scripts are located in:

    AppointmentBooking.Api/Database/

Docker Compose uses these scripts to initialize and seed a new database volume.

## Testing

The project contains unit tests using:

- xUnit
- Moq
- FluentAssertions

Run all tests with:

    dotnet test

The tests cover important business scenarios, including:

- Successfully creating an appointment hold
- Preventing appointments from being created in the past
- Handling a non-existent employee
- Preventing confirmation of an expired appointment hold
- Returning paginated appointments

The unit tests isolate the application service layer by mocking repository dependencies.

## Docker

The application includes a multi-stage Dockerfile and Docker Compose configuration.

Docker Compose provides:

    PostgreSQL <--- Appointment Booking API ---> Redis

PostgreSQL and Redis are exposed only on localhost for local tools. The API is exposed on
localhost at port `8080` by default.

Start the complete environment with:

    docker compose up --build

## Configuration

The application uses configuration for appointment-related behavior.

Example:

    {
      "Bookings": {
        "HoldDurationInMinutes": 1,
        "AvailabilitySlotIntervalMinutes": 30
      }
    }

Database credentials are provided through environment variables when using Docker Compose.

Sensitive configuration such as the actual `.env` file should not be committed to source control.

## Future Improvements

Possible future improvements include:

- Authentication and authorization
- Role-based access control
- JWT authentication and refresh tokens
- Integration tests
- Frontend application
- Email notifications
- Appointment availability endpoints
- More advanced scheduling rules
- CI/CD pipeline
- Production deployment

## License

This project was created as a backend development project demonstrating REST API design, business logic, database access, validation, testing, documentation, and containerization.
