# Minimum Viable Product

## MVP hedefi

Tek bir ekip akışını uçtan uca güvenli biçimde çalıştırmak: kullanıcı giriş yapar, proje oluşturur, üyeyi projeye ekler, görev atar, ekip görevin durumunu ve yorumlarını günceller, proje yöneticisi ilerlemeyi görür.

## MVP kapsamı

### Kimlik ve erişim

- Kayıt ve giriş
- ASP.NET Core Identity ile parola saklama
- JWT access token
- `Admin`, `ProjectManager`, `Member` rolleri
- Endpoint ve kaynak bazlı yetki kontrolleri

### Proje yönetimi

- Proje oluşturma, listeleme, detay, güncelleme ve silme
- Projeye üye ekleme, listeleme ve çıkarma
- Kullanıcının yalnızca üyesi olduğu projeleri görmesi

### Görev yönetimi

- Görev oluşturma, listeleme, detay, güncelleme ve silme
- Görevi proje üyesine atama
- `Todo`, `InProgress`, `Completed` durumları
- `Low`, `Medium`, `High`, `Critical` öncelikleri
- Son teslim tarihi ve hesaplanan gecikme bilgisi
- Sayfalama, durum/öncelik filtresi ve temel sıralama

### İş birliği ve raporlama

- Göreve yorum ekleme ve yorumları listeleme
- Güvenli dosya yükleme ve indirme için ilk uygulama
- Toplam, tamamlanan, devam eden ve geciken görev sayıları
- Tamamlanma yüzdesi

### Teknik gereksinimler

- ASP.NET Core Web API ve controller tabanlı endpoint'ler
- EF Core ve PostgreSQL
- Swagger/OpenAPI
- FluentValidation
- Global exception handling ve Problem Details
- Serilog ile yapılandırılmış loglama
- Kritik unit ve integration testleri

## MVP dışı

Aşağıdakiler değerlidir ancak ilk çalışan sürümü geciktirmemesi için MVP sonrasına bırakılmıştır:

- React, Angular veya Blazor arayüzü
- SignalR gerçek zamanlı bildirimler
- Hangfire zamanlanmış işler
- Redis cache
- E-posta bildirimi
- Refresh token ve token iptali
- Audit log ve tam soft delete altyapısı
- Gelişmiş arama ve dashboard grafiklerinin tümü
- Docker production orchestration ve CI/CD deployment
- Mikroservis mimarisi

## MVP API taslağı

```text
POST   /api/auth/register
POST   /api/auth/login

GET    /api/projects
POST   /api/projects
GET    /api/projects/{projectId}
PUT    /api/projects/{projectId}
DELETE /api/projects/{projectId}

GET    /api/projects/{projectId}/members
POST   /api/projects/{projectId}/members
DELETE /api/projects/{projectId}/members/{userId}

GET    /api/projects/{projectId}/tasks
POST   /api/projects/{projectId}/tasks
GET    /api/projects/{projectId}/tasks/{taskId}
PUT    /api/projects/{projectId}/tasks/{taskId}
DELETE /api/projects/{projectId}/tasks/{taskId}

GET    /api/projects/{projectId}/tasks/{taskId}/comments
POST   /api/projects/{projectId}/tasks/{taskId}/comments

GET    /api/projects/{projectId}/tasks/{taskId}/attachments
POST   /api/projects/{projectId}/tasks/{taskId}/attachments
GET    /api/projects/{projectId}/tasks/{taskId}/attachments/{attachmentId}/content

GET    /api/projects/{projectId}/statistics
```

Görev ve alt kaynakları proje route'u altında tutulur: her yetki kontrolü proje
üyeliğine dayandığı için proje id'si URL'de görünür kalır. Durum güncellemesi ayrı bir
`PATCH .../status` yerine `PUT` gövdesindeki `status` alanıyla yapılır.

Örnek filtreli istek:

```http
GET /api/projects/{projectId}/tasks?status=InProgress&priority=High&page=1&pageSize=20&sortBy=dueDate&sortDirection=asc
```

## MVP çıkış kriterleri

- Temel senaryo temiz bir veritabanında migration uygulanarak çalışıyor.
- Başka projeye erişim denemeleri güvenli biçimde reddediliyor.
- Hatalı girdiler alan bazlı ve anlaşılır hata döndürüyor.
- Görev oluşturma, durum geçişi ve yetkilendirme testleri geçiyor.
- Swagger üzerinde authentication akışı kullanılabiliyor.
- Uygulama ve PostgreSQL yerel ortamda yeniden başlatılabiliyor.

