# NuStore
Download nuget packages which declared in the *.deps.json, and save it to store folder, for minify .net core publish size

## Install
	dotnet tool install -g NuStore

## Uninstall
	dotnet tool uninstall -g NuStore

## Usage
By default `nustore` will load the deps file from current folder, 
and save the packages to /usr/local/share/dotnet/store 
on macOS/Linux and C:/Program Files/dotnet/store on Windows

	nustore [options]
get help info via `nustore --help`

## options

opt | desc
--- | ---
`-p` `--deps` | deps file. default is *.deps.json in current directory
`-d` `--dir` | diretory packages stored(typically at /usr/local/share/dotnet/store on macOS/Linux and C:/Program Files/dotnet/store on Windows).
`-f` `--force` | override exists packages
`--nuget` | set nuget resouce api url. default: https://api.nuget.org/v3/index.json
`-e` `--exclude` | skip packages, support regex. seprate by semicolon for mutiple
`-s` `--special` | restore special packages, support regex. seprate by semicolon for mutiple
`--help` | get help info
