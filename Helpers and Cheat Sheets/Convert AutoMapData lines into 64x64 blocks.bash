#!/bin/bash

intermediate=$(
	while read line
	do
		printf '%4s\n' "$line"
	done < "old-map.txt"
)

echo "$intermediate" | paste -d' ' - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - > "new-map.txt"
