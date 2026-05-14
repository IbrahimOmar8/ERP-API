# نظام ERP - المخازن ونقاط البيع (Egyptian Edition)

نظام متكامل لإدارة المخازن ونقاط البيع متوافق مع النظام الضريبي المصري والفاتورة الإلكترونية (ETA).

## المكونات

### 1. Backend (.NET 8 Clean Architecture)
- **Domain** - النماذج والكيانات والـ Enums
- **Application** - DTOs والخدمات وواجهات الأعمال
- **Infrastructure** - EF Core (SQLite)، DbContext، Migrations
- **ERPTask** - مشروع API (Controllers + Swagger + JWT)

### 2. Mobile (Flutter)
تطبيق موبايل في مجلد `Mobile/` يدعم:
- نقاط البيع (POS) مع قارئ الباركود
- إدارة جلسات الكاش
- عرض المخزون والأرصدة وتحويلات المخازن
- لوحة تحكم + تقارير المبيعات
- طباعة فاتورة 80mm حرارية + مشاركة PDF
- ضريبة القيمة المضافة المصرية (14%)
- تكامل الفاتورة الإلكترونية ETA (إرسال / تحديث / إلغاء)

### 3. Web (Next.js 14 + React + TypeScript)
تطبيق ويب SPA في مجلد `Web/` — يستهلك نفس الـ API عبر JWT:
- Next.js 14 App Router + Tailwind CSS + TanStack Query + Zustand
- صفحات: تسجيل الدخول، لوحة تحكم، POS، جلسات الكاش، الفواتير، تفاصيل الفاتورة + ETA، العملاء، الأصناف، الرصيد، التحويلات، تقرير المبيعات، الإعدادات
- مصادقة JWT مع تجديد تلقائي للتوكن
- يشتغل على `http://localhost:3000` ويتصل بالـ API على `http://localhost:5000/api`

## الوحدات الوظيفية

### المخازن
- الأصناف (مع SKU وBarcode وأكواد ETA/GS1)
- الفئات والوحدات
- المخازن المتعددة
- حركات المخزون (وارد، صادر، تحويل، تسوية، مرتجع)
- التكلفة المتوسطة (Weighted Average)
- فواتير المشتريات + الموردون
- **تحويلات بين المخازن** مع الحفاظ على التكلفة المتوسطة
- ضبط المخزون (Stock Adjustment)

### نقاط البيع
- العملاء (أفراد وشركات مع رقم تسجيل ضريبي)
- ماكينات الكاشير وجلسات الكاش
- فواتير البيع متعددة طرق الدفع (كاش، بطاقة، InstaPay، محفظة، آجل)
- المرتجعات
- خصم على مستوى البند أو الفاتورة
- ضريبة القيمة المضافة المصرية
- **طباعة الفواتير**: HTML (A4 + 80mm حراري) + QR code متوافق مع مواصفات ETA
- **تقرير X/Z** لجلسة الكاشير (نقدي، بطاقة، فروق)

### التقارير ولوحة التحكم
- `GET /api/reports/dashboard` — KPIs لليوم والشهر (مبيعات، أرباح، عدد الفواتير، عملاء، أصناف، أصناف بحد أدنى، قيمة المخزون، جلسات مفتوحة)
- `GET /api/reports/sales?from=&to=&warehouseId=` — تقرير مبيعات يومي بصافي وضريبة وربح
- `GET /api/reports/top-products?from=&to=&take=10` — الأعلى مبيعاً
- `GET /api/reports/stock?warehouseId=&onlyLow=true` — تقرير المخزون
- `GET /api/reports/cash-sessions/{sessionId}` — تقرير X/Z للجلسة

### الامتثال المصري (ETA)
- ملف الشركة (الرقم الضريبي، السجل التجاري، كود النشاط، EtaClientId/Secret)
- معدلات الضرائب (T1, T2, T3)
- **تكامل ETA حقيقي**:
  - OAuth2 Client Credentials مع تخزين كاش للتوكن لكل عميل
  - بناء مستند الفاتورة بمواصفات ETA كاملة (Issuer/Receiver/Lines/Taxes/Signatures)
  - `EtaCanonicalSerializer` ينتج النص القانوني المُستخدم في توقيع CMS/PKCS7 وحساب SHA-256
  - `POST /api/einvoice/sales/{id}/submit` — إرسال للمصلحة (مع توقيع CMS اختياري)
  - `POST /api/einvoice/sales/{id}/refresh` — تحديث الحالة من ETA
  - `POST /api/einvoice/sales/{id}/cancel` — إلغاء الفاتورة من المصلحة بسبب
  - `GET /api/einvoice/recent` — آخر الإرساليات
  - `GET /api/sales/{id}/qr` — QR ETA (`ETA|company|TRN|date|total|vat|invoiceNo|UUID`)

## التشغيل

### Backend (API)
```bash
cd ERPTask
dotnet restore
dotnet run
```

عند أول تشغيل سيتم إنشاء قاعدة البيانات وحساب المدير الافتراضي:
- **اسم المستخدم:** `admin`
- **كلمة المرور:** `Admin@1234`  
  (غيّرها فوراً عبر `POST /api/Auth/change-password`)

### Web (Next.js)
```bash
cd Web
cp .env.local.example .env.local
npm install
npm run dev
```

