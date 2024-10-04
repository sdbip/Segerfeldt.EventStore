#!/bin/bash

# Ansi color code variables
red="\033[0;91m"
green="\033[0;92m"
yellow="\033[0;93m"
reset="\033[0m"

# Result codes
failure=1
success=0

function test() {
    dir=$1
	changed=$(git diff --staged --name-only $dir)
	changed_unstaged=$(git diff --name-only $dir)

	# if changed is not empty there are changes for this project
	if [ -n "$changed" ]; then
		echo -e "$dir has changed; tests need to run"
        staged_changes="$staged_changes $changed"

		# if the last command (dotnet test) failed
		if [[ -n $changed_unstaged ]]; then
			untrusted="$untrusted $dir"
		fi
	fi
}

proj_files=$(find . -name "*.csproj")
for path in $proj_files; do
    test $(dirname $path)
done
test ./Segerfeldt.EventStore.Shared

if [[ -n $staged_changes ]]; then
    dotnet test
    result=$?
fi

echo ""
# if result has not been set (ie. no changes were staged)
if [[ -z ${result} ]]; then
	echo -e "🫣 ${yellow}Tests don't need to run.${reset} ⏏️"
# if result contains "1" at least once (ie. tests for one of the solutions failed)
elif [[ $result == *1* ]]; then
	echo -e "🤕 ${red}Tests failed. ${yellow}Not committing.${reset} ❌"
# if untrusted is not empty (ie. at least one set of successful tests used code that has not been staged)
elif [[ -n $untrusted ]]; then
	echo -e "🤥 ${red}There are unstaged changes in [$untrusted]. ${yellow}Not committing.${reset} ⚠️"
fi
echo ""

# if result is not empty
if [[ -n $result ]]; then
	# if result contains at least one failure or untrusted is not empty
	# return a failure code to stop the commit from completing
	[[ $result == *1* ]] || [[ -n $untrusted ]] && exit $failure
fi

echo -e "✅ ${green}All is good. Committing.${reset} ➡️"
echo ""
