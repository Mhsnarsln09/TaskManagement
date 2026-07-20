# Yerel Geliştirme Ortamı

## Configuration ve secret ayrımı

Git'e eklenebilen, hassas olmayan uygulama ayarları `appsettings.json` ve
`appsettings.Development.json` içinde tutulur. Veritabanı parolası, JWT imza
anahtarı ve API anahtarı gibi secret'lar bu dosyalara yazılmaz.

Yerel Docker değişkenleri için `.env.example` dosyası kopyalanır:

```bash
cp .env.example .env
```

`.env` içindeki `POSTGRES_PASSWORD` yerel bir değerle değiştirilir. `.env`
Git tarafından ignore edilir; `.env.example` ise sadece anahtar isimlerini ve
güvenli örnek değerleri belgeler.

API'nin ileride kullanacağı connection string .NET User Secrets içinde tutulur:

```bash
dotnet user-secrets set \
  "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=taskmanagement;Username=taskmanagement;Password=LOCAL_PASSWORD" \
  --project TaskManagement.Api/TaskManagement.Api.csproj
```

`UserSecretsId` gizli değildir ve `.csproj` ile Git'e eklenir. Gerçek değerler
proje dizininin dışında, işletim sistemi kullanıcı profilinde saklanır.

Tanımlı secret anahtarlarını görmek için:

```bash
dotnet user-secrets list --project TaskManagement.Api/TaskManagement.Api.csproj
```

Bu komut değerleri terminale yazdırdığı için ekran görüntüsü ve log paylaşırken
dikkatli olunmalıdır. Production ortamında User Secrets yerine environment
variables veya yönetilen bir secret store kullanılmalıdır.

## PostgreSQL 18

PostgreSQL geliştirme ortamı `compose.yml` ile tanımlanmıştır. PostgreSQL 18'in
veri dizini düzeni nedeniyle volume `/var/lib/postgresql` yoluna bağlanır.

```bash
docker compose up -d
docker compose ps
docker compose logs postgres
```

Container sağlıklı olduğunda durum `(healthy)`, loglarda ise `database system is
ready to accept connections` görülür.

Container'ı durdurup veriyi korumak için:

```bash
docker compose down
```

`docker compose down -v` geliştirme veritabanını tamamen siler ve yalnızca veri
kaybı bilinçli olarak isteniyorsa kullanılmalıdır.

## Tüm sistemi container ile çalıştırma

`.env` dosyasında `JWT_SIGNING_KEY` en az 32 byte rastgele bir değer olmalıdır.

```bash
docker compose up -d --build
docker compose ps
curl http://localhost:${API_PORT:-8080}/health/ready
```

Compose API, PostgreSQL ve Redis'i ayağa kaldırır; local kolaylık için migration API
başlangıcında uygulanır. Production migration ve rollback akışı
`docs/POST-MVP-OPERATIONS.md` içinde tanımlıdır.
