# Contributing to VidFlow Studio

Thank you for your interest in contributing to VidFlow Studio! This guide will help you get started.

## 🎯 Project Vision

VidFlow Studio is a scene-first creative IDE for producing 5-20 minute short films using agentic workflows with human approval gates. We're building **Cursor for cinema**, not Runway with prompts.

## 🚀 Quick Start

1. **Fork the repository**
2. **Clone your fork locally**
3. **Set up development environment** (see DEVELOPMENT.md)
4. **Create a feature branch**
5. **Make your changes**
6. **Submit a pull request**

## 📋 Development Workflow

### Branch Naming Convention
- `feature/feature-name` - New features
- `bugfix/bug-description` - Bug fixes
- `docs/update-type` - Documentation updates
- `refactor/component-name` - Code refactoring

### Commit Message Format
Use conventional commits:

```
type(scope): description

[optional body]

[optional footer]
```

Types:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation
- `style`: Code style (formatting, etc.)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Build process, dependency updates

Examples:
```
feat(scenes): add scene approval workflow
fix(api): resolve character serialization issue
docs(readme): update installation instructions
```

## 🏗️ Architecture Overview

### Backend (VidFlow.Api)
- **Domain-Driven Design**: Entities, value objects, domain events
- **Feature-Based Architecture**: Each feature in its own folder
- **Event-Driven**: Append-only event store for auditability
- **Agent System**: Role-based AI agents with deterministic pipelines

### Frontend (Qipixel)
- **React + TypeScript**: Modern UI with type safety
- **Component-Based**: Reusable UI components
- **Real-Time Updates**: WebSocket integration for agent activity
- **State Management**: Zustand for application state

### Key Concepts
- **Scene-First**: Scenes are atomic units for review and rendering
- **Human-in-the-Loop**: All AI proposals require explicit approval
- **Non-Destructive**: AI agents create proposals, never mutate directly
- **Cost-Controlled**: Per-scene and per-project budget enforcement

## 🎬 Core Features

### Agent System
Our agent pipeline follows this order:
1. **Writer** - Script and dialogue
2. **Director** - Creative vision and pacing
3. **Cinematographer** - Camera work and visual style
4. **Editor** - Timing and flow
5. **Producer** - Budget and constraint compliance
6. **Showrunner** - Cross-scene consistency (runs after approval)

### Scene Lifecycle
1. **Draft** - Scene created, editable
2. **Review** - Submitted for human review
3. **Approved** - Human approved, ready for render
4. **Rendered** - Scene rendered successfully

### Event System
All state changes emit domain events:
- `SceneCreated`, `SceneUpdated`
- `AgentProposalCreated`, `AgentRunFailed`
- `SceneApproved`, `SceneRevisionRequested`
- `SceneRendered`, `FinalRenderCompleted`

## 🧪 Testing

### Backend Tests
```bash
dotnet test VidFlow.Api.Tests
```

### Frontend Tests
```bash
cd Qipixel
npm test
```

### Integration Tests
```bash
# Run E2E tests with Playwright
cd Qipixel
npm run test:e2e
```

## 📝 Code Style

### C#/.NET
- Follow Microsoft C# coding conventions
- Use PascalCase for public members
- Use camelCase for private members
- Add XML documentation for public APIs

### TypeScript/React
- Use Prettier for formatting
- Follow React hooks best practices
- Use functional components with hooks
- Add TypeScript types for all props

### General
- Write clear, descriptive variable names
- Add comments for complex logic
- Keep functions small and focused
- Don't repeat yourself (DRY)

## 🔧 Development Tools

### Required Tools
- **.NET 8+** - Backend framework
- **Node.js 18+** - Frontend runtime
- **PostgreSQL** - Database
- **Redis** - Caching (optional)

### Recommended Tools
- **VS Code** - Code editor
- **Postman** - API testing
- **pgAdmin** - Database management
- **GitKraken** - Git GUI (optional)

## 🐛 Bug Reports

When reporting bugs, please include:

1. **Environment**: OS, .NET version, Node version
2. **Steps to Reproduce**: Clear reproduction steps
3. **Expected Behavior**: What should happen
4. **Actual Behavior**: What actually happened
5. **Error Messages**: Full error logs/stack traces
6. **Screenshots**: If applicable

## 💡 Feature Requests

For feature requests, please include:

1. **Problem Statement**: What problem does this solve?
2. **Proposed Solution**: How should it work?
3. **User Stories**: Specific user scenarios
4. **Acceptance Criteria**: How to verify it's complete
5. **Priority**: High/Medium/Low

## 📖 Documentation

We maintain several documentation files:

- **README.md** - Project overview and quick start
- **DEVELOPMENT.md** - Detailed development setup
- **PRD** - Product requirements document
- **Technical PRD.md** - Technical specifications

When adding features, please update relevant documentation.

## 🤝 Code Review Process

### Before Submitting
1. **Self-review** your code
2. **Run all tests** locally
3. **Update documentation** if needed
4. **Follow commit message** conventions

### During Review
1. **Be responsive** to feedback
2. **Explain your reasoning** for complex changes
3. **Address all comments** before merging
4. **Keep discussions constructive**

### After Merge
1. **Delete your feature branch**
2. **Update any related issues**
3. **Celebrate your contribution**! 🎉

## 🎯 Areas for Contribution

### High Priority
- **Agent Implementations**: Complete the agent pipeline
- **UI Components**: Scene review workspace
- **Testing**: Unit and integration tests
- **Documentation**: API documentation

### Medium Priority
- **Performance**: Optimization and caching
- **Security**: Authentication and authorization
- **DevOps**: CI/CD pipeline setup
- **Monitoring**: Logging and metrics

### Low Priority
- **Mobile**: Responsive design improvements
- **Accessibility**: ARIA labels and keyboard navigation
- **Internationalization**: Multi-language support
- **Plugins**: Extensibility framework

## 📞 Getting Help

1. **Check existing issues** - Your question might be answered
2. **Read the documentation** - DEVELOPMENT.md, PRD, Technical PRD
3. **Join discussions** - GitHub discussions tab
4. **Create an issue** - For bugs or feature requests

## 📜 Code of Conduct

Please be respectful and constructive:
- **Welcome newcomers** and help them learn
- **Focus on what's best** for the community
- **Show empathy** toward other community members
- **Respect different opinions** and approaches

---

Thank you for contributing to VidFlow Studio! Together, we're building the future of creative filmmaking. 🚀🎬
