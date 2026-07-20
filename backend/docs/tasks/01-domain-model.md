# Görev 01 — Domain modeli

## Amaç

Uygulamanın temel kavramlarını ve ilişkilerini HTTP/veritabanı ayrıntılarına girmeden modellemek.

## Neden?

Entity'ler yalnızca tablo karşılığı değildir; sistemin geçerli durumlarını ve kurallarını temsil eder. Önce domain dili netleşirse migration ve endpoint tasarımı daha az değişir.

## Yapılacaklar

- [x] Ortak kimlik ve audit alanlarının gerçekten gerekli olanlarını belirle.
- [x] `ApplicationUser` için Identity kararını not et; implementasyonu Infrastructure aşamasında yap.
- [x] `Project` entity'sini oluştur: ad, açıklama, oluşturucu/yönetici, tarihler.
- [x] `ProjectMember` join entity'sini oluştur: project id, user id, katılım tarihi.
- [x] `TaskItem` oluştur: başlık, açıklama, status, priority, due date, assignee.
- [x] `Comment` oluştur: içerik, yazar, task ve tarih.
- [x] `TaskStatus` ve `TaskPriority` enum'larını oluştur. `TaskStatus` adının BCL çakışmasını değerlendir; gerekirse `WorkItemStatus` kullan.
- [x] Entity ilişkilerinin cardinality ve silme davranışlarını bir diyagram/not ile açıkla.
- [x] Durum geçişini entity metodu veya domain service ile koru.
- [x] Geçmiş son tarih bilgisini saklamak yerine hesaplanan davranış olarak modelle.
- [x] Domain kuralları için saf unit testler yaz.

## Önemli kararlar

- Kimlik için başlangıçta `Guid` kullanılması dağıtık sistemlere uygundur; ancak okunabilirlik ve indeks maliyeti tartışılmalıdır.
- `Completed` görevi güncelleme yasağı controller'da değil domain/application kuralında olmalıdır.
- Navigation property'ler API response modeli değildir.
- `ApplicationUser` domain projesinde implement edilmez; ASP.NET Core Identity bağımlılığı Infrastructure aşamasında eklenir.
- `TaskStatus` yerine BCL'deki `System.Threading.Tasks.TaskStatus` ile çakışmaması için `WorkItemStatus` kullanılır.

## İlişki notları

- `Project` 1-N `TaskItem` ilişkisinin köküdür; her görev mutlaka bir `ProjectId` taşır.
- `Project` N-N kullanıcı ilişkisi `ProjectMember` join entity ile temsil edilir.
- `TaskItem` 1-N `Comment` ilişkisinin sahibidir; her yorum mutlaka bir `TaskItemId` taşır.
- Kullanıcı ilişkileri şimdilik `Guid` user id ile tutulur; gerçek Identity modeli Infrastructure katmanında kurulacaktır.
- Silme davranışları EF Core mapping aşamasında netleştirilecektir. Domain katmanı veritabanı cascade davranışını bilmez.

## Kabul kriterleri

- Domain projesi EF Core, ASP.NET Core veya Identity paketi referans etmiyor.
- Geçersiz durum geçişi test ile reddediliyor.
- Bir görev proje dışında bir kavrama bağlı olamıyor.
- Entity invariant'ları constructor/metotlar veya application sınırında açıkça korunuyor.

## Önerilen commit

```text
feat(domain): model projects tasks memberships and comments
```
