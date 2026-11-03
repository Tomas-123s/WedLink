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

تُحفظ ملفات التقديم الخاصة محليًا خارج `wwwroot` في `Backend/WedLink.API/PrivateMedia` افتراضيًا، ولا يمكن فتحها كرابط عام. يمكن تغيير المسار بإعداد `Storage:CustomerSubmissionPath`. قاعدة البيانات تحفظ البيانات الوصفية ومفتاح التخزين فقط. مزود الإنتاج لم يُحدد بعد.

## تشغيل الاختبارات

```powershell
dotnet test Backend\WedLink.Tests\WedLink.Tests.csproj
```
