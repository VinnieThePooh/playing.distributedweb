FROM mcr.microsoft.com/dotnet/sdk:5.0
COPY . /app/
WORKDIR /app/https/
WORKDIR /app/Web.NodeThree/
RUN dotnet dev-certs https -ep /app/https/devcert3.pfx -p devcertpass
RUN dotnet restore Web.NodeThree.csproj
CMD ["dotnet", "run", "-c release", "--no-restore", "-p Web.NodeThree.csproj"]