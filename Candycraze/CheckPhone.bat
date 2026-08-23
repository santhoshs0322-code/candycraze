@echo off
set PATH=%PATH%;C:\Users\sandy\AppData\Local\Android\Sdk\platform-tools
echo Checking connected Android devices...
adb devices -l
echo.
echo If your device shows as 'unauthorized', check your phone screen and tap Allow.
echo If no device shows, make sure USB Debugging is enabled.
pause
