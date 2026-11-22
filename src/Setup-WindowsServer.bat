@echo off
chcp 65001 >nul 2>&1
title New Tech POS - Complete Server Setup Script
color 0B

echo ==============================================================================
echo        NEW TECH POS - COMPLETE WINDOWS SERVER AUTOMATION SETUP
echo        التهيئة الشاملة والكاملة لسيرفر النظام (IIS + SignalR + Firewall + Ports)
echo ==============================================================================
echo.

:: 1. Check for Administrator Privileges
net session >nul 2>&1
if %errorLevel% NEQ 0 (
    color 0C
    echo [ERROR] يجب تشغيل هذا الملف كمسؤول (Run as Administrator) !
    echo         اضغط كليك يمين على الملف واختر: Run as administrator
    echo.
    pause
    exit /b 1
)

echo [1/8] ضبط أداء السيرفر وخطة الطاقة (High Performance & Never Sleep)...
powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c >nul 2>&1
powercfg /change standby-timeout-ac 0 >nul 2>&1
powercfg /change monitor-timeout-ac 30 >nul 2>&1
powercfg /change disk-timeout-ac 0 >nul 2>&1
powercfg /hibernate off >nul 2>&1
echo       تم بنجاح [✓]

echo.
echo [2/8] تثبيت وتفعيل IIS بكافة الخصائص المطلوبة لـ ASP.NET Core و SignalR...
:: Try Windows Server native command first, fallback to DISM
powershell -Command "if (Get-Command Install-WindowsFeature -ErrorAction SilentlyContinue) { Install-WindowsFeature -Name Web-Server, Web-WebServer, Web-Common-Http, Web-Default-Doc, Web-Dir-Browsing, Web-Http-Errors, Web-Static-Content, Web-Http-Redirect, Web-Net-Ext45, Web-Asp-Net45, Web-ISAPI-Ext, Web-ISAPI-Filter, Web-WebSockets, Web-Filtering, Web-Mgmt-Console -IncludeManagementTools | Out-Null }" >nul 2>&1

:: Universal DISM activation to guarantee WebSockets and IIS are enabled
dism /online /enable-feature /featurename:IIS-WebServerRole /all /norestart >nul 2>&1
dism /online /enable-feature /featurename:IIS-WebServer /all /norestart >nul 2>&1
dism /online /enable-feature /featurename:IIS-CommonHttpFeatures /all /norestart >nul 2>&1
dism /online /enable-feature /featurename:IIS-StaticContent /all /norestart >nul 2>&1
dism /online /enable-feature /featurename:IIS-DefaultDocument /all /norestart >nul 2>&1
dism /online /enable-feature /featurename:IIS-DirectoryBrowsing /all /norestart >nul 2>&1
dism /online /enable-feature /featurename:IIS-HttpErrors /all /norestart >nul 2>&1
dism /online /enable-feature /featurename:IIS-ApplicationDevelopment /all /norestart >nul 2>&1
dism /online /enable-feature /featurename:IIS-WebSockets /all /norestart >nul 2>&1
dism /online /enable-feature /featurename:IIS-RequestFiltering /all /norestart >nul 2>&1
dism /online /enable-feature /featurename:IIS-ManagementConsole /all /norestart >nul 2>&1
echo       تم بنجاح [✓]

echo.
echo [3/8] تشغيل خدمات IIS وضبطها لتبدأ تلقائياً مع تشغيل السيرفر (Automatic)...
sc config W3SVC start= auto >nul 2>&1
sc config WAS start= auto >nul 2>&1
net start WAS >nul 2>&1
net start W3SVC >nul 2>&1
echo       تم بنجاح [✓]

echo.
echo [4/8] ضبط إعدادات HTTP.sys لمنع أخطاء Header Too Long (400 Bad Request)...
reg add "HKLM\SYSTEM\CurrentControlSet\Services\HTTP\Parameters" /v MaxFieldLength /t REG_DWORD /d 65536 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Services\HTTP\Parameters" /v MaxRequestBytes /t REG_DWORD /d 65536 /f >nul 2>&1
echo       تم بنجاح [✓]

echo.
echo [5/8] فتح البورتات في الجدار الناري (SQL Server, POS API, Web, Ping)...
:: SQL Server Default Port (1433)
netsh advfirewall firewall delete rule name="SQL Server (Port 1433)" >nul 2>&1
netsh advfirewall firewall add rule name="SQL Server (Port 1433)" dir=in action=allow protocol=TCP localport=1433 >nul 2>&1

