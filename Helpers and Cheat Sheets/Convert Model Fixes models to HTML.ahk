


Loop, Read, %A_ScriptDir%\input.txt
{
	If ((RegExMatch(A_LoopReadLine, "^[a-zA-Z]+$") == 0) && (RegExMatch(A_LoopReadLine, "^[0-9]+$") != 0))
	{
		bBlankLineFound := False
		iModelNumber := A_LoopReadLine
		sModelNumberFiveDigits := SubStr("00000" . A_LoopReadLine, -4)
	}
	Else If (InStr(A_LoopReadLine, "Description: ") != 0)
	{
		arDescriptionParts := StrSplit(A_LoopReadLine, ":", " `t", 2)
		sDescription := arDescriptionParts[2]
		arDescriptionParts := ""
	}
	Else If (InStr(A_LoopReadLine, "e.g.") != 0)
	{
		arExampleLocationParts := StrSplit(A_LoopReadLine, ".", " `t", 3)
		sExampleLocation := StrReplace(arExampleLocationParts[3], ")")
		arExampleLocationParts := ""
	}
	Else If (InStr(A_LoopReadLine, "-") != 0)
	{
		arBugsFixedParts := StrSplit(A_LoopReadLine, "-", " `t", 2)
		sBugsFixed .= "								<li>" . arBugsFixedParts[2] . "</li>`r`n"
		arBugsFixedParts := ""
	}
	Else If (InStr(A_LoopReadLine, "") != 0)
	{
		If (bBlankLineFound != True)
		{
			bBlankLineFound := True
			sOutputContents .= "					<tr>`r`n"
			sOutputContents .= "						<td class=""data-table-cell-left""><a href=""https://github.com/XJDHDR/DFU_UBLaMF/blob/master/3%20-%20Exported%20OBJs%20%26%20FBXs/" . sModelNumberFiveDigits . "/" . iModelNumber . ".obj"" rel=""external"" onclick=""target='_blank'"">" . iModelNumber . "</a> <sup><span title=""" . sDescription . """>desc</span></sup></td>`r`n"
			sOutputContents .= "						<td class=""data-table-cell-mid""><a href=""https://github.com/XJDHDR/DFU_UBLaMF/tree/master/5%20-%20Non%20Prefabs/"" rel=""external"" onclick=""target='_blank'"">Block</a> <sup><span title=""" . sExampleLocation . """>e.g. loc.</span></sup></td>`r`n"
			sOutputContents .= "						<td class=""data-table-cell-mid"">2021.01.04</td>`r`n"
			sOutputContents .= "						<td class=""data-table-cell-right"" style=""text-align: left;"">`r`n"
			sOutputContents .= "							<ul class=""minimal-padding"">`r`n"
			sOutputContents .= sBugsFixed
			sOutputContents .= "							</ul>`r`n"
			sOutputContents .= "						</td>`r`n"
			sOutputContents .= "					</tr>`r`n"
			sBugsFixed := ""
		}
	}
}

FileAppend, %sOutputContents%, Output.txt, UTF-8

ExitApp
