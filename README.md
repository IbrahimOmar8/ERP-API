# نظام ERP - المخازن ونقاط البيع (Egyptian Edition)

نظام متكامل لإدارة المخازن ونقاط البيع متوافق مع النظام الضريبي المصري والفاتورة الإلكترونية (ETA).

## المكونات

### 1. Backend (.NET 8 Clean Architecture)
- **Domain** - النماذج والكيانات والـ Enums
- **Application** - DTOs والخدمات وواجهات الأعمال
- **Infrastructure** - EF Core (SQLite)، DbContext، Migrations
- **ERPTask** - مشروع API (Controllers + Swagger)

### 2. Mobile (Flutter)
تطبيق موبايل في مجلد `Mobile/` يدعم:
- نقاط البيع (POS) مع قارئ الباركود
- إدارة جلسات الكاش
- عرض المخزون والأرصدة
- ضريبة القيمة المضافة المصرية (14%)
- تكامل الفاتورة الإلكترونية (ETA)

## الوحدات الوظيفية

### المخازن
- الأصناف (مع SKU وBarcode وأكواد ETA/GS1)
- الفئات والوحدات
- المخازن المتعددة
- حركات المخزون (وارد، صادر، تحويل، تسوية، مرتجع)
- التكلفة المتوسطة (Weighted Average)
- فواتير المشتريات + الموردون
- تحويلات المخازن
- ضبط المخزون (Stock Adjustment)

### نقاط البيع
- العملاء (أفراد وشركات مع رقم تسجيل ضريبي)
- ماكينات الكاشير وجلسات الكاش
- فواتير البيع متعددة طرق الدفع (كاش، بطاقة، InstaPay، محفظة، آجل)
- المرتجعات
- خصم على مستوى البند أو الفاتورة
- ضريبة القيمة المضافة المصرية

### الامتثال المصري
- ملف الشركة (الرقم الضريبي، السجل التجاري، كود النشاط)
- معدلات الضرائب (T1, T2, T3)
- إرسال الفواتير الإلكترونية للمصلحة (ETA Submissions)

## التشغيل

### Backend
```bash
cd ERPTask
dotnet restore
dotnet ef migrations add InitialCreate --project ../Infrastructure --startup-project .
dotnet ef database update --project ../Infrastructure --startup-project .
dotnet run
```

Swagger متاح على: `http://localhost:5000/swagger`

### Mobile
```bash
cd Mobile
flutter pub get
flutter run
```

## التقنيات

- 🔧 .NET 8 Web API + EF Core 8 + SQLite
- 📱 Flutter 3.19+
- 📊 AutoMapper, MediatR
- 🧪 Swagger UI
- 🧾 ETA E-Invoicing (Egyptian Tax Authority)

## بنية المشروع

```
/
├── Domain/              # نماذج البيانات
│   ├── Models/
│   │   ├── Inventory/   # نماذج المخازن
│   │   ├── POS/         # نماذج نقاط البيع
│   │   └── Egypt/       # نماذج الامتثال المصري
│   └── Enums/
├── Application/         # المنطق الأعمال
│   ├── DTOs/
│   ├── Services/
│   └── Inerfaces/
├── Infrastructure/      # EF Core + الـ DbContext
├── ERPTask/             # Web API + Controllers
└── Mobile/              # تطبيق Flutter
```
