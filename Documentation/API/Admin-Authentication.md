# مصادقة حسابات الإدارة

تستخدم لوحة الإدارة جلسة ASP.NET Core Identity عبر Cookie آمنة لأن الواجهة والـAPI مستضافان على نفس الدومين. لا تُحفظ كلمات المرور أو رموز الجلسة في `localStorage`.

## المسارات

| Method | Route | الاستخدام |
|---|---|---|
| GET | `/api/v1/admin/auth/csrf` | إصدار رمز CSRF للمتصفح |
| POST | `/api/v1/admin/auth/login` | تسجيل الدخول بالبريد وكلمة المرور |
| GET | `/api/v1/admin/auth/me` | التحقق من الجلسة الحالية |
| POST | `/api/v1/admin/auth/logout` | إنهاء الجلسة الحالية |

قبل `login` أو `logout`، يطلب المتصفح الرمز من `csrf` ويرسل قيمة `token` في ترويسة `X-CSRF-TOKEN`. يجب إرسال الطلبات مع بيانات اعتماد المتصفح (`credentials: "include"`). يتطلب تسجيل الدخول HTTPS. الجلسة HttpOnly، وSameSite=Lax، ومدتها 8 ساعات مع تجديد أثناء النشاط.

رد تسجيل الدخول الفاشل موحد ولا يكشف هل البريد مسجل. بعد خمس محاولات فاشلة، يُقفل الحساب لمدة 15 دقيقة. تتطلب كلمة المرور 8 أحرف على الأقل وتضم حرفًا كبيرًا وصغيرًا ورقمًا ورمزًا.

مثال مبسط للتدفق من الواجهة:

```ts
const csrfResponse = await fetch("/api/v1/admin/auth/csrf", {
  credentials: "include",
});
const { token } = await csrfResponse.json();

await fetch("/api/v1/admin/auth/login", {
  method: "POST",
  credentials: "include",
  headers: {
    "Content-Type": "application/json",
    "X-CSRF-TOKEN": token,
  },
  body: JSON.stringify({ email, password }),
});
```

لإنشاء أول حساب محلي، استخدم أداة الإعداد الموضحة في [إعداد قاعدة البيانات محليًا](../Database/LocalSetup.md). لا يوجد مسار عام لإنشاء حساب إدارة.
