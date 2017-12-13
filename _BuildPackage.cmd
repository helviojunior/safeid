@echo off
set hour=%time:~0,2%
if "%hour:~0,1%" == " " set hour=0%hour:~1,1%
echo hour=%hour%
set min=%time:~3,2%
if "%min:~0,1%" == " " set min=0%min:~1,1%
echo min=%min%
set secs=%time:~6,2%
if "%secs:~0,1%" == " " set secs=0%secs:~1,1%
echo secs=%secs%

set year=%date:~-4%
echo year=%year%
set month=%date:~3,2%
if "%month:~0,1%" == " " set month=0%month:~1,1%
echo month=%month%
set day=%date:~0,2%
if "%day:~0,1%" == " " set day=0%day:~1,1%
echo day=%day%

set datetimef=%year%%month%%day%_%hour%%min%%secs%

rmdir .\PKGTemp /s /q
mkdir PKGTemp
cd PKGTemp
mkdir IAMProxy
mkdir IAMMultiProxy
mkdir IAMServer
mkdir IAMInstall
cd IAMInstall
cd..
mkdir IAMCas
cd IAMCas
mkdir App_Data
cd App_Data
mkdir plugins
cd ..
cd ..
cd IAMServer
mkdir x64
mkdir x86
mkdir plugins
mkdir Web
cd web
mkdir _data
mkdir code_plugins
cd ..
cd ..
rem mkdir IAMDatabaseService
rem cd IAMDatabaseService
rem mkdir MongoDB
rem cd ..

echo "Copiando Plugins"
xcopy ..\bin\IAMPlugins\*.dll .\IAMServer\plugins
rem xcopy ..\bin\IAMPlugins\*.dll E:\IAMServer\plugins /y

echo "Copiando IAMMultiProxy"
xcopy ..\bin\IAMMultiProxy\*.exe .\IAMMultiProxy
xcopy ..\bin\IAMMultiProxy\*.dll .\IAMMultiProxy

echo "Copiando IAMProxy"
xcopy ..\bin\IAMProxy\*.exe .\IAMProxy
xcopy ..\bin\IAMProxy\*.dll .\IAMProxy

rem exclui arquivos lixo gerados pelo visual studio
del *".vshost.exe"* /s /q

echo "Gera arquivos zip de proxy e multproxy"
echo IAMMultiProxy.exe --install > .\IAMMultiProxy\_Install.cmd
echo IAMMultiProxy.exe --uninstall > .\IAMMultiProxy\_Uninstall.cmd
..\7za.exe a .\IAMServer\web\_data\multproxy.zip .\IAMMultiProxy\*.*
..\7za.exe a .\IAMServer\web\_data\proxy.zip .\IAMProxy\*.*

echo "Copiando IAMServer"
xcopy ..\bin\IAMServer\*.exe .\IAMServer
xcopy ..\bin\IAMServer\*.dll .\IAMServer
xcopy ..\bin\IAMServer\*.png .\IAMServer
xcopy ..\bin\IAMServer\*.jpg .\IAMServer
xcopy ..\bin\IAMServer\x64\*.\ .\IAMServer\x64
xcopy ..\bin\IAMServer\x86\*.* .\IAMServer\x86

echo "Copiando IAMInstall"
xcopy ..\bin\IAMInstall\*.exe .\IAMInstall
xcopy ..\bin\IAMInstall\*.dll .\IAMInstall
xcopy ..\certificates\*.pfx .\IAMInstall\
xcopy ..\certificates\*.cer .\IAMInstall\

echo "Copiando IAM Web Server"
rem xcopy ..\IAMWebServer\IAMWebServer\obj\Release\Package\PackageTmp\*.* .\IAMServer\Web /E
xcopy ..\IAMWebServer\IAMWebServer\obj\Release\Package\PackageTmp\*.* .\IAMServer\Web /E /y
del .\IAMServer\Web\*.config /q
xcopy ..\bin\IAMCodePlugins\*.dll .\IAMServer\web\code_plugins /E /y
xcopy ..\bin\IAMCodePlugins\*.exe .\IAMServer\web\code_plugins /E /y
del .\IAMServer\web\code_plugins\IAMCodeManager.dll /q
del .\IAMServer\web\code_plugins\Test.exe /q
del *".vshost.exe"* /s /q
xcopy .\IAMServer\web\code_plugins\*.dll ..\IAMWebServer\IAMWebServer\code_plugins /E /y
xcopy .\IAMServer\web\code_plugins\*.exe ..\IAMWebServer\IAMWebServer\code_plugins /E /y


echo "Copiando IAM CAS Web Server"
rem xcopy ..\IAMWebCas\IAMWebCas\obj\Release\Package\PackageTmp\*.* .\IAMCas /E /y
xcopy ..\IAMWebCas\IAMWebCas\obj\Release\Package\PackageTmp\*.* .\IAMCas /E /y
xcopy ..\bin\IAMWebCas\*.dll .\IAMCas\App_Data\plugins\ /y
xcopy ..\bin\IAMWebCas\*.dll .\IAMCas\App_Data\plugins\ /y
del .\IAMCas\*.config /q

Rem atualiza o diretório local de debug e teste dos plugins
xcopy .\IAMServer\plugins\*.dll E:\IAMServer\Plugins\ /y

rem exclui arquivos lixo gerados pelo visual studio
del *".vshost.exe"* /s /q
del *".pdb"* /s /q

Rem reestrutura o proxy e multiproxy
ren IAMProxy IAMProxy_tmp
ren IAMMultiProxy IAMProxy
cd IAMProxy
mkdir proxies
cd proxies
cd ..
cd ..
move /y .\IAMProxy_tmp .\IAMProxy\proxies\_base

