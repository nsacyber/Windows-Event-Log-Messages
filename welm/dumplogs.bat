@echo off

setlocal enabledelayedexpansion enableextensions

set WEVTPATH=%WINDIR%\System32\wevtutil.exe

set LOGS=logs.txt

set LOGSERRORLOG=logs_errors.txt

if exist "%WEVTPATH%" (

    if exist logs (
        rmdir /S /Q logs
    )

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
)

endlocal