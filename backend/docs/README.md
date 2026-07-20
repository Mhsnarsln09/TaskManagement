# TaskManagement Geliştirme Rehberi

Bu klasör, TaskManagement API'sini geliştirirken izlenecek ürün ve teknik yol haritasıdır. Amaç yalnızca çalışan bir uygulama çıkarmak değil; alınan kararların nedenlerini anlayarak ASP.NET Core backend geliştirmeyi öğrenmektir.

## Okuma sırası

1. [PRD](PRD.md): Ürünün problemi, kullanıcıları ve davranışları
2. [MVP](MVP.md): İlk yayın kapsamı ve özellikle kapsam dışı bırakılanlar
3. [Teknik Kararlar](TECHNICAL-DECISIONS.md): Mimari yaklaşım ve gerekçeleri
4. [Yerel Geliştirme](DEVELOPMENT.md): Secret ve PostgreSQL kurulumu
5. [Frontend Entegrasyonu](../../frontend/docs/FRONTEND-INTEGRATION.md): Auth, SignalR, CORS ve hata sözleşmesi
6. Aşağıdaki görev dosyaları: Uygulama sırası

## Çalışma yöntemi

Her görev için aynı döngü izlenir:

1. Görevin açıklamasını ve "Neden?" bölümünü oku.
2. Küçük bir değişiklik yap.
3. Uygulamayı derle ve ilgili testi çalıştır.
4. Swagger/Postman üzerinden davranışı doğrula.
5. Kabul kriterlerinin tamamlandığından emin ol.
6. Değişikliği küçük ve anlamlı bir Git commit'i olarak kaydet.

Bir görev tamamlanmadan sonraki göreve geçilmemelidir. Görevlerdeki kodlar doğrudan kopyalanmak yerine önce veri akışı ve bağımlılık yönü anlaşılmalıdır.

## Görev sırası

- [00 — Başlangıç ve geliştirme ortamı](tasks/00-foundation.md)
- [01 — Domain modeli](tasks/01-domain-model.md)
- [02 — EF Core ve PostgreSQL](tasks/02-persistence.md)
- [03 — Temel proje ve görev API'leri](tasks/03-core-api.md)
- [04 — Identity ve JWT](tasks/04-authentication.md)
- [05 — Yetkilendirme ve iş kuralları](tasks/05-business-rules.md)
- [06 — Yorum, dosya ve istatistik](tasks/06-mvp-completion.md)
- [07 — Testler ve kalite kapıları](tasks/07-testing.md)
- [08 — MVP sonrası profesyonelleştirme](tasks/08-post-mvp.md)
- [09 — SuperAdmin ve rol yönetimi](tasks/09-super-admin-role-management.md)

## Definition of Done

Bir görev ancak aşağıdakiler sağlandığında tamamlanmış sayılır:

- Proje uyarısız veya açıklanmış uyarılarla derleniyor.
- İlgili otomatik testler geçiyor.
- API davranışı Swagger/Postman ile doğrulanmış.
- Hatalı ve yetkisiz istek senaryoları düşünülmüş.
- Hassas bilgi kaynak koda yazılmamış.
- Katman sınırları `TECHNICAL-DECISIONS.md` içindeki kurallara uygun.
- Application katmanı Infrastructure/API referansı almıyor; Api katmanı business use-case, DTO, validator veya persistence implementasyonu içermiyor.
- Kabul kriterlerinin her biri karşılanmış.
