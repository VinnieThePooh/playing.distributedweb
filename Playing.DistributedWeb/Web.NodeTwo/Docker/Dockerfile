FROM mcr.microsoft.com/dotnet/sdk:5.0
ARG ENV_NAME=Docker
COPY . /app
WORKDIR /app/https/
COPY ./devcert.pfx .
WORKDIR /app/Web.NodeTwo/
RUN dotnet restore Web.NodeTwo.csproj
CMD ["dotnet", "run", "-c release", "--no-restore", "-p Web.NodeTwo.csproj", "--environment=${ENV_NAME}"]