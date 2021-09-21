FROM mcr.microsoft.com/dotnet/sdk:5.0
COPY . /app/
WORKDIR /app/https/
WORKDIR /app/Web.NodeTwo/
RUN dotnet dev-certs https -ep /app/https/devcert2.pfx -p devcertpass
RUN dotnet restore Web.NodeTwo.csproj
CMD ["dotnet", "run", "-c release", "--no-restore", "-p Web.NodeTwo.csproj"]