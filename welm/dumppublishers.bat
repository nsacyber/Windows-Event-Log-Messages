@echo off

setlocal enabledelayedexpansion enableextensions

set WEVTPATH=%WINDIR%\System32\wevtutil.exe

set PUBLISHERS=publishers.txt

set PUBSERRORLOG=publishers_errors.txt

if exist "%WEVTPATH%" (

    if exist publishers (
        rmdir /S /Q publishers
    )

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
)

endlocal