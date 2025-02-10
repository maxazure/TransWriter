@echo off
REM 设置项目路径
set PROJECT_PATH=TransWriter

REM 切换到项目目录
cd %PROJECT_PATH%

REM 恢复依赖项并编译项目
dotnet restore
dotnet build --configuration Release

REM 返回初始目录
cd ..

echo 编译完成！
pause
