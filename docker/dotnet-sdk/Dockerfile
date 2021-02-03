# escape=`

FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine3.12
WORKDIR /work

ENV PATH="${PATH}:/root/.dotnet/tools"

#Install docker
COPY --from=docker:latest /usr/local/bin/docker /usr/bin/docker
#Install docker-compose
COPY --from=docker/compose:alpine-1.28.2 /usr/local/bin/docker-compose /usr/bin/docker-compose
RUN docker-compose --version