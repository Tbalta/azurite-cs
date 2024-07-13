#!/bin/bash

function test
{
    filename=$1
    target=$2
    stdlib_path=$3
    Azurite "$filename.azur" -o "$filename.out" -t "$target" --stdlib "$stdlib_path" > /dev/null
    # exit 1 if the files are different
    diff "$filename.out.$target" "$filename.expected.$target"
    if [ $? -eq 0 ]; then
        echo "Test $filename passed"
    else
        echo "Test $filename failed"
        exit 1
    fi
}


stdlib_path="$HOME/.azurite"
if [ ! -z "$1" ]; then
    stdlib_path=$1
fi

TEST_DIR=$(dirname "$0")
test "${TEST_DIR}/test1" "azur" "$stdlib_path"
test "${TEST_DIR}/test2" "azullvm" "$stdlib_path"
test "${TEST_DIR}/test2" "azurir" "$stdlib_path"
