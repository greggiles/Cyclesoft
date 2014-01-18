@echo off
:: 
:: Batch script to create and delete port rules in Windows 7 firewall
:: Created by Péter Barabás (barabas_p(at)yahoo...)
::
setlocal
 
:: In the next two line, you can set the parameters for script:
set PORTNUMBER=8080
set RULENAME="WorkoutTracker"
 
:: Using command line parameter for selecting process:
:: if "%1"=="/o" call :_OpeningPort
:: if "%1"=="/d" call :_DelRules
:: if "%1"=="" echo No parameter. Exiting.
:: goto :EOF
 
:_OpeningPort
:: Opening Port on firewall:
netsh advfirewall firewall show rule name=%RULENAME% >nul
if not ERRORLEVEL 1 (
rem Rule %RULENAME% already exist.
echo Firewall rule already exists, let's move on!
) else (
echo Rule %RULENAME% not exist. Creating...
netsh advfirewall firewall add rule name=%RULENAME% dir=in action=allow protocol=tcp localport=%PORTNUMBER% remoteip=LocalSubnet profile=private
)
netsh http show urlacl url=http://*:8080/ >nul
if not ERRORLEVEL 1 (
rem URLACL for WorkoutTracker already exist.
echo URLACL already exists, let's move on!
) else (
echo Adding URLACL for the website! Creating...
netsh advfirewall firewall add rule name="WorkoutTracker" dir=in protocol=tcp localport=8080 profile=private remoteip=localsubnet action=allow
)
echo .
echo .
echo Starting web service. 
echo Please close this window after finished using external devices
echo .
echo .
echo PRESS Q to CLOSE WHEN DONE WITH WORKOUT TRACKER.
"c:\Program Files\IIS Express\iisexpress.exe" /config:%1/applicationhost.config 

echo Closing ... removing Firewall rule. 

:_DelRules
:: Deleting enabled port:
echo 
netsh advfirewall firewall show rule name=%RULENAME% >nul
if not ERRORLEVEL 1 (
echo Rule %RULENAME% exist. Deleting...
netsh advfirewall firewall delete rule name=%RULENAME% protocol=tcp localport=%PORTNUMBER%
) else (
echo Rule %RULENAME% does not exist. 
)

goto :EOF

