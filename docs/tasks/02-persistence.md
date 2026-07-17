# Görev 02 — EF Core ve PostgreSQL

## Amaç

Domain modelini ilişkisel veritabanına doğru kısıtlar ve tekrar üretilebilir migration'larla taşımak.

## Neden?

Nesne ilişkileri ile ilişkisel model aynı şey değildir. Foreign key, unique constraint, index ve delete behavior kararları veri bütünlüğünü yalnızca uygulama koduna bırakmaz.

## Yapılacaklar

- [x] EF Core ve Npgsql paketlerini Infrastructure projesine ekle.
- [x] Identity ile uyumlu `ApplicationDbContext` oluştur.
- [x] Her entity için `IEntityTypeConfiguration<T>` mapping yaz.
- [x] Metin uzunluklarını, zorunlu alanları ve kolon türlerini belirle.
- [x] `(ProjectId, UserId)` üzerine unique constraint ekle.
- [x] Sık sorgulanacak task alanları için ölçülü indeksler ekle: project, assignee, status, due date.
- [x] Cascade/restrict davranışlarını bilinçli belirle; bir projeden üye çıkarmanın görevler üzerindeki etkisini ele al.
- [x] İlk migration'ı üret ve boş PostgreSQL veritabanına uygula.
- [x] Development seed içinde rollerin oluşturulmasını planla; gerçek kullanıcı parolası seed etme.
- [x] DbContext'i API dependency injection container'ına kaydet.

## Persistence kararları

- EF Core ve Npgsql yalnızca Infrastructure projesindedir; Domain projesi veritabanı bağımlılığı taşımaz.
- `ApplicationDbContext`, `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>` üzerinden kuruldu. Böylece auth tabloları ile domain tabloları aynı migration içinde yönetilebilir.
- `ApplicationUser` Domain içinde değil Infrastructure içinde tutulur; çünkü ASP.NET Core Identity uygulama altyapısıdır.
- Enum alanları string olarak saklanır. Bu, veritabanında `Completed` gibi okunabilir değerler üretir; enum sırası değişirse veri bozulmaz.
- `ProjectMember` üzerinde `(ProjectId, UserId)` unique index vardır. Böylece aynı kullanıcı aynı projeye iki kez eklenemez.
- Task sorguları için `ProjectId`, `AssigneeUserId`, `Status`, `DueDate` indekslendi. Bunlar listeleme ve filtreleme ekranlarında sık kullanılacak alanlardır.
- `Project` silinirse üyelikler ve görevler cascade silinir. Görev silinirse yorumlar cascade silinir.
- Kullanıcı silinirse proje sahipliği, üyelik ve yorum yazarlığı restrict davranır; geçmiş sahiplik/yazarlık verisi bozulmaz.
- Kullanıcı silinirse atanmış görevlerde `AssigneeUserId` null yapılır. Bir projeden üye çıkarmak görevi silmez; sadece atama uygulama kuralıyla ayrıca ele alınır.
- Development seed aşamasında roller oluşturulabilir; gerçek kullanıcı parolası seed edilmeyecektir.
- Connection string kaynak koda veya production appsettings dosyasına yazılmadı. Lokal değerler environment variable/user-secrets üzerinden verilecektir.

## Öğrenme başlıkları

- Change tracking ve entity state
- Migration ile database update farkı
- Navigation, foreign key ve join entity
- Eager loading, projection ve N+1 problemi
- Tracking ve `AsNoTracking`

## Kabul kriterleri

- Veritabanı yalnızca migration'larla sıfırdan kurulabiliyor.
- Aynı kullanıcı aynı projeye iki kez eklenemiyor.
- Gerekli foreign key ve indeksler veritabanında mevcut.
- Connection string kaynak koda veya repodaki production ayarına yazılmamış.

## Önerilen commit

```text
feat(persistence): add ef core mappings and initial migration
```
