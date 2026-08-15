# API v1

الجذر `/api/v1`. الاستجابات الناجحة تستخدم `{success,message,data,errors}` والقوائم تضيف `pageNumber/pageSize/totalCount/totalPages/hasPreviousPage/hasNextPage`. الحد الأقصى للصفحة 100.

المجموعات المنفذة: Auth، Users، Students، Teachers، Moderators، Partners، Subjects، Curricula، GradeLevels، TeacherAssignments، Sessions، Attendance، StudentCredits/Payments ضمن الطالب، Materials، Assignments، Submissions، TeacherPayouts، PayoutAdjustments، OperatingExpenses، FinancialPeriods، PartnerShares، PartnerDividends، FinancialTransactions، Notifications، AuditLogs، ArchivedRecords، Reports، وDashboards الأربعة.

أمثلة:

```http
GET /api/v1/teachers?pageNumber=1&pageSize=20&subjectId=<guid>&curriculumId=<guid>
Authorization: Bearer <jwt>
Accept-Language: ar
```

```json
POST /api/v1/attendance/{attendanceId}/confirm
{"notes":"تم التحقق","idempotencyKey":"attendance-2026-001"}
```

```json
POST /api/v1/students/{studentId}/payments
{"amount":1200,"currency":"EGP","paymentMethod":"Cash","paidAt":"2026-08-15T12:00:00Z","idempotencyKey":"receipt-123","purchasedCredits":8}
```

التفاصيل الكاملة والنماذج والحالات متاحة تفاعليًا في Swagger، ويمكن استيراد `docs/openapi.json` أو `postman/EducationPlatform.postman_collection.json`.
