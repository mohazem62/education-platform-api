# Education Platform Backend

واجهة خلفية عربية أولًا لمنصة تعليمية متعددة الأدوار، مبنية بـ ASP.NET Core 9 وEF Core وSQL Server وIdentity/JWT وفق تقسيم Clean Architecture عملي.

## البنية

- `Domain`: الكيانات، الحالات وقواعد المال والرصيد.
- `Application`: DTOs، عقود الخدمات، الأخطاء والتحقق.
- `Infrastructure`: SQL Server/EF Core، Identity، JWT، التخزين والخدمات والمعاملات.
- `Api`: Controllers، السياسات، Swagger، التعريب، الحماية، معالجة الأخطاء والصحة.
- `tests`: اختبارات قواعد النطاق واختبارات HTTP smoke/authorization.

## التشغيل محليًا

المتطلبات: .NET SDK 9، Docker Desktop اختياريًا، وSQL Server 2022.

```powershell
Copy-Item .env.example .env
# ضع الأسرار كمتغيرات بيئة في جلسة التشغيل؛ docker compose يقرأ MSSQL_SA_PASSWORD من .env
docker compose up -d
dotnet tool restore
dotnet restore
dotnet tool run dotnet-ef database update --project src/EducationPlatform.Infrastructure --startup-project src/EducationPlatform.Api
dotnet run --project src/EducationPlatform.Api
```

مرّر `InitialAdmin__UserName` و`InitialAdmin__Password` مرة واحدة مع `Database__AutoMigrate=true` لإنشاء المدير الأول. لا توجد كلمة مرور مثبتة في المصدر. عطّل الخيار بعد الإنشاء.

Swagger: `/swagger`، OpenAPI JSON: `/swagger/v1/swagger.json`، الصحة: `/health/live` و`/health/ready`.

تسجيل الدخول عبر `POST /api/v1/auth/login` ثم استخدم `Authorization: Bearer <token>`. رسائل API عربية افتراضيًا، ويُقبل `Accept-Language: ar` أو `en`. البيانات والأوقات UTF-8 وUTC/ISO-8601.

## أوامر التحقق

```powershell
dotnet restore EducationPlatform.sln
dotnet build EducationPlatform.sln --no-restore
dotnet test EducationPlatform.sln --no-build
```

راجع [API](docs/API.md)، [المصادقة](docs/AUTHENTICATION.md)، [الصلاحيات](docs/ROLES_AND_PERMISSIONS.md)، [الحضور](docs/ATTENDANCE_WORKFLOW.md)، [المالية](docs/FINANCIAL_WORKFLOW.md)، [ERD](docs/ERD.md)، و[الافتراضات](docs/ASSUMPTIONS.md).
