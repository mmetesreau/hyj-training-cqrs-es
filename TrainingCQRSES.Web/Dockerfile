FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

COPY TrainingCQRSES/*.csproj /source/TrainingCQRSES/
RUN dotnet restore /source/TrainingCQRSES/TrainingCQRSES.csproj

COPY TrainingCQRSES.Web/*.csproj /source/TrainingCQRSES.Web/
RUN dotnet restore /source/TrainingCQRSES.Web/TrainingCQRSES.Web.csproj

COPY TrainingCQRSES/. /source/TrainingCQRSES/
COPY TrainingCQRSES.Web/. /source/TrainingCQRSES.Web/

WORKDIR /source/TrainingCQRSES.Web/

RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "TrainingCQRSES.Web.dll"]