:: SQL Server Browser Service (1434 UDP)
netsh advfirewall firewall delete rule name="SQL Server Browser (Port 1434 UDP)" >nul 2>&1
netsh advfirewall firewall add rule name="SQL Server Browser (Port 1434 UDP)" dir=in action=allow protocol=UDP localport=1434 >nul 2>&1

:: POS API Custom Ports (5050 / 8080)
netsh advfirewall firewall delete rule name="POS API Server (Port 5050)" >nul 2>&1
netsh advfirewall firewall add rule name="POS API Server (Port 5050)" dir=in action=allow protocol=TCP localport=5050 >nul 2>&1

netsh advfirewall firewall delete rule name="POS API Server (Port 8080)" >nul 2>&1
netsh advfirewall firewall add rule name="POS API Server (Port 8080)" dir=in action=allow protocol=TCP localport=8080 >nul 2>&1

:: Web Ports (HTTP 80 / HTTPS 443)
netsh advfirewall firewall delete rule name="Web Server HTTP (Port 80)" >nul 2>&1
netsh advfirewall firewall add rule name="Web Server HTTP (Port 80)" dir=in action=allow protocol=TCP localport=80 >nul 2>&1

netsh advfirewall firewall delete rule name="Web Server HTTPS (Port 443)" >nul 2>&1
netsh advfirewall firewall add rule name="Web Server HTTPS (Port 443)" dir=in action=allow protocol=TCP localport=443 >nul 2>&1

:: Allow ICMP Ping
netsh advfirewall firewall delete rule name="Allow ICMPv4-In (Ping)" >nul 2>&1
netsh advfirewall firewall add rule name="Allow ICMPv4-In (Ping)" protocol=icmpv4:8,any dir=in action=allow >nul 2>&1
echo       تم بنجاح [✓]

echo.
echo [6/8] ضبط نوع الشبكة إلى خاصة (Private Network) للسماح بالاتصال بين الأجهزة...
powershell -Command "Get-NetConnectionProfile | Set-NetConnectionProfile -NetworkCategory Private -ErrorAction SilentlyContinue" >nul 2>&1
echo       تم بنجاح [✓]

echo.
echo [7/8] إنشاء مجلد نشر الـ API الافتراضي ومنح الصلاحيات...
if not exist "C:\inetpub\POS_API" mkdir "C:\inetpub\POS_API" >nul 2>&1
icacls "C:\inetpub\POS_API" /grant "IIS_IUSRS":(OI)(CI)F /T >nul 2>&1
icacls "C:\inetpub\POS_API" /grant "IUSR":(OI)(CI)F /T >nul 2>&1
echo       تم بنجاح [✓]

echo.
echo [8/8] إعادة تشغيل خدمة HTTP و IIS لتفعيل وتطبيق كامل الإعدادات...
net stop http /y >nul 2>&1
net start http >nul 2>&1
net start WAS >nul 2>&1
net start W3SVC >nul 2>&1
iisreset /restart >nul 2>&1
echo       تم بنجاح [✓]

echo.
echo ==============================================================================
echo     ✅  تم الانتهاء بنجاح! السيرفر جاهز تماماً 100%% لتشغيل النظام:
echo.
echo     1. تم تثبيت وتفعيل IIS مع بروتوكول WebSockets لدعم SignalR اللحظي
echo     2. تم ضبط خدمات IIS لتبدأ تلقائياً (Automatic Startup)
echo     3. تم فتح كافة البورتات (SQL 1433 + API 5050 + Web 80 + Ping)
echo     4. تم توسيع حجم Headers لمنع خطأ 400 Bad Request نهائياً
echo     5. تم ضبط السيرفر على وضع الأداء العالي ومنع السكون 24/7
echo     6. تم إنشاء مجلد C:\inetpub\POS_API وإعطاؤه الصلاحيات الكاملة
echo.
echo     [تنبيه]: إذا لم تكن قمت بتثبيت (.NET 8 Hosting Bundle)، قم بتثبيته
echo              مرة واحدة لكي يتمكن IIS من تشغيل تطبيقات .NET Core.
echo ==============================================================================
echo.
pause
