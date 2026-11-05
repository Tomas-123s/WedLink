# إعداد قاعدة البيانات محليًا

يستخدم WedLink برنامج SQL Server محليًا، وEF Core لإدارة الاتصال والترحيلات. إعدادات الاتصال لا تُحفظ في ملفات المشروع؛ تُحفظ محليًا باستخدام .NET User Secrets.

## إعداد الاتصال على جهاز مطور جديد

تأكد من تثبيت وتشغيل SQL Server، ثم نفّذ من مجلد المشروع:

```powershell
dotnet user-secrets init --project Backend\WedLink.API
dotnet user-secrets set "ConnectionStrings:WedLinkDb" "Server=localhost;Database=WedLinkDb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;" --project Backend\WedLink.API
```

هذا الإعداد يستخدم Windows Authentication. `TrustServerCertificate=True` مخصص لشهادة التطوير المحلية؛ إعداد النشر الإنتاجي سيستخدم شهادة موثوقة وإعدادات مختلفة.

## إنشاء قاعدة البيانات أو تطبيق آخر ترحيل

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet tool restore
dotnet tool run dotnet-ef database update --project Backend\WedLink.DataAccess\WedLink.DataAccess.csproj --startup-project Backend\WedLink.API\WedLink.API.csproj --context WedLinkDbContext
```

لإنشاء أول حساب إدارة محليًا بعد ضبط User Secrets، شغّل الأمر التالي مرة واحدة. ستُطلب منك بيانات الحساب في الطرفية، وكلمة المرور لا تظهر أثناء الكتابة:

```powershell
dotnet run --project Backend\WedLink.AdminSetup
```

الأداة تطبّق الترحيلات، ثم تنشئ حسابًا بدور `SuperAdmin`. لا تحفظ كلمة المرور في ملفات المشروع. كلمة المرور تحتاج 8 أحرف على الأقل، وحرفًا كبيرًا وصغيرًا ورقمًا ورمزًا.

لإضافة ترحيل بعد إضافة أو تعديل كيانات مستقبلًا:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet tool run dotnet-ef migrations add MigrationName --project Backend\WedLink.DataAccess\WedLink.DataAccess.csproj --startup-project Backend\WedLink.API\WedLink.API.csproj --context WedLinkDbContext --output-dir Persistence\Migrations
```

غيّر `MigrationName` إلى اسم قصير يصف التعديل، ثم طبّق الترحيل بأمر `database update` أعلاه.

## ما الذي أنشأه الترحيل الأول؟

`InitialInfrastructure` يثبت اتصال EF Core ويُنشئ قاعدة `WedLinkDb` وجدول `__EFMigrationsHistory`. ترحيل `AdminIdentity` يضيف جداول حسابات وأدوار الإدارة. ترحيلات `Customers` و`UniqueCustomerPhone` و`ActiveCustomerPhoneUnique` تضيف جدول العملاء وتضمن تفرد الهاتف بين العملاء غير المحذوفين منطقيًا. يُزال الفراغ من رقم الهاتف قبل الحفظ، وتُترك بقية صيغته كما أدخلها المستخدم. ترحيل `TemplateCatalog` يضيف القوالب والإصدارات والمزايا والإضافات.

`WedLinkDbContext` هو نقطة EF Core التي تجمع جداول Identity والكيانات المستقبلية. اتصال SQL Server يُسجل في `Program.cs` باستخدام سلسلة الاتصال من الإعدادات.

## تشغيل الـAPI محليًا

```powershell
dotnet run --project Backend\WedLink.API --launch-profile https
```

فحص الصحة: `https://localhost:7270/api/v1/health`
OpenAPI أثناء التطوير: `https://localhost:7270/openapi/v1.json`

Invitation validity defaults to two calendar months after the wedding. Set `InvitationLifecycle:DefaultValidityMonths` (1–24) to change the period for new publications; existing invitations keep their stored expiry. The expiration worker checks every five minutes by default. Configure `InvitationLifecycle:ExpirationIntervalMinutes` (1–1440) to change that interval. Temporary media is deleted three months after the wedding; its worker checks every 60 minutes by default, configurable with `InvitationLifecycle:MediaCleanupIntervalMinutes` (1–1440). The database retains deleted-media metadata; the application does not keep a recovery copy.

تُحفظ ملفات التقديم والصور والموسيقى في جدول `StoredMediaFiles` بقاعدة البيانات، بينما تحتفظ جداول الوسائط بالبيانات الوصفية ومفتاح التخزين. يتطلب النشر تطبيق migration `StoreMediaInDatabase` قبل استخدام تخزين الملفات الجديد. الملفات القديمة المحفوظة على نظام الملفات لا تُنقل تلقائيًا، والملفات المفقودة تحتاج إلى رفعها مجددًا.

في الإنتاج، يقرأ الـAPI قاعدة البيانات من متغير Render السري `ConnectionStrings__WedLinkDb`. لا يحتوي المستودع على بيانات اتصال MonsterASP. اربط هذا المتغير بسلسلة **Remote** من MonsterASP (مع `Encrypt=True`) لأن الـAPI يعمل على Render؛ فعّل Remote access في MonsterASP، واستخدم اسم المضيف العام. بعد توجيه الاتصال إلى قاعدة MonsterASP، طبّق عليها الترحيلات قبل استقبال الملفات. لا تضع كلمة المرور في ملفات المشروع أو Git.

## تشغيل الاختبارات

```powershell
dotnet test Backend\WedLink.Tests\WedLink.Tests.csproj
```

## النسخ الاحتياطي والاستعادة

للتطوير المحلي، أوقف الكتابة إلى النظام ثم أنشئ نسخة من قاعدة البيانات باستخدام SQL Server Management Studio أو `BACKUP DATABASE`. احتفظ بالنسخة خارج مجلد المستودع، واحمها بصلاحيات وصول مناسبة. مثال T-SQL (استبدل المسار بمجلد نسخ احتياطي مخصص على خادم SQL Server):

```sql
BACKUP DATABASE [WedLinkDb]
TO DISK = N'<protected-backup-path>\WedLinkDb.bak'
WITH COPY_ONLY, INIT, CHECKSUM;
RESTORE VERIFYONLY FROM DISK = N'<protected-backup-path>\WedLinkDb.bak' WITH CHECKSUM;
```

لاستعادة نسخة محلية، استخدم SSMS أو `RESTORE DATABASE` إلى قاعدة اختبار منفصلة أولًا، ثم تحقق من الجداول والترحيلات والملفات الخاصة المرتبطة بها. ملفات الوسائط الخاصة تُخزّن خارج قاعدة البيانات؛ انسخ مجلد التخزين الخاص بصورة متسقة مع النسخة، ولا تضع النسخ أو الوسائط أو بيانات الاعتماد في Git. لم تُعتمد بعد سياسة احتفاظ إنتاجية أو مزود تخزين خارجي.

## ملاحظات P12

تغطي اختبارات التكامل API باستخدام قاعدة EF Core InMemory معزولة لكل مصنع اختبار. يلزم تشغيل تحقق يدوي على SQL Server وواجهات المتصفح قبل أي نشر؛ نتائج InMemory لا تثبت سلوك ترجمات SQL أو الأداء الإنتاجي.
