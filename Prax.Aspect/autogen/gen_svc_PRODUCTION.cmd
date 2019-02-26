REM Remember to run from the Visual Studio command prompt so wsdl.exe is in the path
@ECHO OFF
if "%~1"=="" goto missing
if "%~2"=="" goto missing
wsdl https://myaspect.net/webservice/aspectws/wsdl11  /u:%1 /p:%2
goto end

:missing
echo Remember to specify username and password as command line args

:end