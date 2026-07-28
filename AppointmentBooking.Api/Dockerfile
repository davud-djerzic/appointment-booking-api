FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ["AppointmentBooking.Api/AppointmentBooking.Api.csproj", "AppointmentBooking.Api/"]

RUN dotnet restore "AppointmentBooking.Api/AppointmentBooking.Api.csproj"

COPY . .

WORKDIR "/src/AppointmentBooking.Api"

RUN dotnet publish "AppointmentBooking.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/sdk:10.0 AS final

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "AppointmentBooking.Api.dll"]