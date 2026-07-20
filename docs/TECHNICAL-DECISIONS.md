# Teknik Kararlar

## Mimari

Bağımlılık yönü içeri doğrudur:

```text
Api -> Application -> Domain
Api -> Infrastructure -> Application/Domain
```

- **Domain:** Entity, enum, domain kuralı ve domain exception'ları. EF Core veya HTTP bilmez.
- **Application:** Proje/görev/auth kullanım senaryoları, request/response DTO'ları, FluentValidation kuralları ve port/interface'ler. İş akışını yönetir; EF Core, ASP.NET Core Identity veya HTTP bilmez.
- **Infrastructure:** EF Core DbContext, repository implementasyonları, Identity entity/configuration/migration'lar, kullanıcı doğrulama adaptörü, role seed ve JWT token üretimi. Application port'larını uygular.
- **Api:** HTTP controller'ları, authentication/authorization middleware yapılandırması, ClaimsPrincipal tabanlı current-user adapter'ı, Swagger/OpenAPI ve global exception middleware.

Bu ayrımın amacı klasör çoğaltmak değil, iş kurallarının veritabanı ve HTTP ayrıntılarından bağımsız kalmasını sağlamaktır.

Mevcut CRUD akışında controller'lar yalnızca HTTP sözleşmesini taşır. `ProjectService` ve `TaskService` Application katmanındadır; veri erişimi için `IProjectRepository` ve `ITaskRepository` port'larını kullanır. EF Core projection, sıralama ve pagination sorguları Infrastructure repository implementasyonlarında kalır.

Identity/JWT akışında register/login use-case'i Application katmanındadır. ASP.NET Core Identity ve JWT üretimi Infrastructure adaptörleriyle uygulanır. API yalnızca bearer middleware'i yapılandırır ve korumalı endpoint'lerin ihtiyaç duyduğu aktif kullanıcı kimliğini Application'daki `ICurrentUser` port'una bağlayan HTTP adapter'ını sağlar.

## Katman Kuralları

Bu projede katmanlı mimari tavsiye değil, zorunlu geliştirme kuralıdır. Yeni kod eklenirken aşağıdaki sınırlar korunur.

### Domain

Domain yalnızca iş modelini ve domain davranışını içerir.

Domain'e girebilir:

- Entity, value object, enum
- Entity metotları ve domain invariant'ları
- Domain'e ait saf kurallar

Domain'e giremez:

- EF Core attribute/configuration, DbContext, migration
- ASP.NET Core, controller, HTTP status code
- DTO, request/response modeli
- FluentValidation request validator'ı
- Identity, JWT, ClaimsPrincipal
- Repository, database query, pagination/sort query

### Application

Application kullanım senaryolarını yönetir. Dış dünya ile konuşmak için interface/port tanımlar, implementasyon bilmez.

Application'a girebilir:

- Use-case/application service
- Request/response DTO
- FluentValidation kuralları
- Port/interface: repository, current user, identity, token generator
- Application-level exception

Application'a giremez:

- `ApplicationDbContext`
- EF Core LINQ provider'a özel query implementasyonu
- ASP.NET Core controller/middleware/filter
- `UserManager`, `RoleManager`, `IdentityUser`
- JWT framework sınıfları
- Dosya sistemi, e-posta, dış servis SDK implementasyonu

### Infrastructure

Infrastructure, Application port'larının teknik implementasyonlarını içerir.

Infrastructure'a girebilir:

- EF Core DbContext, entity configuration, migration
- Repository implementasyonu
- Identity user modeli ve Identity servis implementasyonu
- JWT token üretimi implementasyonu
- Role seed, dış servis adaptörleri

Infrastructure'a giremez:

- Controller veya HTTP response kararı
- API route/model binding bilgisi
- UI veya transport-specific contract
- Domain kuralını bypass eden veri değişikliği

### Api

Api sadece host ve HTTP transport katmanıdır.

Api'ye girebilir:

- Controller
- Middleware ve exception handler
- Authentication/authorization middleware yapılandırması
- Swagger/OpenAPI yapılandırması
- `ClaimsPrincipal` -> `ICurrentUser` adapter'ı
- Dependency injection composition root

Api'ye giremez:

- Proje/görev/auth iş akışı
- Request/response DTO'ları
- FluentValidation validator'ları
- Repository veya EF query
- Identity register/login implementasyonu
- JWT token üretimi
- Role seed iş mantığı

### Yeni Kod Karar Ağacı

