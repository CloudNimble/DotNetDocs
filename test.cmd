@echo off
REM Simple test runner wrapper for CloudNimble.DotNetDocs
REM Usage: test [options]
REM Examples:
REM   test                    - Run all Core tests
REM   test -failed            - Show only failed tests
REM   test DocNamespace       - Run tests with DocNamespace in the name
REM   test -list              - List all tests
REM   test -all               - Run all test projects

setlocal enabledelayedexpansion

set FILTER=
set PROJECT=Core
set CONFIG=Debug
set EXTRA_ARGS=
set FAILED_ONLY=

:parse_args
if "%~1"=="" goto run_tests

if /i "%~1"=="-failed" (
    set FAILED_ONLY=-FailedOnly
    shift
    goto parse_args
)

if /i "%~1"=="-all" (
    set PROJECT=All
    shift
    goto parse_args
)

if /i "%~1"=="-list" (
    set EXTRA_ARGS=-List
    shift
    goto parse_args
)

if /i "%~1"=="-release" (
    set CONFIG=Release
    shift
    goto parse_args
)

if /i "%~1"=="-nobuild" (
    set EXTRA_ARGS=!EXTRA_ARGS! -NoBuild
    shift
    goto parse_args
)

if /i "%~1"=="-help" (
    echo CloudNimble.DotNetDocs Test Runner
    echo.
    echo Usage: test [filter] [options]
    echo.
    echo Options:
    echo   -failed     Show only failed test output
    echo   -all        Run all test projects
    echo   -list       List tests without running
    echo   -release    Use Release configuration
    echo   -nobuild    Skip building before tests
    echo   -help       Show this help
    echo.
    echo Examples:
    echo   test                         Run all Core tests
    echo   test DocNamespace            Run tests matching "DocNamespace"
    echo   test -failed                 Show only failed tests
    echo   test -all -failed            Run all projects, show only failures
    goto :eof
)

REM Assume anything else is a filter
set FILTER=%~1
shift
goto parse_args

:run_tests
if not "%FILTER%"=="" (
    REM Try to determine the best filter format
    REM If it contains =, assume it's already a proper filter
    echo %FILTER% | findstr /C:"=" >nul
    if errorlevel 1 (
        REM No = found, so create a filter
        REM Check if it looks like a test method name or class name
        set FILTER=FullyQualifiedName~%FILTER%
    )
)

powershell -ExecutionPolicy Bypass -File "%~dp0run-tests.ps1" -Project %PROJECT% -Configuration %CONFIG% -Filter "%FILTER%" %FAILED_ONLY% %EXTRA_ARGS%
exit /b %ERRORLEVEL%