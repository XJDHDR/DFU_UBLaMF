


Loop, Read, %A_ScriptDir%\input.txt
{
	If (InStr(A_LoopReadLine, "location-") != 0)
	{
		bBlankLineFound := False
		arLocationNumberParts := StrSplit(A_LoopReadLine, "-", " `t")
		sLocationRegionNumber := arLocationNumberParts[2]
		sLocationSettlementNumber := arLocationNumberParts[3]
		arLocationParts := ""
	}
	Else If (InStr(A_LoopReadLine, "Location:") != 0)
	{
		arLocationNameParts := StrSplit(A_LoopReadLine, "-", " `t")
		sLocationRegionName := StrReplace(StrReplace(arLocationNameParts[2], "Location: "), " ", "%20")
		sLocationSettlementName := arLocationNameParts[3]
		arLocationNameParts := ""
	}
	Else If (InStr(A_LoopReadLine, "Corrected Building Data") != 0)
	{
		arBuildingAndLineNumbersParts := StrSplit(A_LoopReadLine, "-", " `t")
		arBuildingNumberParts := StrSplit(arBuildingAndLineNumbersParts[2], "#", " `t")
		sBuildingNumber := arBuildingNumberParts[2]
		arLineNumberParts := StrSplit(arBuildingAndLineNumbersParts[3], " ", " `t")
		sLineNumber := StrReplace(arLineNumberParts[2], ")")
		arBuildingAndLineNumbersParts := ""
		arBuildingNumberParts := ""
		arLineNumberParts := ""
	}
	Else If (InStr(A_LoopReadLine, "") != 0)
	{
		If (bBlankLineFound != True)
		{
			bBlankLineFound := True
			sOutputContents .= "					<tr>`r`n"
			sOutputContents .= "						<td class=""data-table-cell-left"">" . sLocationSettlementNumber . "</td>`r`n"
			sOutputContents .= "						<td class=""data-table-cell-mid"">" . sLocationSettlementName . "</td>`r`n"
			sOutputContents .= "						<td class=""data-table-cell-mid"">2021.01.02</td>`r`n"
			sOutputContents .= "						<td class=""data-table-cell-mid""><a href=""https://github.com/XJDHDR/DFU_UBLaMF/blob/master/5%20-%20Non%20Prefabs/Locations/Location%20" . sLocationRegionNumber . "%20-%20" . sLocationRegionName . "/location-" . sLocationRegionNumber . "-" . sLocationSettlementNumber . ".json#L" . sLineNumber . """ rel=""external"" onclick=""target='_blank'"">" . sLineNumber . "</a></td>`r`n"
			sOutputContents .= "						<td class=""data-table-cell-mid"">" . sBuildingNumber . "</td>`r`n"
			sOutputContents .= "						<td class=""data-table-cell-mid"">FactionId: 22 → 0</td>`r`n"
			sOutputContents .= "						<td class=""data-table-cell-right""><a href=""#[1]"">[1]</a></td>`r`n"
			sOutputContents .= "					</tr>`r`n"
		}
	}
}

FileAppend, %sOutputContents%, Output.txt, UTF-8

ExitApp
