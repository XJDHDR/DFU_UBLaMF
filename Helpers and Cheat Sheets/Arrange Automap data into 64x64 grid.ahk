#NoEnv
ListLines, Off


Loop, Files, %A_ScriptDir%\*.json, F
{
	sFileList .= A_LoopFileName "`n"
}

Loop, Parse, sFileList, `n
{
	bAutomapPortionFound := false
	iAutomapLinesIterated := 0
	sNewAutomapData := ""
	Loop, Read, %A_LoopField%, %A_LoopField%.NewFile
	{
		If (InStr(A_LoopReadLine, "AutoMapData")) {
			; Found the portion that contains the Automap data. Activate the other conditionals.
			bAutomapPortionFound := true
			FileAppend, %A_LoopReadLine%`r`n
		}
		Else If ((InStr(A_LoopReadLine, "],")) && (bAutomapPortionFound == true)) {
			; Reached the end of the portion that contains the Automap data.
			bAutomapPortionFound := false
			FileAppend, %sNewAutomapData%`r`n
			FileAppend, %A_LoopReadLine%`r`n
		}
		Else If (bAutomapPortionFound == true) {
			If (iAutomapLinesIterated == 0) {
				iAutomapLinesIterated := 1
				sNewlinesRemoved := RegExReplace(A_LoopReadLine, "`r`n")
				sTabsRemovedAsWell := RegExReplace(sNewlinesRemoved, "`t")
				sPaddedString := "    " . sTabsRemovedAsWell
				sNewAutomapData .= "`t`t`t`t" . SubStr(sPaddedString, -3, 4)
			}
			Else If (iAutomapLinesIterated >= 64) {
				iAutomapLinesIterated := 1
				sNewlinesRemoved := RegExReplace(A_LoopReadLine, "`r`n")
				sTabsRemovedAsWell := RegExReplace(sNewlinesRemoved, "`t")
				sPaddedString := "    " . sTabsRemovedAsWell
				sNewAutomapData .= "`r`n`t`t`t`t" . SubStr(sPaddedString, -3, 4)
			}
			Else {
				iAutomapLinesIterated += 1
				sNewlinesRemoved := RegExReplace(A_LoopReadLine, "`r`n")
				sTabsRemovedAsWell := RegExReplace(sNewlinesRemoved, "`t")
				sPaddedString := "    " . sTabsRemovedAsWell
				sNewAutomapData .= SubStr(sPaddedString, -3, 4)
			}
		}
		Else {
			; This line is outside the portion that contains the Automap data. Just write it to the new file as-is.
			FileAppend, %A_LoopReadLine%`r`n
		}
	}
	FileMove, %A_LoopField%.NewFile, %A_LoopField%, true
}
