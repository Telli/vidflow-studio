# Development Setup Instructions

## Quick Start

This document provides step-by-step instructions for setting up the VidFlow Studio development environment.

## Prerequisites

- **.NET 8+** - Backend framework
- **Node.js 18+** - Frontend runtime
- **PostgreSQL** - Primary database
- **Redis** (optional) - Caching and session storage
- **Git** - Version control

## Backend Setup

### 1. Navigate to Backend Directory
```bash
cd VidFlow
```

### 2. Restore Dependencies
```bash
dotnet restore
```

### 3. Configure Database
Update `VidFlow.Api/appsettings.json` with your PostgreSQL connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=VidFlow;Username=your_username;Password=your_password"
  }
}
```

### 4. Run Database Migrations
```bash
dotnet ef database update --project VidFlow.Api
```

### 5. Start the API
```bash
dotnet run --project VidFlow.Api
```

The API will be available at `https://localhost:5001`

## Frontend Setup

### 1. Navigate to Frontend Directory
```bash
cd Qipixel
```

### 2. Install Dependencies
```bash
npm install
```

### 3. Start Development Server
```bash
npm start
```

The frontend will be available at `http://localhost:3000`

## Environment Configuration

### Backend Environment Variables
Create `VidFlow.Api/.env`:

```env
ASPNETCORE_ENVIRONMENT=Development
DATABASE_URL=your_postgres_connection_string
REDIS_URL=your_redis_connection_string
OPENAI_API_KEY=your_openai_key
ANTHROPIC_API_KEY=your_anthropic_key
GEMINI_API_KEY=your_gemini_key
```

### Frontend Environment Variables
Create `Qipixel/.env`:

```env
REACT_APP_API_URL=https://localhost:5001
REACT_APP_WS_URL=wss://localhost:5001/hubs/agent-activity
```

## Running Tests

### Backend Tests
```bash
dotnet test VidFlow.Api.Tests
```

### Frontend Tests
```bash
cd Qipixel
npm test
```

## Development Workflow

1. **Create Feature Branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make Changes**
   - Backend changes in `VidFlow/`
   - Frontend changes in `Qipixel/`

3. **Run Tests**
   ```bash
   # Backend
   dotnet test VidFlow.Api.Tests
   
   # Frontend
   cd Qipixel && npm test
   ```

4. **Commit and Push**
   ```bash
   git add .
   git commit -m "feat: add your feature"
   git push origin feature/your-feature-name
   ```

5. **Create Pull Request**
   - Visit GitHub repository
   - Click "New Pull Request"
   - Select your feature branch
   - Fill out PR template
   - Submit for review

## Troubleshooting

### Database Connection Issues
- Ensure PostgreSQL is running
- Verify connection string format
- Check database exists and user has permissions

### Frontend Build Errors
- Clear node_modules: `rm -rf node_modules package-lock.json`
- Reinstall: `npm install`
- Check Node.js version: `node --version`

### API Startup Issues
- Verify .NET 8+ is installed: `dotnet --version`
- Check port availability (5001)
- Review application logs for specific errors

## Additional Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [React Documentation](https://reactjs.org/docs/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Redis Documentation](https://redis.io/documentation)

## Getting Help

If you encounter issues:

1. Check this troubleshooting section
2. Review the main README.md
3. Check the PRD and Technical PRD documents
4. Create an issue in the GitHub repository

---

Happy coding! 🚀
