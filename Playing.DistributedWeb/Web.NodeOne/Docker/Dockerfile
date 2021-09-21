FROM mcr.microsoft.com/dotnet/sdk:5.0
COPY . /app/
WORKDIR /app/https/
WORKDIR /app/Web.NodeOne/
RUN dotnet dev-certs https -ep /app/https/devcert1.pfx -p devcertpass
RUN dotnet restore Web.NodeOne.csproj
CMD ["dotnet", "run", "-c release", "--no-restore", "-p Web.NodeOne.csproj"]