# 简介
.net core 部署有两种方式，一种是独立式部署（SCD），另一种是框架依赖式部署（FDD）。以SCD方式生成发布包时，dotnet会将所有依赖打包到一个文件夹内，并为应用程序生成可执行文件。以FDD方式部署的程序是不用安装.net core 运行时的，而在FDD模式下需要安装对应版本的.net core 运行时，两者的区别可参考：[.net core application deployment](https://docs.microsoft.com/en-us/dotnet/core/deploying/)。

用VS开发时，默认采用的是FDD模式，但将发布包部署到服务器上时可能会出现以下错误：
![assembly specified in the dependencies manifest was not found](http://km.oa.com/files/photos/pictures/201806/1529581432_61_w898_h67.png)
导致这个问题的原因是VS默认以 manifest的方式打包，对于本地Store中存在的公共包将不会被包含在发布包中，只会将其记录入*.deps.json文件中。所以当开发机与服务器Store目录中的包不一致时就会出现以上问题。一般的解决办法是将`PublishWithAspNetCoreTargetManifest`改为`false`，以停用manifest文件，但这会导致发布包变得无比巨大（虽然比SCD生成的文件少，但也会有200+的文件），当然我们也可以让编译服务器与部署服务器的Store环境一致或指定manifest文件来解决以上问题。对于本地Store的作用可参考： [Runtime package store](https://docs.microsoft.com/en-us/dotnet/core/deploying/runtime-store)

上文已经了解到Store的作用，其实我们手动将缺少的或未被包含在发布包的中Package拷到Store目录也可以解决问题。既然如此，NuStore作用就是：自动完成deps文件分析并从NuGet中下载依赖包，然后将其放入Store目录，这样发布包只需要包含不属于NuGet的包即可，最大化的减小发布包的体积。

# 安装
    dotnet tool install -g NuStore
# 更新
    dotnet tool update -g NuStore
# 卸载
    dotnet tool uninstall -g NuStore
# 用法
nustore verb [options]
直接使用`nustore restore`命令时，工具会加载当前目录中的*.deps.json文件，并将下载的包保存到 usr/local/share/dotnet/store目录（macOS/Linux）或 C:/Program Files/dotnet/store目录中（Windows）。使用`nustore --help`可获取更多帮助。

>.netcore2.1在centos下的安装目录为/usr/share/dotnet/，但微软官方文档为：usr/local/share/dotnet/store。所以在linux环境下使用时，请使用--dir参数指定为正确的目录。

直接使用`nustore minify`命令时，会将项目dll合并为一个，并将其与appsettings.json、runtimeconfig等文件放到./nustored文件夹中

# 参数
## Verbs
1. restore  下载所有依赖包
1. minify    精简当前发布包

## restore Options

选项 | 说明
------------ | -------------
`-p ` `--deps` | 指定deps文件。默认搜索当前目录中的*.deps.json文件
`-d`  `--dir` | 将包下载到指定的目录中。默认usr/local/share/dotnet/store目录（macOS/Linux）或 C:/Program Files/dotnet/store目录中（Windows）
`-f`  `--force` | 是否覆盖已下载的包，默认为否
`--nuget` | 指定NuGet Api服务地址。默认： https://api.nuget.org/v3/index.json​
`-e`  `--exclude` | 排除指定的包，支持正则，多个要件使用分号分隔
`-s`  `--special` | 下载指定的包，支持正则，多个条件使用分号分隔
`--runtime` | netcoreapp2.0/netcoreapp2.1，默认从deps文件中分析
`--arch` | x64/x86，默认从deps文件中分析
`--verbosity` | 显示详细日志
`--help` | 获取restore的帮助信息

>当--special与--exclude都存在时，先判断是否为Special包，再判断是否为Exclude包

# 示例
使用 e:/nustore/test.deps.json依赖文件，排除所有名称以Micosoft.和System开头的包，但下载Microsoft.Extensions.Logging包或其它包。

    nustore restore --dir="e:/nustore" --deps="e:/nustore/test.deps.json" --exclude="^microsoft.*;^System.*" -s "Microsoft\.Extensions.Logging"

## minify Options

 opt           | desc
-------------- | -----
`-d` `--dir` | 输出目录，默认为./nustored
`-c` `--copy` | 需要复制到输出目录的文件，默计为appsettings.json。支持通配符，如：*.exe;appsettings.json，多个配置用分号分隔
`-a` `--all` | 合并当前文件夹中的所有dll（包括非项目dll）
`--exclude` | 需要排除合并的dll，多个配置用分号分隔
`-k` `--kind` | 设置输出类型(dll, exe, winexe supported, 默认与入口项目一致)
`--search` | 添加dll的搜索目录，多个配置用分号分隔
`--delaysign` | 设置签名，但不签dll
`--debug` | 启用pdb生成
`-v` `--verbosity` | 显示详细日志
`--help` | 获取minify的帮助信息


### 示例

合并dlls，并复制 *.json and *.exe 到./out/目录.
``` bash
nustore minify --dir out -c *.json;*.exe
```

# 代码
https://github.com/aspark/nustore​  
https://aspark.gitbook.io/nustore  
https://www.nuget.org/packages/NuStore/  
