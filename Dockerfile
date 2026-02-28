# Use a variable for the .NET version to easily update it in one place.
ARG DOTNET_VERSION=9.0

# --- Build Stage ---
# This stage compiles the application and publishes the output.
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /app

# Copy solution and project files first to leverage Docker layer caching.
# This ensures that 'dotnet restore' is only re-run when project dependencies change.
COPY ["ZtrBoardGame.sln", "Directory.Build.props", "global.json", "./"]
COPY ["build/_build.csproj", "./build/"]
COPY ["src/ZtrBoardGame.Configuration.Shared/ZtrBoardGame.Configuration.Shared.csproj", "./src/ZtrBoardGame.Configuration.Shared/"]
COPY ["src/ZtrBoardGame.Console/ZtrBoardGame.Console.csproj", "./src/ZtrBoardGame.Console/"]
COPY ["src/ZtrBoardGame.RaspberryPi/ZtrBoardGame.RaspberryPi.csproj", "./src/ZtrBoardGame.RaspberryPi/"]
COPY ["tests/ZtrBoardGame.Console.Tests/ZtrBoardGame.Console.Tests.csproj", "./tests/ZtrBoardGame.Console.Tests/"]
COPY ["tests/ZtrBoardGame.E2E.Tests/ZtrBoardGame.E2E.Tests.csproj", "./tests/ZtrBoardGame.E2E.Tests/"]

# Restore dependencies for all projects.
RUN dotnet restore "ZtrBoardGame.sln"

# Copy the rest of the application source code.
# The .dockerignore file prevents unnecessary files from being copied.
COPY . .

# Install Nuke as a global tool within the build container and run the publish command.
# Using the full path to the tool avoids potential PATH issues.
RUN dotnet tool install Nuke.GlobalTool --global --version 9.0.4
RUN /root/.dotnet/tools/nuke Publish --configuration Release --skip UnitTests E2ETests Format CreateVersionLabel

# --- Final Stage ---
# This stage creates the final, smaller, and more secure runtime image.
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final

# Create a non-root user and group for security purposes.
# Running the application as a non-root user is a critical security best practice.
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

WORKDIR /app
# Copy the published application from the build stage.
COPY --from=build /app/output .

# Set the owner of the application files to the non-root user.
RUN chown -R appuser:appgroup .

# Switch to the non-root user.
USER appuser

# Set the entry point for the container.
ENTRYPOINT ["dotnet", "ZtrBoardGame.Console.dll"]