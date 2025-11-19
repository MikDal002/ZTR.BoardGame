FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Add copying .csproj and .sln files first, then doing a restore and then copying rest to speedup the build

WORKDIR /app

COPY . .

RUN dotnet tool install Nuke.GlobalTool --global
RUN export PATH="$PATH:/root/.dotnet/tools" && nuke Publish --configuration Release --skip UnitTests E2ETests Format

FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app
COPY --from=build /app/output .
ENTRYPOINT [ "dotnet", "ZtrBoardGame.Console.dll"]