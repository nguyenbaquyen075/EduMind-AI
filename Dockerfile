# Web SuDia: build bằng SDK .NET 8, chạy bằng ảnh ASP.NET gọn nhẹ, không chạy bằng root
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY SuDia.Web.csproj ./
RUN dotnet restore SuDia.Web.csproj
COPY . .
RUN dotnet publish SuDia.Web.csproj -c Release -o /out --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out ./
# Thư mục dữ liệu bền (gắn volume): khoá mã hoá đăng nhập, tệp tài liệu khi chưa dùng R2
RUN mkdir -p /app/App_Data && chown -R app:app /app/App_Data
USER app
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080
ENTRYPOINT ["dotnet", "SuDia.Web.dll"]
