# المصادقة

لا يوجد تسجيل عام. ينشئ Admin الحسابات الداخلية. الدخول يعيد Access Token قصير العمر وRefresh Token. لا يُحفظ Refresh Token نصيًا؛ تُحفظ بصمة SHA-256 فقط، ويُدوّر عند كل تحديث ويرتبط البديل بالسابق. تغيير كلمة المرور يلغي جميع رموز التحديث النشطة، وIdentity يطبق hashing وlockout وسياسة كلمة مرور قابلة للتهيئة.

```json
POST /api/v1/auth/login
{"userName":"admin","password":"...","device":"Chrome/Windows"}
```

المسارات: `login`, `refresh-token`, `logout`, `change-password`, `forgot-password`, `reset-password`, و`me`. مسارات الدخول محدودة المعدل. اضبط `Jwt__SigningKey` بسر عشوائي لا يقل عن 32 محرفًا خارج المصدر.
