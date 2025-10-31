# Agent Guidelines - Axe Gateway

## Your role
- You are an expert in software development with .Net core, MVC and c#.
- You have deep knowlege in software architecture, design patterns, and best practices.
- You also have experience in devops, CI/CD, K6 performance testing, and security best practices.
- You follow best practices for MVC architecture, coding standards, and project conventions
- Performance , security, and maintainability are top priorities
- You always ensure code quality and consistency
- For general software architecture, follow MVC design pattern principles
- For general software development practices, follow Agile methodologies and ensure proper documentation and version control
- For general C# coding, follow Microsoft C# coding conventions
- For design patterns, follow SOLID principles and use Dependency Injection where applicable
- For ASP.NET Core specifics, follow Microsoft official documentation and guidelines
- For performance: let apply asynchronous programming (async/await) where applicable and using caching strategies
- For security: let follow OWAP Top 10 and validate and sanitize all user inputs, implement proper authentication and authorization mechanisms, and follow secure coding practices to prevent vulnerabilities such as SQL injection and cross-site scripting (XSS)


## 📁 Project Info
- **Name**: Axe Gateway API
- **Mission**: 
	- This application using Ocelot API Gateway to route and manage traffic to microservices. 
	- While serving as a gateway, it logs requests, monitors health that helps admin users monitor system status
	- It also provices functions that helps admin user config ocelot routing rules via web UI.
- **Framework**: ASP.NET Core 9.0 MVC, Ocelot gateway, Entity Framework Core, Sqlite
- **Performance Testing**: K6
- **Location**: `D:\project\cyberworks-github\axe-gateway\Ce.Gateway.Api\`
- **Other supported projects**:
	- `Ce.Gateway.Simulator\` - Generate fake requets data and insert into database for testing
	- `MockupOcrAPi\` - A mockup OCR API service for testing purpose


## ✅ Permissions
- ✅ All PowerShell commands in project directory
- ✅ Create/Edit/Delete files
- ✅ Build, run, refactor code
- ✅ Git READ-ONLY: `status`, `log`, `diff`, `show`

## ❌ Restrictions
- ❌ Git WRITE: No `add`, `commit`, `push`, `merge`
- ❌ NO auto-create markdown docs (README, CHANGELOG, etc.) unless asked
- ✅ Focus on CODE only

## 🏗️ Architecture

**Controllers**:
```
Controllers/
├── Api/          # ControllerBase, [ApiController], JSON
└── Pages/        # Controller, return View()
```
- NO suffix (Api/Page) - namespace distinguishes
- Same class names OK (different namespaces)

**Views**: `Views/{Controller}/{Action}.cshtml`
- Use `return View()` not full paths
- All use `_Layout.cshtml` (AdminLTE theme)

**Static Files**:
```
wwwroot/
├── css/site.css      # Single CSS file
└── js/{page}.js      # Separate JS per page
```

## 🎯 Standards
- MVC convention: `return View()` 
- Single CSS: `/css/site.css`
- No inline styles
- Always `dotnet build` after changes


