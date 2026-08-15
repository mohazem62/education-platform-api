# رموز الأخطاء

كل خطأ يعيد `success=false` و`message` و`errors[]` و`traceId`. الرموز الثابتة الأساسية:

| Code | Meaning |
|---|---|
| `AUTH_INVALID_CREDENTIALS` | بيانات دخول/تحديث غير صحيحة |
| `AUTH_ACCOUNT_LOCKED` | الحساب مقفل مؤقتًا |
| `VALIDATION_ERROR` | فشل تحقق المدخلات |
| `FORBIDDEN_RESOURCE` | فشل صلاحية أو ملكية |
| `STUDENT_NOT_FOUND` / `TEACHER_NOT_FOUND` / `SESSION_NOT_FOUND` | المورد غير موجود |
| `INSUFFICIENT_SESSION_BALANCE` | الرصيد لا يكفي |
| `ATTENDANCE_ALREADY_CONFIRMED` | الحضور عولج سابقًا |
| `FINANCIAL_PERIOD_CLOSED` | الفترة مغلقة/قيد الحساب |
| `INVALID_PARTNER_PERCENTAGES` | مجموع النسب ليس 100% |
| `CONCURRENCY_CONFLICT` | تعارض RowVersion |
| `DATA_CONFLICT` | قيد فريد/تكامل في قاعدة البيانات |
