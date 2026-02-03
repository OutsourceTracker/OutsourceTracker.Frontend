# OutsourceManager.Frontend

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)](https://github.com/OutsourceTracker/OutsourceManager.Backend/actions) <!-- Update badge link if you have CI -->

Backend API for **OutsourceTracker** – a platform for managing outsourced development teams, tasks, invoices, and performance tracking.

This project provides RESTful endpoints for authentication, project management, time tracking, invoicing, and reporting.

## Features

- **JWT-based Authentication & Authorization** (with role-based access: Admin, Manager, Freelancer/Client)
- **Project & Task Management** CRUD operations
- **Time Tracking & Work Logs**
- **Invoice Generation & Payment Status Tracking**
- **Reporting** (e.g., hours per project, freelancer performance)
- **Integration with Common Library** (OutsourceTracker.Common)
- **Swagger/OpenAPI Documentation** (built-in)
- **Health Checks & Metrics**
- **CORS & Rate Limiting** configured for frontend consumption

## Tech Stack

- **Framework**: ASP.NET Core 8.0 (Web API)
- **Language**: C# 12
- **ORM**: Entity Framework Core (with PostgreSQL/SQL Server support)
- **Auth**: JWT Bearer + Identity (or custom user store)
- **Serialization**: System.Text.Json
- **Logging**: Serilog (structured logging to console/file)
- **Validation**: FluentValidation
- **API Documentation**: Swashbuckle.AspNetCore
- **Testing**: xUnit + Moq + Testcontainers (optional)
- **Containerization**: Docker support

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- PostgreSQL 15+ or SQL Server (local or Docker)
- (Optional) Docker & Docker Compose for local dev

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/OutsourceTracker/OutsourceManager.Frontend.git
   cd OutsourceManager.Frontend
   ```