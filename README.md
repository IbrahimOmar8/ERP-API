# ✅ ERDTask  System

This project is a ERDTask and Process Tracking System** that allows organizations to define, execute, and monitor multi-step ERDTasks. Developed with **.NET 7 Web API** and an **Angular 18 Admin Dashboard**.

---

## ✨ Features

- Define and manage ERDTasks with multiple steps
- Assign tasks to different user roles (employee, manager, finance)
- Execute steps with input, approval, or rejection logic
- Built-in validation middleware simulating external API checks
- Admin dashboard with real-time ERDTask tracking

---

## 🚀 Tech Stack

- 🔧 .NET 7 Web API + Entity Framework Core
- 🎨 Angular 18 (Standalone API) + Angular Material
- 🧠 SQLite (local database)
- 🧪 Swagger UI for interactive API documentation

---


---

## 🔧 Setup Instructions

### 🖥️ Backend (.NET Core)

```bash
cd ERDTask
dotnet ef migrations add InitialCreate --project Infrastructure --startup-project ERPTask
 ERPTask % dotnet ef database update --project Infrastructure --startup-project ERPTask
dotnet ef database update
dotnet run



Folder Structure

/backend
  ├── Controllers
  ├── Models
  ├── DTOs
  ├── Middleware
  └── Services
