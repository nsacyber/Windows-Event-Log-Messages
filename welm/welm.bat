@echo off

setlocal enabledelayedexpansion enableextensions

set WEVTPATH=%WINDIR%\System32\wevtutil.exe

set LOGS=logs.txt
set PUBLISHERS=publishers.txt

set LOGSERRORLOG=logs_errors.txt
set PUBSERRORLOG=publishers_errors.txt

if exist "%WEVTPATH%" (

    if exist wevtutil (
        rmdir /S /Q wevtutil
    )

    mkdir wevtutil
    pushd wevtutil

    mkdir logs
    pushd logs

    "%WEVTPATH%" el >%LOGS%
    move %LOGS% ..\ >nul

    for /f "tokens=*" %%A in ('"%WEVTPATH%" el') do (
        set XML_FILENAME=%%A
        set XML_FILENAME=!XML_FILENAME:/=--!
        "%WEVTPATH%" gl "%%A" /f:xml >"!XML_FILENAME!.xml" 2>>%LOGSERRORLOG%
		
        if !errorlevel! neq 0 (
            echo wevtutil returned error code !errorlevel! when running wevutil gl on '%%A'. see above line for details. >>%LOGSERRORLOG%
        )
    )
	
    move %LOGSERRORLOG% ..\ >nul

    popd

    mkdir publishers
    pushd publishers

    "%WEVTPATH%" ep >%PUBLISHERS%
    move %PUBLISHERS% ..\ >nul

    for /f "tokens=*" %%A in ('"%WEVTPATH%" ep') do (
        set XML_FILENAME=%%A
        set XML_FILENAME=!XML_FILENAME:/=--!
        "%WEVTPATH%" gp "%%A" /ge /gm:true /f:xml >"!XML_FILENAME!.xml" 2>>%PUBSERRORLOG%
		
        if !errorlevel! neq 0 (
            echo wevtutil returned error code !errorlevel! when running wevutil gp on '%%A'. see above line for details. >>%PUBSERRORLOG%
        )
    )
	
    move %PUBSERRORLOG% ..\ >nul	

    popd

    popd
)

set WELMPATH=.

if exist "%~dp0%\welm.exe" (
    set WELMPATH=%~dp0%\welm.exe
)

if exist "%~dp0%\WelmConsole.exe" (
    set WELMPATH=%~dp0%\WelmConsole.exe
)

if exist welm (
    rmdir /S /Q welm
)

mkdir welm
pushd welm

"%WELMPATH%" -e -f all
"%WELMPATH%" -p -f all
"%WELMPATH%" -l -f all

popd

move *.txt .\welm >nul

endlocal