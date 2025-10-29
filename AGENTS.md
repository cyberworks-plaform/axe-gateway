# Agent Guidelines - Axe Gateway

## 📁 Project Info
- **Framework**: ASP.NET Core 9.0 MVC
- **Location**: `D:\project\cyberworks-github\axe-gateway\Ce.Gateway.Api\`

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

## 📋 Routes
- `/dashboard` → Pages\DashboardController
- `/monitor` → Pages\MonitorController  
- `/nodehealth/ui` → Pages\NodeHealthController
- `/api/{feature}/*` → Api\{Feature}Controller
