@echo off

setlocal enabledelayedexpansion enableextensions

if exist "%WINDIR%\System32\wevtutil.exe" (

    if exist logs (
        rmdir /S /Q logs
    )

    mkdir logs
    pushd logs

    wevtutil el > logs.txt

    for /f "tokens=*" %%A in ('wevtutil el') do (
        set XML_FILENAME=%%A
        set XML_FILENAME=!XML_FILENAME:/=--!
        wevtutil gl "%%A" /f:xml > "!XML_FILENAME!.xml"
    )

    popd
)

endlocal