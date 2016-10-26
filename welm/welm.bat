@echo off

setlocal enabledelayedexpansion enableextensions

if exist "%WINDIR%\System32\wevtutil.exe" (

    if exist wevtutil (
        rmdir /S /Q wevtutil
    )

    mkdir wevtutil
    pushd wevtutil

    mkdir logs
    pushd logs

    wevtutil.exe el > logs.txt
    move logs.txt ..\ >nul

    for /f "tokens=*" %%A in ('wevtutil.exe el') do (
        set XML_FILENAME=%%A
        set XML_FILENAME=!XML_FILENAME:/=--!
        wevtutil.exe gl "%%A" /f:xml > "!XML_FILENAME!.xml"
    )

    popd

    mkdir publishers
    pushd publishers

    wevtutil.exe ep > publishers.txt
    move publishers.txt ..\ >nul

    for /f "tokens=*" %%A in ('wevtutil.exe ep') do (
        set XML_FILENAME=%%A
        set XML_FILENAME=!XML_FILENAME:/=--!
        wevtutil.exe gp "%%A" /ge /gm:true /f:xml > "!XML_FILENAME!.xml"
    )

    popd

    popd
)

set WELMPATH=%~dp0%\welm.exe

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