- Kod entity durumunu ve domain invariant'ını koruyorsa Domain.
- Kod bir kullanıcı senaryosunu koordine ediyorsa Application.
- Kod database, Identity, JWT, dosya sistemi veya dış servis implementasyonuysa Infrastructure.
- Kod HTTP isteğini alıp cevap dönüyorsa Api.
- Kod bir framework tipine ihtiyaç duyuyorsa önce bunun Application port'u olup olamayacağı değerlendirilir.

Katman ihlali fark edilirse yeni özellik eklemeden önce düzeltilir. "Sonra taşırız" yaklaşımı kabul edilmez; yanlış katmana eklenen kod tamamlanmış sayılmaz.

## Başlangıç tercihleri

- **Controller:** Öğrenme aşamasında routing, model binding, filter ve authorization davranışlarını görünür kılar.
- **PostgreSQL:** Açık kaynaklı, yaygın ve production kullanımına uygun ilişkisel veritabanı.
- **EF Core:** İlişkiler, change tracking, LINQ ve migration'ları öğrenmek için ana veri erişim aracı.
- **Repository port'ları:** Application katmanı Infrastructure'a referans vermediği için CRUD use-case'leri `IProjectRepository` ve `ITaskRepository` port'ları üzerinden veri erişir. Genel generic repository yazılmayacak; yalnızca use-case sınırında ihtiyaç olan özel port'lar tutulur.
- **FluentValidation:** Girdi doğrulamasını controller ve entity'lerden ayırır.
- **Mapster veya manuel mapping:** İlk birkaç endpoint'te manuel mapping ile DTO/entity ayrımı öğrenilir; tekrar arttığında Mapster değerlendirilir.
- **Problem Details:** API hataları için standart ve istemci tarafından işlenebilir format.
- **UTC:** Sunucu ve kullanıcı saat dilimi hatalarını azaltır.

## Bilinçli olarak ertelenenler

### CQRS ve MediatR

İlk CRUD akışı doğrudan application service/use-case ile kurulacak. Okuma ve yazma ihtiyaçları belirginleşince CQRS uygulanacak. Böylece MediatR yalnızca katman sayısını artıran bir araç değil, çözdüğü problem görüldükten sonra öğrenilir.

### Result pattern

Beklenen iş hataları ile beklenmeyen sistem hatalarının farkı görüldükten sonra eklenir. Her metoda mekanik olarak `Result<T>` sarmak yerine hata sözleşmesi tasarlanır.

### Redis, Hangfire ve SignalR

Görev 08 ile somut ihtiyaçlar oluştu: proje istatistiği kısa süreli Redis cache'i,
son tarih reminder'ı için Hangfire ve kalıcı bildirimin hızlı teslimatı için SignalR
eklendi. Bunların tamamı Application portları arkasındadır. Redis kapalıyken no-op
cache kullanılır; SignalR hatası kalıcı işlemi bozmaz; Hangfire job'ı idempotency
anahtarıyla tekrar çalıştırılabilir.

## Veri modelleme ilkeleri

- API'de entity doğrudan döndürülmez; request/response DTO kullanılır.
- `ProjectMember`, User–Project çoktan çoğa ilişkisinin açık join entity'sidir.
- Foreign key'ler entity üzerinde görünür tutulur.
- Zorunlu alanlar hem domain/application seviyesinde hem veritabanında korunur.
- Concurrency ihtiyacı görev güncellemelerinde test edildikten sonra optimistic concurrency token eklenir.
- `TaskItem` adı, `System.Threading.Tasks.Task` ile isim çakışmasını önler.

## Güvenlik ilkeleri

- Authentication kullanıcının kim olduğunu, authorization ne yapabileceğini belirler.
- İstekten gelen `userId` kimlik kanıtı sayılmaz; aktif kullanıcı token claim'inden alınır.
- Kaynağın projeye üyeliği her kritik sorguda denetlenir.
- Dosya adı, uzantı, MIME türü ve boyutu doğrulanır; istemci dosya adı fiziksel yol olarak kullanılmaz.
- Secret'lar development'ta user-secrets/environment variables ile sağlanır.

## Kaynak bazlı yetkilendirme

### Merkezi application servisi tercihi

Kaynak bazlı yetkilendirme için ASP.NET Core authorization handler/policy yerine merkezi bir application servisi (`ProjectAuthorizationService`) seçildi. Gerekçe:

- Kurallar (üyelik, sahiplik, Admin bypass) veriye bağlıdır ve Application katmanındaki use-case'lerle aynı yerde yaşar.
- HTTP pipeline'a bağımlı olmadığı için controller'dan bağımsız unit/integration test edilebilir.
- `[Authorize(Roles = "...")]` yalnızca kaba kapı olarak kalır; kritik kontrol hiçbir zaman UI veya attribute varsayımına bırakılmaz.

Servis üç seviye sunar:

