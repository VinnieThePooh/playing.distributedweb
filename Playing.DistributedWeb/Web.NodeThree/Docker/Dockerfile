FROM mcr.microsoft.com/dotnet/sdk:5.0
ARG ENV_NAME=Docker
COPY . /app
WORKDIR /app/https/
WORKDIR /app/Web.NodeThree/
RUN dotnet restore Web.NodeThree.csproj
CMD ["dotnet", "run", "-c release", "--no-restore", "-p Web.NodeThree.csproj", "--environment ${ENV_NAME}"]