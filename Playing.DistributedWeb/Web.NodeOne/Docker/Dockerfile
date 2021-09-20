FROM mcr.microsoft.com/dotnet/sdk:5.0
COPY . /app/
WORKDIR /app/Web.NodeOne/
RUN dotnet restore Web.NodeOne.csproj
CMD ["dotnet", "run", "-c release", "--no-restore", "-p Web.NodeOne.csproj"]