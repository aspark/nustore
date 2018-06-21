# NuStore
Download nuget packages which declared in the *.deps.json, and save it to store folder, for minify .net core publish size

## Install
	dotnet tool install -g NuStore

## Update
	dotnet tool update -g Nustore

## Uninstall
	dotnet tool uninstall -g NuStore

## Usage
By default `nustore restore` will load the deps file from current folder, 
and save the packages to /usr/local/share/dotnet/store 
on macOS/Linux and C:/Program Files/dotnet/store on Windows

	nustore verb [options]
get help info via `nustore --help`


## verbs
1. restore
2. minify

## restore options

 opt           | desc
-------------- | -----
`-p` `--deps` | deps file. default is *.deps.json in current directory
`-d` `--dir` | diretory packages stored(typically at /usr/local/share/dotnet/store on macOS/Linux and C:/Program Files/dotnet/store on Windows).
`-f` `--force` | override exists packages
`--nuget` | set nuget resouce api url. default: https://api.nuget.org/v3/index.json
`-e` `--exclude` | skip packages, support regex. seprate by semicolon for mutiple
`-s` `--special` | restore special packages, support regex. seprate by semicolon for mutiple
`--runtime` | .net core runtime version, the defaut value set by deps file, for excample netcoreapp2.0/netcoreapp2.1
`--arch` | x64/x86, by defalut this value is resolved from platform attribute which declared in deps file 
`--verbosity` | show detail log
`--help` | get help info

### example

Use e:/nustore/test.deps.json file to restore packages to e:/nustore. exclude all packages which start with microsoft

	nustore restore --dir="e:/nustore" --deps="e:/nustore/test.deps.json" --exclude="^microsoft.*;^System.*" -s "Microsoft\.Extensions.Logging"
