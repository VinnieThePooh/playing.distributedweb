FROM mcr.microsoft.com/dotnet/sdk:5.0
COPY . /app/
WORKDIR /app/Web.NodeThree/
RUN dotnet restore Web.NodeThree.csproj
CMD ["dotnet", "run", "-c release", "--no-restore", "-p Web.NodeThree.csproj"]