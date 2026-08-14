# Assignment & Submission Management System

A role-based, full-stack Web Application built with **ASP.NET Core Web API (C#)**, **Next.js (React & TypeScript)**, and **PostgreSQL**.

---

## 🌟 Features

### 👤 Role-based Access Control
*   **Admin**
    *   Manage users (Students, Teachers, Admins).
    *   Create and manage Classes/Courses & Subjects.
    *   Assign teachers to specific subjects/classes.
    *   View all assignments & student submissions across the system.
*   **Teacher**
    *   Create, update, and delete assignments with title, description, deadline, max marks, and class/subject association.
    *   Publish assignments immediately or keep as draft.
    *   View student submissions for their assignments (text, links, attached files).
    *   Grade submissions with marks, status updates, and written feedback.
*   **Student**
    *   View published assignments assigned specifically to their registered class.
    *   Submit assignment solutions using text response, URL links (e.g. GitHub/Google Docs), or uploaded file attachments.
    *   Update submission prior to the deadline.
    *   View submission status, teacher feedback, and awarded marks.

### 📄 Pagination (New)
*   **Assignments list** is fully paginated on both backend and frontend to ensure high performance even with large volumes of data.
*   **Submissions dashboard** supports page navigation (Next/Prev) for teachers reviewing class submissions.
*   **Notification history** panel lists items in batches of 10.

### 🔍 Advanced Filtering (New)
*   Search for assignments instantly using real-time search on title or description.
*   Filter assignments by subject/class, or by publication status (Draft/Published) for teachers and admins.
*   Flexible sorting by creation date, deadline, or title in ascending/descending order.
*   Filter submissions inside the grading panel by status (All, Submitted, Graded, Needs Revision).

### 🔔 In-App Notifications (New)
*   A real-time styled notification center directly inside the dashboard header with unread badge counter.
*   Automatic trigger system:
    *   **Students** are notified immediately when a new assignment is published for their class.
    *   **Teachers** receive notifications when students submit solutions to their assignments.
    *   **Students** receive feedback and grade alerts the moment their teacher grades a submission.
*   Unread indicators and quick "Mark all read" controls.

---

## 🛠️ Tech Stack

*   **Backend**: ASP.NET Core 8 Web API, Entity Framework Core (Npgsql PostgreSQL), MediatR (CQRS pattern), JWT Authentication, Swagger/OpenAPI.
*   **Frontend**: Next.js 14, React 18, TypeScript, Custom Glassmorphic Responsive CSS design system.
*   **Database**: PostgreSQL.
*   **Testing**: xUnit with EF Core In-Memory database.

---

## 🔑 Demo Credentials

| Role | Email Address | Password | Enrolled Class / Subject |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@school.com` | `Admin123!` | All System Access |
| **Teacher** | `teacher.rahim@school.com` | `Teacher123!` | Physics (Class 10 - Science) |
| **Teacher** | `teacher.karim@school.com` | `Teacher123!` | Chemistry (Class 10 - Science) |
| **Student** | `student.rafiq@school.com` | `Student123!` | Class 10 - Science |
| **Student** | `student.salma@school.com` | `Student123!` | Class 10 - Science |
| **Student** | `student.tariq@school.com` | `Student123!` | Class 11 - Science |

*Note: The login page includes quick-fill buttons for instant demo account login.*

---

## 🚀 Getting Started

### Option A: Run Backend & Frontend with Docker Compose
1. Make sure you have Docker Desktop installed.
2. Spin up the entire integrated application stack:
    ```bash
    docker compose up --build -d
    ```
3. Access the services:
    *   **Frontend App**: `http://localhost:3000`
    *   **Backend Web API**: `http://localhost:5000`
    *   **Swagger API Docs**: `http://localhost:5000/swagger`

To stop the running application:
```bash
docker compose down
```

---

### Option B: Run Locally without Docker (Development Mode)

#### 1. Start Database
Start the PostgreSQL container:
```bash
docker compose up -d postgres
```

#### 2. Run Backend API
```bash
cd backend
dotnet run
```
*   API will be accessible at: `http://localhost:5000`
*   Swagger Documentation: `http://localhost:5000/swagger`

#### 3. Run Unit Tests
To execute business logic unit tests:
```bash
dotnet test
```

#### 4. Run Frontend App
```bash
cd frontend
npm install
npm run dev
```
*   Frontend application will be available at: `http://localhost:3000`

---

## 📋 Technical Assumptions & Design Decisions

1.  **CQRS Architecture**: Handled via MediatR queries and commands to decouple API controllers from business logic handlers.
2.  **Submission Flexibility**: Students can submit text answers, external links, and uploaded files simultaneously.
3.  **Deadline Enforcement**: Submissions after the deadline are blocked automatically by backend business logic.
4.  **Auto-seeding**: Database seeds default classes, subjects, users, and assignments on initial startup for easy validation.