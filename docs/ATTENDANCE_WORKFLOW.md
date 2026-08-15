# مسار الحضور والرصيد

1. الطالب ينشئ طلبًا لجلسة تخصه فقط.
2. Admin أو Moderator يؤكد أو يرفض.
3. التأكيد يبدأ transaction واحدة، يعيد فحص الحالة والرصيد، ثم يحدّث الحضور والجلسة والرصيد.
4. تُضاف حركة `StudentCreditTransaction`، واستحقاق `TeacherEarning` بسعر الجلسة التاريخي، وقيد `FinancialTransaction`، وسجل Audit.
5. قيد فريد على `Attendance.SessionId` و`TeacherEarning.SessionId`، مع RowVersion ومعاملة SQL، يمنع الخصم أو الاستحقاق المكرر. التأكيد المتكرر يعيد نجاحًا دون أثر جديد.

لا يحدث خصم عند الرفض أو الإلغاء، ولا يُسمح برصيد سالب.
