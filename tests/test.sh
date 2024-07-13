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

test "tests/test1" "azur"
test "tests/test2" "azullvm"
test "tests/test2" "azurir"
