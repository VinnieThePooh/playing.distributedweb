FROM mcr.microsoft.com/dotnet/sdk:5.0
ARG ENV_NAME=Docker
ARG HTTPS_CERT_GEN_PASS=devcertpass
COPY . /app
WORKDIR /app/https/
WORKDIR /app/Web.NodeOne/
RUN dotnet dev-certs https -ep /app/https/devcert1.pfx -p ${HTTPS_CERT_GEN_PASS}
RUN dotnet restore Web.NodeOne.csproj
CMD ["dotnet", "run", "-c release", "--no-restore", "-p Web.NodeOne.csproj"]