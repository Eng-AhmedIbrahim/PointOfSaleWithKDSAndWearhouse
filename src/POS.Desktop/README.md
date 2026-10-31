# POS Desktop Application

تطبيق Point of Sale لسطح المكتب مبني على Blazor WebView و WPF.

## المميزات

- ✅ تطبيق سطح مكتب كامل باستخدام Blazor WebView
- ✅ تسجيل الملفات (File Logging) باستخدام Serilog
- ✅ نفس واجهة المستخدم من تطبيق الويب
- ✅ دعم كامل لجميع ميزات POS

## متطلبات التشغيل

- .NET 8.0 SDK
- Windows 10/11
- Visual Studio 2022 أو أحدث

## إعداد المشروع

1. افتح ملف الحل `POS-Desktop.sln`
2. قم ببناء المشروع `POS.Desktop`
3. تأكد من أن API يعمل على `https://localhost:7142`

## تسجيل الملفات

يتم حفظ ملفات السجل في مجلد `POS-Logs` على سطح المكتب.

مسار الملفات: `%USERPROFILE%\Desktop\POS-Logs\POS-Desktop-YYYY-MM-DD.txt`

## البناء والنشر

```bash
dotnet build POS.Desktop/POS.Desktop.csproj -c Release
dotnet publish POS.Desktop/POS.Desktop.csproj -c Release -r win-x64 --self-contained
```

## الإعدادات

يمكن تعديل إعدادات API من ملف `appsettings.json`

## هيكل المشروع

```
POS-Desktop-App/
├── POS.Desktop/          # المشروع الرئيسي
├── Dependencies/          # المشاريع المرجعية
│   ├── BlazorBase/
│   ├── POS.Contract/
│   ├── POS.Authorization/
│   ├── POS.Core/
│   └── POS.Reports/
└── POS-Desktop.sln       # ملف الحل
```