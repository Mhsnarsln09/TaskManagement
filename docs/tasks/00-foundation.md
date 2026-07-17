# Görev 00 — Başlangıç ve geliştirme ortamı

## Amaç

Solution'ın bağımlılık yönünü doğrulamak, kalite araçlarını hazırlamak ve uygulamanın her geliştiricide aynı şekilde ayağa kalkacağı temeli kurmak.

## Neden önce bunu yapıyoruz?

Yanlış proje referansları Clean Architecture sınırlarını daha ilk günden bozar. Derleme, format ve veritabanı kurulumu erken standartlaştırılırsa sonraki hataların iş kodundan mı ortamdan mı geldiği daha kolay anlaşılır.

## Yapılacaklar

- [x] .NET SDK sürümünü `global.json` ile 10.0.x ailesine sabitle.
- [x] Dört projenin referanslarını kontrol et; Domain hiçbir projeyi referans etmemeli.
- [x] Template ile gelen `WeatherForecast` kodlarını kaldır.
- [x] `Directory.Build.props` oluştur; nullable ve implicit usings ayarlarını ortaklaştır.
- [x] `.editorconfig` ve uygun `.gitignore` ekle.
- [x] Development configuration ile secret ayrımını öğren; gerçek secret commit etme.
- [x] PostgreSQL geliştirme örneğini belirle (Docker Compose ile PostgreSQL 18).
- [x] `TaskManagement.UnitTests` ve `TaskManagement.IntegrationTests` projelerini solution'a ekle.
- [x] `dotnet build` ve `dotnet test` komutlarını çalıştır.

## Öğrenme başlıkları

- Solution ile project arasındaki fark
- `.csproj`, target framework ve project reference
- Dependency inversion ile sadece dependency injection arasındaki fark
- Configuration provider sırası
- Nullable reference types

## Kabul kriterleri

- `dotnet build TaskManagement.sln` başarılı.
- `dotnet test TaskManagement.sln` başarılı.
- API projesi çalışıyor ve Swagger development ortamında açılıyor.
- Domain projesinde Infrastructure veya Api referansı yok.
- Repoda parola, connection string parolası veya JWT secret yok.

## Önerilen commit

```text
chore: establish solution foundation and test projects
```
