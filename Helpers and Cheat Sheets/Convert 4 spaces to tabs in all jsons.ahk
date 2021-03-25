#NoEnv
ListLines, Off


Loop, Files, %A_ScriptDir%\*.json, F
{
	sFileList .= A_LoopFileName "`n"
}

Loop, Parse, sFileList, `n
{
	FileRead, sFileContents, %A_LoopField%
	sNewFileContents := StrReplace(sFileContents, "    ", "`t")
	FileDelete, %A_LoopField%
	FileAppend, %sNewFileContents%, %A_LoopField%, UTF-8
}
