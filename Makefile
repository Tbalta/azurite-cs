

debug:
	dotnet publish ./src -o /bin -c Debug

build:
	dotnet publish ./src -o ./bin

install: build
	cp ./src/bin/Debug/netcoreapp6.0/publish/Azurite ${HOME}/.azurite/bin
	cp ./src/bin/Debug/netcoreapp6.0/publish/Azurite.dll ${HOME}/.azurite/bin
	cp ./src/bin/Debug/netcoreapp6.0/publish/Azurite.runtimeconfig.json ${HOME}/.azurite/bin

test: clean-tests
	./tests/test.sh

clean-tests:
	$(RM) tests/*.out.*
	$(RM) tests/tutorial/*.out.*

clean: clean-tests
	dotnet clean ./src
