function test
{
    filename=$1
    Azurite -f "$filename.azur" -s "$filename.out" -t azur > /dev/null
    # exit 1 if the files are different
    diff "$filename.out.azur" "$filename.expected"
    if [ $? -eq 0 ]; then
        echo "Test $filename passed"
    else
        echo "Test $filename failed"
        exit 1
    fi
}

test "test1"