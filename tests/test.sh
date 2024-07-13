#!/bin/bash

function test
{
    filename=$1
    target=$2
    Azurite "$filename.azur" -o "$filename.out" -t "$target" > /dev/null
    # exit 1 if the files are different
    diff "$filename.out.$target" "$filename.expected.$target"
    if [ $? -eq 0 ]; then
        echo "Test $filename passed"
    else
        echo "Test $filename failed"
        exit 1
    fi
}

TEST_DIR=$(dirname "$0")
test "${TEST_DIR}/test1" "azur"
test "${TEST_DIR}/test2" "azullvm"
test "${TEST_DIR}/test2" "azurir"