ثم افتح:
| الواجهة | الرابط |
|---|---|
| Swagger API | `http://localhost:5000/swagger` |
| تطبيق الويب | `http://localhost:3000/` |

> ⚠️ **قبل الإنتاج**: عدّل `appsettings.json` ✦ `Jwt:Key` لمفتاح عشوائي طويل (32+ حرف) و `DefaultAdminPassword`.

### تفعيل تكامل ETA الحقيقي
1. في `appsettings.json` غيّر `EtaInvoicing.Enabled` إلى `true`:
   ```json
   "EtaInvoicing": {
     "BaseUrl": "https://api.preprod.invoicing.eta.gov.eg",
     "AuthUrl": "https://id.preprod.eta.gov.eg/connect/token",
     "Scope": "InvoicingAPI",
     "RequestTimeoutSeconds": 60,
     "Enabled": true
   }
   ```
2. أنشئ سجلاً في جدول `CompanyProfiles` وعبّئ:
   - `TaxRegistrationNumber` (الرقم الضريبي)
   - `EtaClientId` (Client ID من بوابة ETA)
   - `EtaClientSecret` (Client Secret)
   - `EtaEnabled = true`
3. للتقديم بتوقيع حقيقي مرّر `signedCmsBase64` (PKCS#7 من USB Token / HSM):
   ```http
   POST /api/einvoice/sales/{saleId}/submit
   { "signedCmsBase64": "MIIK..." }
   ```
   إذا تُرك null، يُرسل hash SHA-256 للنص الكنسي (مناسب لاختبار preprod فقط).

### Mobile
```bash
cd Mobile
flutter pub get
flutter run
```
عدّل `Mobile/lib/core/config/api_config.dart` ✦ `baseUrl` ليشير إلى عنوان السيرفر (افتراضياً `http://10.0.2.2:5000/api` لـ Android emulator).

## المصادقة (Auth)

كل الـ API يحمي بـ JWT Bearer. الويب يحفظ التوكن في localStorage ويضيفه تلقائياً للهيدر `Authorization`. عند انتهاء الصلاحية، يحاول تجديده بـ `POST /api/Auth/refresh` ثم يعيد المحاولة. لو فشل، يحوّل لصفحة الدخول.

CORS مضبوط للسماح فقط للنطاقات المحددة في `Cors:AllowedOrigins` (افتراضياً `http://localhost:3000`). أضف نطاق الإنتاج هناك.

## الأدوار
- `Admin` - كل الصلاحيات
- `Manager` - إدارة الأصناف، المخازن، الموظفين، المبيعات، التحويلات
- `Cashier` - فتح/إغلاق جلسة وبيع
- `WarehouseKeeper` - إدارة الأصناف والمخزون والمشتريات والتحويلات
- `Accountant` - الفواتير والمدفوعات والمرتجعات وتقارير ETA

## التقنيات

- 🔧 .NET 8 Web API + EF Core 8 + SQLite
- 🌐 Next.js 14 + React 18 + TypeScript + Tailwind CSS + TanStack Query + Zustand
- 📱 Flutter 3.19+ مع `pdf` + `printing` + `flutter_barcode_scanner`
- 📊 AutoMapper, MediatR
- 🧪 Swagger UI
- 🧾 ETA E-Invoicing (Egyptian Tax Authority) + QRCoder

## بنية المشروع

```
/
├── Domain/              # نماذج البيانات
│   ├── Models/
│   │   ├── Inventory/   # نماذج المخازن
│   │   ├── POS/         # نماذج نقاط البيع
│   │   ├── Egypt/       # نماذج الامتثال المصري
│   │   └── Auth/        # المستخدمون والأدوار
│   └── Enums/
├── Application/         # منطق الأعمال
│   ├── DTOs/
│   ├── Services/
│   │   ├── Egypt/       # ETA token + canonical serializer + document builder
│   │   ├── Reports/     # تقارير ولوحة تحكم
│   │   └── Inventory/   # ... + StockTransferService
│   └── Inerfaces/
├── Infrastructure/      # EF Core + الـ DbContext
├── ERPTask/             # Web API فقط
│   ├── Controllers/     # ETA, Reports, StockTransfers, InvoicePrint, CompanyProfile, ...
│   └── Services/        # InvoicePrintService (HTML A4 + 80mm + QR)
├── Web/                 # تطبيق Next.js (راجع Web/README.md)
│   └── src/app/         # App Router pages: login, dashboard, pos, sales, ...
└── Mobile/              # تطبيق Flutter
    └── lib/features/
        ├── auth/        # تسجيل الدخول وإدارة JWT
        ├── pos/         # كاشير + فواتير + ETA + طباعة PDF
        ├── inventory/   # أصناف + رصيد + تحويلات
        └── reports/     # Dashboard + Sales report
```

## ملاحظات مهمة قبل النشر
- لم يتم تنفيذ `dotnet build` أو `npm run build` في بيئة المطور الأصلية لعدم توفر SDK/Node محلياً؛ شغّل `dotnet restore && dotnet build` و `npm install && npm run build` على جهازك للتأكد من نجاح الكومبايل قبل أول تشغيل.
- لتجربة الـ ETA في بيئة الإنتاج راجع وثائق ETA لاستبدال preprod URLs بـ production URLs.
- لو الـ API والويب على نطاقات مختلفة، حدّث `Cors:AllowedOrigins` في `appsettings.json` و `NEXT_PUBLIC_API_URL` في `Web/.env.local`.
