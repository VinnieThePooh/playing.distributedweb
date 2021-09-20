FROM mcr.microsoft.com/dotnet/sdk:5.0
COPY . /app/
WORKDIR /app/Web.NodeTwo/
RUN dotnet restore Web.NodeTwo.csproj
CMD ["dotnet", "run", "-c release", "--no-restore", "-p Web.NodeTwo.csproj"]