- `EnsureMemberAsync(projectId)`: Aktif kullanıcı projenin üyesi olmalıdır (okuma, görev oluşturma/güncelleme).
- `EnsureCanManageAsync(projectId)`: Proje sahibi veya `Admin` rolü gerekir (proje güncelleme/silme, üye ekleme/çıkarma).
- `EnsureCanDeleteTasksAsync(projectId)`: Görev silme ek olarak rol kapısından geçer: `Admin` **veya** `ProjectManager` rolüne sahip proje sahibi. `Member` rolündeki bir kullanıcı, projenin sahibi olsa bile görev silemez; proje yönetim hakları (üye ekleme, proje güncelleme) sahiplik üzerinden devam eder.

`Admin` sistem seviyesindeki roldür ve üyelik şartını atlar. Rol yalnızca kaba kapıdır; projeye özel yetki her zaman sahiplik/üyelik verisiyle birlikte hesaplanır (`ProjectManager` rolü, sahibi olmadığı projede yetki vermez).

### 401 / 403 / 404 sözleşmesi

- **401:** Token yok veya geçersiz. JWT middleware ProblemDetails ile döner.
- **404:** Proje yok **veya** aktif kullanıcı projenin üyesi değil. Üye olmayana 403 dönmek proje id'sinin varlığını sızdıracağı için bilinçli olarak 404 dönülür. Görevler proje kapsamında sorgulandığından aynı politika görev okumaları için de geçerlidir.
- **403:** Kullanıcı projenin üyesi ama işlemin gerektirdiği yetkiye sahip değil (ör. sahibi olmadığı projede görev silme, üye yönetimi). Üye zaten projenin varlığını bildiği için burada 404 gizlemesi gerekmez.
- **409:** Domain kuralı ihlali (ör. tamamlanmış görevi düzenleme, mükerrer üyelik, sahibi üyelikten çıkarma).

### Üyelik iş kuralları

- Projeyi oluşturan kullanıcı otomatik olarak ilk `ProjectMember` kaydıdır (`Project.Create`).
- Proje listesi yalnızca aktif kullanıcının üyesi olduğu projeleri döner.
- Mükerrer üyelik hem domain kuralıyla (`DomainException` → 409) hem `(ProjectId, UserId)` unique index'iyle reddedilir. Yarış durumunda domain kontrolünü atlayan istek unique index'e takılır; global exception handler PostgreSQL `23505` unique violation taşıyan `DbUpdateException`'ı 500 yerine `409 Conflict` olarak döndürür.
- Göreve atanan kullanıcının proje üyesi olduğu doğrulanır (400, `assigneeUserId`).
- Proje sahibi üyelikten çıkarılamaz; açık (Todo/InProgress) görevi atanmış üye, görevler yeniden atanmadan çıkarılamaz.

## Görev durum makinesi

İzin verilen geçişler:

```text
Todo       -> InProgress | Cancelled
InProgress -> Completed  | Cancelled
Completed  -> InProgress (yeniden açma)
Cancelled  -> (terminal)
```

- `Todo -> Completed` API kolaylığı olarak kabul edilir ve domain'de `Start` + `Complete` olarak çalışır.
- **Yeniden açma politikası:** Tamamlanmış görev yalnızca status `InProgress` istenerek yeniden açılabilir (`TaskItem.Reopen`); bunun dışındaki her düzenleme domain tarafından 409 ile reddedilir.
- Geçersiz geçişler `DomainException`/`ConflictException` ile 409 döner.

## Yorum, dosya ve istatistik (Görev 06)

### Görev alt kaynakları ve TaskAccessGuard

Yorum ve dosya endpoint'leri proje route'u altındadır
(`/api/projects/{projectId}/tasks/{taskId}/...`). Her istek iki kontrolü birlikte
geçer: çağıran proje üyesi olmalı **ve** görev gerçekten o projeye ait olmalı. İkinci
kontrol olmadan, A projesinin üyesi kendi proje id'sini yabancı bir görev id'siyle
eşleştirip B projesindeki görevin yorumlarını okuyabilirdi. Bu iki kontrol
`TaskAccessGuard` içinde toplanır; her iki başarısızlık da 404 sözleşmesine uyar.

Yorum yazarı her zaman token'daki aktif kullanıcıdır; istekten yazar id'si alınmaz.
Response'taki yazar/yükleyen bilgisi `UserSummaryResponse` ile sınırlıdır (id,
userName, displayName) — e-posta ve roller bilinçli olarak dışarıda tutulur. Yorum
düzenleme/silme MVP kapsamı dışıdır; endpoint yoktur (kimin kimin yorumunu
düzenleyebileceği ve geçmişin tutulup tutulmayacağı ayrı bir karardır).

