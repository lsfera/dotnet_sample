#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0-alpine3.12 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine3.12 AS build
WORKDIR /src
COPY ["./src/DataGenerator", "."]

RUN dotnet restore DataGenerator.csproj
COPY ./src/DataGenerator .
WORKDIR "/src"
RUN dotnet build DataGenerator.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish DataGenerator.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ARG MESSAGES=1000
ENV MESSAGES=${MESSAGES}
CMD ["dotnet", "DataGenerator.dll"]