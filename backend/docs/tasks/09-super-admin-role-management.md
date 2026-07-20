# 09 - SuperAdmin ve Rol Yönetimi

## Amaç

Sistemde ilk `SuperAdmin` hesabını güvenli biçimde oluşturmak ve kullanıcı rollerini
yalnızca bu rolün yönetebileceği API'ler sağlamak. Public kayıt akışı rol kabul
etmeyecek ve yeni kullanıcıyı `Member` olarak oluşturmaya devam edecektir.

## Mevcut durum

- `Admin`, `ProjectManager` ve `Member` rolleri başlangıçta oluşturuluyor.
- `POST /api/auth/register` rol almaz; kullanıcı otomatik `Member` olur.
- `Admin` proje kaynaklarında sistem seviyesi yetkiye sahiptir.
- Kullanıcı listeleme, arama ve rol atama API'si yoktur.
- İlk yönetici hesabını güvenli biçimde oluşturacak bootstrap mekanizması yoktur.

## Mimari karar

- `SuperAdmin` sistem kimlik ve rol yönetiminden sorumludur.
- `Admin` mevcut proje kaynaklarındaki global yetkisini korur ancak rol veremez.
- Rol yönetimi bir Identity use-case'idir; Domain katmanına taşınmaz.
- Contract, use-case ve port'lar Application; Identity implementasyonu
  Infrastructure; authorization/HTTP eşleme ise Api katmanında kalır.
- Controller doğrudan `UserManager` veya `RoleManager` kullanmaz.

## Görevler

### Application

- [ ] `SuperAdmin` rolünü `ApplicationRoles` listesine ekle.
- [ ] Sayfalı kullanıcı listeleme/arama response'larını tanımla: `id`, `email`,
  `userName`, `displayName`, `roles`.
- [ ] Rol değiştirme request'ini tanımla; yalnızca bilinen roller kabul edilsin.
- [ ] Kullanıcı listeleme ve rol değiştirme use-case'lerini Application altında yaz.
- [ ] Son `SuperAdmin` rolünün kaldırılamaması kuralını use-case seviyesinde koru.
- [ ] Rol değişikliklerinde refresh token oturumlarını iptal et; eski JWT'nin rol
  yetkisini token süresi boyunca taşımaması için güvenlik davranışını belgeleyip test et.

### Infrastructure

- [ ] Identity port'larını `UserManager`/`RoleManager` ile uygula.
- [ ] İlk SuperAdmin'i yalnızca açıkça verilen secret/config değerleriyle idempotent
  bootstrap et (`BootstrapAdmin:Email`, `UserName`, `Password`).
- [ ] Bootstrap kapalıysa veya değerler eksikse varsayılan parola/hesap üretme.
- [ ] Parola, token ve secret değerlerini loglama.
- [ ] Rol değişikliğini mevcut audit mekanizmasına aktör, hedef kullanıcı ve eski/yeni
  rollerle kaydet; hassas veri kaydetme.

### Api

- [ ] `GET /api/admin/users?page=&pageSize=&search=` endpoint'ini ekle.
- [ ] `PUT /api/admin/users/{userId}/roles` endpoint'ini ekle.
- [ ] Endpoint'leri yalnızca `SuperAdmin` policy'si ile koru.
- [ ] `400`, `401`, `403`, `404`, `409` Problem Details cevaplarını belgeleyip OpenAPI'ye ekle.
- [ ] Rate limit ve correlation id davranışını mevcut pipeline ile koru.

### Testler

- [ ] Public register request'inde rol alanı olmadığını ve kullanıcının `Member` olduğunu test et.
- [ ] SuperAdmin dışındaki rollerin kullanıcı listeleyemediğini/rol değiştiremediğini test et.
- [ ] SuperAdmin'in geçerli rol atayıp kaldırabildiğini test et.
- [ ] Bilinmeyen rol, olmayan kullanıcı ve eşzamanlı güncelleme senaryolarını test et.
- [ ] Son SuperAdmin'in kendisini veya başka son SuperAdmin'i düşüremediğini test et.
- [ ] Bootstrap işleminin tekrar çalıştırıldığında ikinci hesap/rol üretmediğini test et.

## Kabul kriterleri

- Public kayıt ekranı ve API'si rol seçtirmez.
- Varsayılan veya repoya yazılmış SuperAdmin parolası yoktur.
- Rol atama yalnızca doğrulanmış `SuperAdmin` tarafından yapılabilir.
- Son SuperAdmin kilitlenemez veya rolü kaldırılamaz.
- Katman bağımlılıkları korunur ve Api içinde Identity iş kuralı bulunmaz.
- Unit ve integration testleri ile build uyarısız geçer.

## Migration

ASP.NET Core Identity rol ve kullanıcı-rol tabloları zaten bulunduğundan yalnızca
`SuperAdmin` rolü ve endpoint'ler eklenirse migration gerekmez. Yeni kalıcı audit
alanı veya tablo eklenirse migration ayrıca üretilmelidir.
