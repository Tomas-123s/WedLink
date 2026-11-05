# WedLink Frontend

واجهة WedLink مبنية باستخدام React وTypeScript وVite، وتضم الموقع العام ولوحة الإدارة وصفحات الدعوات العامة بالعربية وRTL.

## التشغيل محليًا

من مجلد `Frontend`:

```powershell
npm ci
npm run dev
```

يفتح Vite عنوانًا محليًا في المتصفح، غالبًا `http://localhost:5173`.
لتجربة صفحة الدعوة التجريبية بعد تجهيز قاعدة البيانات وتشغيل الـAPI، افتح `/invite/wedlink-editorial-luxury-demo` على نفس عنوان الواجهة.

### حل شاشة بيضاء أو خطأ `504 (Outdated Optimize Dep)`

أوقف خادم Vite (`Ctrl+C`) ثم أعد تشغيله مع إعادة بناء كاش الاعتماديات:

```powershell
npm run dev -- --force
```

بعد ظهور العنوان المحلي، حدّث الصفحة تحديثًا قويًا باستخدام `Ctrl+Shift+R`. إذا استمرت المشكلة، راجع أول خطأ في أدوات المطور `F12 > Console`.

## قوالب الدعوات

تقرأ صفحة الدعوة المفتوحة `rendererKey` آمنًا من API. القالب الأول هو `EditorialLuxuryTemplate`؛ ويُختار عند تعيين مفتاح الإصدار الداخلي إلى `editorial-luxury-v1` أو `cinematic-v*`. القيم الأخرى تعرض القالب الافتراضي. لا يُرسل `packageKey` الداخلي ضمن بيانات الدعوة العامة.

## إنشاء نسخة بناء

```powershell
npm run build
npm test
```

الصفحات والميزات موجودة تحت `src/features`. يشغّل CI اختبارات الواجهة ثم يبني نسخة الإنتاج عند كل push أو pull request.
