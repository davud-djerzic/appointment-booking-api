# Appointment Booking API

## Overview

Appointment Booking API is a RESTful backend application built with ASP.NET Core and PostgreSQL for managing appointment scheduling in service-based businesses such as hair salons or barber shops.

The API supports employee and service management, appointment reservations, appointment confirmation, cancellation, automatic expiration of temporary reservations, and automatic completion of appointments.

---

## Features

### Employees

- Create employee
- Update employee
- Activate / deactivate employee
- Search employees
- Pagination

### Services

- Create service
- Update service
- Activate / deactivate service
- Search services

### Employee Service Assignments

- Assign services to employees
- Activate / deactivate assignments

### Appointments

- Create temporary appointment hold
- Confirm appointment
- Cancel appointment
- Complete appointment
- Delete expired holds
- Search appointments
- Pagination
- Filtering

### Background Services

- Automatic cleanup of expired appointment holds
- Automatic completion of scheduled appointments

---

## Technologies

- ASP.NET Core 10
- Dapper
- PostgreSQL
- Docker
- Swagger / OpenAPI
- Npgsql
- Dependency Injection
- Background Services

---

## Architecture

Controller
↓

Service
↓

Repository (Dapper)
↓

PostgreSQL

---

## Project Structure

AppointmentBooking.Api
│
├── Controllers
├── Services
├── Repositories
├── DTOs
├── Models
├── BackgroundServices
├── Configuration
├── Exceptions
└── Extensions

---

## Running locally

### Clone repository

git clone ...

### Start PostgreSQL

docker compose up -d

### Run API

dotnet run

---

## Swagger

https://localhost:xxxx/swagger

---

## Business Rules

- Employees must be active.
- Services must be active.
- Employees can perform only assigned services.
- Temporary appointment holds expire automatically.
- Double booking is prevented using PostgreSQL exclusion constraints.
- Scheduled appointments are automatically completed at the configured time.

---

## Future Improvements

- Authentication (JWT)
- Authorization
- Email notifications
- Availability endpoint
- Frontend (React + TypeScript)
- Integration tests

---

## License

MIT
