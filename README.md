# CelexWebApp  

?? **Plataforma de gestión administrativa para CELEX (CECyT 13 - IPN)**  
*Optimizando procesos académicos y administrativos para alumnos, profesores y administradores.*  

---

## ?? **¿Qué hace CelexWebApp?**  
Este sistema centraliza y agiliza tareas clave del curso de inglés **CELEX** en el **CECyT 13 del IPN**, permitiendo:  
- **Gestión de usuarios**: CRUD para alumnos, profesores y administradores.  
- **Generación de documentos**: Reportes en PDF (ej: listas de asistencia, calificaciones).  
- **Acceso seguro**: Roles diferenciados (admin, profesor, alumno) con Identity.  
- **Integración con servicios externos**: [Si hay APIs de pago o sistemas del IPN, agrégalos aquí].  

---

## ?? **Tecnologías principales**  
- **Backend**:  
  ![.NET](https://img.shields.io/badge/.NET-8-%23512bd4)  
  - ASP.NET Core 8 (MVC)  
  - Entity Framework Core + SQL Server  
  - ASP.NET Core Identity (Autenticación por roles)  
- **Despliegue**:  
  ![Azure](https://img.shields.io/badge/Azure-%230072C6?logo=microsoft-azure)  
  - Azure App Service  
  - Azure SQL Database (opcional)  

---

## ?? **Configuración inicial**  

### **Requisitos**  
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)  
- SQL Server (Local o Azure)  
- Visual Studio 2022 / VS Code  

### **Pasos para ejecutar localmente**  
1. Clona el repositorio:  
   ```bash
   git clone https://github.com/WontedDuck/CelexWebAppReal
   cd CelexWebAppReal
