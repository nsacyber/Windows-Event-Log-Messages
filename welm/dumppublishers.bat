@echo off

setlocal enabledelayedexpansion enableextensions

if exist "%WINDIR%\System32\wevtutil.exe" (

    if exist publishers (
        rmdir /S /Q publishers
    )

    mkdir publishers
    pushd publishers

    wevtutil ep > publishers.txt

    for /f "tokens=*" %%A in ('wevtutil ep') do (
        set XML_FILENAME=%%A
        set XML_FILENAME=!XML_FILENAME:/=--!
        wevtutil gp "%%A" /ge /gm:true /f:xml > "!XML_FILENAME!.xml"
    )

    popd
)

endlocal