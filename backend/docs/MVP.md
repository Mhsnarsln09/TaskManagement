# Minimum Viable Product

## MVP hedefi

Tek bir ekip akışını uçtan uca güvenli biçimde çalıştırmak: kullanıcı giriş yapar, proje oluşturur, üyeyi projeye ekler, görev atar, ekip görevin durumunu ve yorumlarını günceller, proje yöneticisi ilerlemeyi görür.

## Durum notu — 22 Temmuz 2026

Temel akışlar uygulanmıştır; ancak mevcut sürüm yayınlanabilir MVP olarak tamamlanmış kabul edilmez. Silinmiş proje alt kaynaklarına erişim, yorum sayfalama sözleşmesi, görev yetkilerinin kapsamı ve refresh token iptali [Görev 10 — MVP güvenlik ve tutarlılık](tasks/10-mvp-hardening.md) tamamlanana kadar çıkış engelidir.

## MVP kapsamı

### Kimlik ve erişim

- Kayıt ve giriş
- ASP.NET Core Identity ile parola saklama
- JWT access token
- `Admin`, `ProjectManager`, `Member` rolleri
- Endpoint ve kaynak bazlı yetki kontrolleri
- Sunucu tarafında refresh token iptal eden çıkış akışı

### Proje yönetimi

- Proje oluşturma, listeleme, detay, güncelleme ve silme
- Projeye üye ekleme, listeleme ve çıkarma
- Kullanıcının yalnızca üyesi olduğu projeleri görmesi
- Admin'in yönetim amacıyla tüm aktif projeleri listeleyebilmesi
- Soft-delete edilen projenin bütün alt kaynaklardan erişilemez olması

### Görev yönetimi

- Görev oluşturma, listeleme, detay, güncelleme ve silme
- Görevi proje üyesine atama
- `Todo`, `InProgress`, `Completed` durumları
- `Low`, `Medium`, `High`, `Critical` öncelikleri
- Son teslim tarihi ve hesaplanan gecikme bilgisi
- Sayfalama, durum/öncelik filtresi ve temel sıralama
- Proje sahibi/Admin için tam görev yönetimi; atanmış üye için yalnız durum güncelleme yetkisi
- Gecikmiş görevin diğer alanlarını mevcut geçmiş tarih nedeniyle kilitlemeyen güncelleme doğrulaması

### İş birliği ve raporlama

- Göreve yorum ekleme ve yorumları listeleme
- Güvenli dosya yükleme ve indirme için ilk uygulama
- Toplam, tamamlanan, devam eden ve geciken görev sayıları
- Tamamlanma yüzdesi
- En yeni yorumların ilk yüklemede görünmesini ve eski yorumların geriye doğru alınmasını sağlayan tutarlı sayfalama

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

- Sürükle-bırak Kanban ve görevlerin kolon içi kalıcı sıralaması
- Tam metin görev araması ve gelişmiş filtreler
- Proje içi çoklu roller, davet/onay akışı ve sahiplik devri
- Yorum düzenleme/silme, mention, etiket ve alt görevler
- Kullanıcıya açılan aktivite ve değişiklik geçmişi
- Soft-delete geri yükleme ekranı ve otomatik retention temizliği
- E-posta doğrulama, şifre sıfırlama ve aktif oturum yönetimi
- Gelişmiş dashboard grafikleri
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
- Silinmiş proje ve görevlerin alt kaynaklarına bilinen ID veya eski üyelikle erişilemiyor.
- Hatalı girdiler alan bazlı ve anlaşılır hata döndürüyor.
- Görev oluşturma, durum geçişi ve yetkilendirme testleri geçiyor.
- 20'den fazla yorumda en yeni yorum ilk sayfada ve eski yorum yükleme sırası doğru çalışıyor.
- Çıkış sonrasında refresh token tekrar kullanılamıyor.
- Stale update sürüm/ETag çakışması `409 Conflict` veya `412 Precondition Failed` ile reddediliyor.
- Swagger üzerinde authentication akışı kullanılabiliyor.
- Uygulama ve PostgreSQL yerel ortamda yeniden başlatılabiliyor.
