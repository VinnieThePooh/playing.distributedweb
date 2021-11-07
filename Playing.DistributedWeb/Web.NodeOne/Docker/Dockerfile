FROM mcr.microsoft.com/dotnet/sdk:5.0
ARG ENV_NAME=Docker
COPY . /app
WORKDIR /app/https/
WORKDIR /app/Web.NodeOne/
RUN dotnet restore Web.NodeOne.csproj
CMD ["dotnet", "run", "-c release", "--no-restore", "-p Web.NodeOne.csproj", "--environment ${ENV_NAME}"]