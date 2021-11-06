FROM mcr.microsoft.com/dotnet/sdk:5.0
ARG ENV_NAME=Docker
ARG HTTPS_CERT_GEN_PASS=devcertpass
COPY . /app
WORKDIR /app/https/
WORKDIR /app/Web.NodeTwo/
RUN dotnet dev-certs https -ep /app/https/devcert2.pfx -p ${HTTPS_CERT_GEN_PASS}
RUN dotnet restore Web.NodeTwo.csproj
CMD ["dotnet", "run", "-c release", "--no-restore", "-p Web.NodeTwo.csproj"]