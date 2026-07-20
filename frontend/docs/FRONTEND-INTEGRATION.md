# Frontend Entegrasyon Sözleşmesi

## İstemci teknolojileri

Frontend Next.js App Router ve TypeScript ile geliştirilecek; stil ve bileşen
altyapısında Tailwind CSS, shadcn/ui ve Lucide icons kullanılacaktır. Bu belge görsel
tasarımdan bağımsız olarak backend ile entegrasyonun teknik sözleşmesidir.

## Yerel adresler

- API: `http://localhost:8080`
- Scalar: `http://localhost:8080/scalar/v1`
- OpenAPI: `http://localhost:8080/openapi/v1.json`
- SignalR hub: `http://localhost:8080/hubs/notifications`

Development CORS originleri `http://localhost:3000`, `http://localhost:4200` ve
`http://localhost:5173` olarak tanımlıdır. Frontend başka portta çalışırsa
`Cors:AllowedOrigins` listesine açıkça eklenmelidir; wildcard origin ve credentials
birlikte kullanılmaz.

## Authentication akışı

`POST /api/auth/register` ve `POST /api/auth/login` response'u access token, access
token bitiş zamanı, refresh token, refresh token bitiş zamanı ve kullanıcı özetini
döndürür. API çağrılarında:

Register request yalnızca `email`, `userName`, `password` ve nullable `displayName`
alanlarını kabul eder. **Rol alanı yoktur ve kayıt ekranında rol seçimi
gösterilmemelidir.** Backend her yeni kullanıcıyı otomatik `Member` yapar. Mevcut
API'de kullanıcı/rol yönetimi veya SuperAdmin endpoint'i yoktur.

```http
Authorization: Bearer ACCESS_TOKEN
```

Access token süresi dolduğunda `POST /api/auth/refresh` tek kez çağrılır. Başarılı
response'taki hem access token hem refresh token atomik olarak eskilerinin yerini
almalıdır. Aynı refresh token ikinci kez kullanılmaz; reuse detection tüm oturumu
iptal eder. Aynı anda birden fazla `401` gelirse frontend tek bir refresh isteğini
paylaştırmalı, paralel refresh çağrıları başlatmamalıdır.

Refresh token JavaScript tarafından okunabilen kalıcı storage'da tutulacaksa XSS
riski ayrıca kabul edilmiş olur. Production frontend tasarımında refresh token için
Secure, HttpOnly, SameSite cookie/BFF yaklaşımı değerlendirilmelidir; mevcut API
tokenı JSON response içinde döndürür.

## SignalR

JavaScript client bağlantısı tokenı `accessTokenFactory` ile verir:

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:8080/hubs/notifications", {
    accessTokenFactory: () => accessToken
  })
  .withAutomaticReconnect()
  .build();

connection.on("notificationReceived", notification => {
  // Listeyi güncelle veya kullanıcıya toast göster.
});
```

SignalR kaçırılan mesajları saklayan kaynak değildir. İlk bağlantıda ve reconnect
sonrasında `GET /api/notifications` çağrılmalıdır. Okunan bildirim
`PUT /api/notifications/{id}/read` ile işaretlenir.

## Hata sözleşmesi

Hatalar `application/problem+json` döner. Frontend en az `status`, `title`, `detail`
ve validation hatalarında `errors` alanlarını işlemelidir. Kaynak bazlı güvenlikte
üye olmayan kullanıcıya kaynak varlığını sızdırmamak için bazı erişimler `404`,
yetkisi yetersiz mevcut üyeye `403` döner. Rate limit aşımı `429`dur.

## İstemci üretimi

TypeScript tipleri ve API client kodu elle kopyalanmamalıdır. Development API açıkken
`/openapi/v1.json` sözleşmesinden Next.js istemcisi için client üretilmeli; üretilen
kod ayrı bir klasörde tutulmalı, doğrudan elle düzenlenmemeli ve backend contract
değiştiğinde yeniden üretilmelidir.