### Dosya yükleme güvenliği

- İstemci dosya adı yalnızca görüntüleme içindir. Fiziksel ad her zaman sunucuda
  üretilir (`{Guid:N}{uzantı}`); path traversal filtreyle değil yapısal olarak
  engellenir.
- `UploadFileName` istemci adını tek path segmentine indirger: `/`, `\`, `:` sonrası
  alınır, kontrol karakterleri (NUL dahil) atılır, 255 karakter sınırı uygulanır.
- Uzantı ve content-type allow-list'i configuration'dadır (`FileUpload` bölümü);
  bilinmeyen tür varsayılan olarak reddedilir. Boş dosya ve boyut aşımı 400 döner;
  boyut sınırı ayrıca `MultipartBodyLengthLimit` ile transport seviyesinde de uygulanır.
- İstemcinin beyan ettiği content-type kanıt değildir; asıl savunma indirme tarafında:
  içerik her zaman `Content-Disposition: attachment` + `X-Content-Type-Options:
  nosniff` ile döner, böylece yanlış etiketli dosya API origin'inde çalıştırılamaz.
- Depolama `IFileStorage` port'u arkasındadır; ilk implementasyon yerel disktir
  (`FileStorage:Local:RootPath`). `LocalFileStorage` stored name'i kendi başına da
  doğrular ve çözülen yolun depolama kökü içinde kaldığını denetler.
- Tutarlılık: önce dosya yazılır, sonra metadata kaydedilir. Metadata kaydı
  başarısız olursa fiziksel dosya silinmeye çalışılır (öksüz dosya, indirilemeyen
  metadata'dan daha ehven). `StoredFileName` unique index'i bir fiziksel dosyanın tek
  bir metadata satırına ait olmasını garanti eder.

### İstatistik

Tüm sayımlar (`Total`, `Todo`, `InProgress`, `Completed`, `Cancelled`, `Overdue`) tek
bir `GroupBy` projection'ıyla tek sorguda hesaplanır. Boş proje sıfır satır ürettiği
için null sonuç sıfırlara map edilir; tamamlanma yüzdesi boş projede 0 döner (sıfıra
bölme yok). İptal edilen görevler paydada kalır, böylece görev iptal etmek ilerlemeyi
şişirmez. Endpoint tüm proje üyelerine açıktır; üye olmayan 404 alır.

## Test stratejisi

### Mock kütüphanesi: NSubstitute

Unit testlerde tek mock kütüphanesi olarak NSubstitute kullanılır (Moq değil); iki
kütüphanenin karışması yasak. Mock'lar yalnızca Application katmanının port'ları
(`IProjectRepository`, `ITaskRepository`, `ICurrentUser`) için kullanılır; domain
entity'leri ve `ProjectAuthorizationService` gibi saf application servisleri gerçek
halleriyle test edilir, böylece implementasyon ayrıntısı değil davranış sınanır.

### Integration testler gerçek PostgreSQL üzerinde (Testcontainers)

Integration testler daha önce in-memory Sqlite kullanıyordu; bu, provider'a özel
workaround'lar gerektiriyordu (`DateTimeOffsetToBinaryConverter`, exception
handler'da mesaj tabanlı unique-violation tespiti) ve ilişkisel davranışı
kanıtlamıyordu. Testler artık Testcontainers ile compose.yml'dekiyle aynı imajdan
(`postgres:18`) gerçek bir PostgreSQL container'ı başlatır ve şemayı
`Database.MigrateAsync()` ile gerçek migration'lardan kurar. Böylece `xmin`
concurrency token'ı, unique index ihlallerinin `PostgresException` üzerinden 409'a
map'lenmesi ve `timestamp with time zone` sıralaması üretimdekiyle aynı yolda test
edilir; Sqlite'a özel tüm kod yolları silindi.

İzolasyon stratejisi: her test sınıfı `IClassFixture<TaskManagementApiFactory>` ile
kendi factory'sini, dolayısıyla kendi container'ını ve taze migrate edilmiş
veritabanını alır. Testler kendi kullanıcılarını/projelerini benzersiz adlarla
oluşturur; ortak sıraya veya önceden var olan veriye bağımlılık yoktur ve xUnit'in
sınıflar arası paralel koşusu güvenlidir.

## Zaman bağımlı kurallar

Gecikme bilgisi `DueDate < UtcNow (bugün) && Status != Completed && Status != Cancelled` olarak hesaplanır ve `TaskResponse.IsOverdue` alanında döner. Sistem saati doğrudan okunmaz; `TimeProvider` DI üzerinden enjekte edilir (`TimeProvider.System` kayıtlıdır) ve testlerde sahte saat ile değiştirilebilir.
