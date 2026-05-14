# ERP Web (Next.js)

تطبيق ويب لإدارة المخازن ونقاط البيع — يستهلك ASP.NET API عبر JWT.

## التقنيات

- **Next.js 14** App Router + React 18 + TypeScript
- **Tailwind CSS** للتنسيق + RTL Arabic
- **TanStack Query** لإدارة بيانات الخادم
- **Zustand** لحالة المصادقة (مع persist)
- **Axios** + interceptor للـ JWT + تجديد التوكن التلقائي
- **lucide-react** للأيقونات
- **react-hot-toast** للإشعارات

## الصفحات

| المسار | الوصف |
|---|---|
| `/login` | تسجيل الدخول |
| `/dashboard` | لوحة التحكم — KPIs + الأعلى مبيعاً |
| `/pos` | كاشير POS — شبكة أصناف + سلة + باركود + دفع |
| `/cash-sessions` | فتح/إغلاق جلسات الكاش + X/Z |
| `/sales` | سجل الفواتير |
| `/sales/[id]` | تفاصيل الفاتورة + ETA submit/refresh/cancel + طباعة |
| `/customers` | العملاء (CRUD) |
| `/products` | الأصناف (عرض + بحث) |
| `/stock` | تقرير الرصيد |
| `/transfers` | تحويلات بين المخازن |
| `/sales-report` | تقرير مبيعات حسب التاريخ |
| `/settings` | إعدادات الشركة وتفعيل ETA |

## التشغيل

```bash
cd Web
cp .env.local.example .env.local   # اضبط NEXT_PUBLIC_API_URL
npm install
npm run dev
```

الويب سيشتغل على `http://localhost:3000`. تأكد أن الـ API يعمل على البورت الذي ضبطته في `NEXT_PUBLIC_API_URL` (الافتراضي `http://localhost:5000/api`).

> ⚠️ تأكد أن `Cors:AllowedOrigins` في `ERPTask/appsettings.json` يحتوي على `http://localhost:3000`.

## البناء للإنتاج

```bash
npm run build
npm start
```

`next.config.js` مضبوط على `output: 'standalone'` للنشر داخل Docker أو Vercel.

## بنية المشروع

```
Web/src/
├── app/
│   ├── (app)/         # مجموعة الصفحات المحمية (تحتاج تسجيل دخول)
│   │   ├── layout.tsx # السايد بار + حماية المصادقة
│   │   ├── dashboard/
│   │   ├── pos/
│   │   ├── sales/
│   │   ├── customers/
│   │   ├── products/
│   │   ├── stock/
│   │   ├── transfers/
│   │   ├── cash-sessions/
│   │   ├── sales-report/
│   │   └── settings/
│   ├── login/
│   ├── layout.tsx     # Providers + خط Cairo + RTL
│   ├── page.tsx       # redirect to /dashboard or /login
│   └── globals.css    # Tailwind + utilities
├── components/        # Sidebar, RequireAuth, KpiCard, PageHeader, Providers
├── lib/               # api.ts, auth.ts, format.ts
└── types/             # تعريفات الـ DTOs (مطابقة لـ C#)
```

## المصادقة

- التوكن يُخزّن في `localStorage` (Zustand persist).
- Axios يضيف `Authorization: Bearer <token>` تلقائياً.
- لو الـ API رد 401: يحاول تجديد التوكن مرة واحدة بـ refresh token، وإن فشل يحوّل إلى `/login`.
- صفحات `(app)` محمية بمكوّن `RequireAuth`.

## ملاحظات للنشر
- إعداد متغير `NEXT_PUBLIC_API_URL` للإنتاج (مثل `https://api.example.com/api`).
- استخدم HTTPS في الإنتاج لحماية الـ JWT.
- لو الـ API على نطاق مختلف، تأكد من ضبط CORS في `ERPTask/appsettings.json`.
