@ECHO off

set "wpath=C:\Users\micel\RiderProjects\LC_Lives\LC_Lives\"
set "manpath=%wpath%bin\Release\netstandard2.1\manifest.json"
for /F "delims==" %%# in ('type LC_Lives.csproj ^| grep "<Version>.*</Version>"') do set "a=%%#"

if EXIST "%wpath%bin\Release\netstandard2.1" (
	@REM mv "%wpath%bin\Release\netstandard2.1" "%wpath%bin\Release\netstandard2.1"
	rm "%wpath%bin\Release\netstandard2.1\slenered.LC_Lives.deps.json"
	cp "%wpath%icon.png" "%wpath%bin\Release\netstandard2.1"
	cp "%wpath%README.md" "%wpath%bin\Release\netstandard2.1"

	for /F "delims=" %%# in (GEN_manifest.json) do (
		if "%%#" equ "  "version_number": "%%%%"," (
			echo   "version_number": "%a:~17,-10%", >> %manpath%
		) else (
			echo %%# >> %manpath%
		)
	)

	7z a -aoa -tzip "C:\Users\micel\RiderProjects\LC_Lives\LC_Lives\bin\Release\slenered.LC_Lives.%a:~17,-10%.zip" "C:\Users\micel\RiderProjects\LC_Lives\LC_Lives\bin\Release\netstandard2.1\*"
rmdir /s /q "%wpath%bin\Release\netstandard2.1"
)
