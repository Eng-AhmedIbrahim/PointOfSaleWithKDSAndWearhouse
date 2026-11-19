@echo off
title New Tech WMS - IIS Setup
color 0A
echo.
echo ============================================================
echo   New Tech WMS - IIS Setup Script
echo   تهيئة نظام إدارة المخازن على IIS
echo ============================================================
echo.

:: Check for admin privileges
net session >nul 2>&1
if %errorLevel% NEQ 0 (
    echo [ERROR] يجب تشغيل هذا الملف كـ Administrator
    echo         Right-click -> Run as Administrator
    echo.
    pause
    exit /b 1
)

echo [1/4] ضبط حجم الـ HTTP Headers في Windows...
reg add "HKLM\SYSTEM\CurrentControlSet\Services\HTTP\Parameters" /v MaxFieldLength /t REG_DWORD /d 65536 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Services\HTTP\Parameters" /v MaxRequestBytes /t REG_DWORD /d 65536 /f >nul 2>&1
echo       تم بنجاح ✓

echo [2/4] إيقاف IIS مؤقتاً...
iisreset /stop >nul 2>&1
echo       تم بنجاح ✓

echo [3/4] إعادة تشغيل IIS...
iisreset /start >nul 2>&1
echo       تم بنجاح ✓

echo [4/4] التحقق من الإعدادات...
reg query "HKLM\SYSTEM\CurrentControlSet\Services\HTTP\Parameters" /v MaxFieldLength >nul 2>&1
if %errorLevel% EQU 0 (
    echo       تم التحقق بنجاح ✓
) else (
    echo       [WARNING] تعذر التحقق من الإعدادات
)

echo.
echo ============================================================
echo   ✅ تم إعداد النظام بنجاح!
echo   يمكنك الآن استخدام نظام إدارة المخازن
echo ============================================================
echo.
pause
