# Görev 10 — MVP güvenlik ve tutarlılık

## Amaç

Mevcut API'yi veri izolasyonu, tutarlı yetkilendirme, doğru sayfalama ve iptal edilebilir
oturumlar açısından yayınlanabilir MVP seviyesine getirmek.

## Öncelik ve sıra

P0 maddeleri tamamlanmadan sürüm yayınlanmaz. P1 maddeleri MVP çıkış kriteridir ve aynı
sürüm adayı içinde tamamlanır. Her madde ilgili integration veya contract testiyle kapanır.

## P0 — Yayın engelleri

### B10-01 — Silinmiş proje alt kaynaklarını kapat

- [ ] `IsMemberAsync` ve bütün proje kapsamlı yetki sorgularını aktif proje varlığıyla sınırla.
- [ ] Görev, istatistik, yorum ve ek endpoint'lerinin soft-delete edilmiş proje için `404` döndürmesini sağla.
- [ ] Admin bypass yolunun da soft-delete filtresini aşmamasını sağla.
- [ ] Proje silme işleminde üyeliklerin korunması/silinmesi kararını restore stratejisiyle uyumlu belgele.
- [ ] Eski üye ve bilinen proje/görev ID'si senaryolarını integration testle kapsa.

Kabul: Silinmiş bir projenin hiçbir alt kaynağı normal API üzerinden okunamaz veya değiştirilemez.

### B10-02 — Görev yetki matrisini tekleştir

- [ ] `ProjectOwner`, `Admin` ve atanmış üye davranışlarını merkezi authorization policy/service içinde tanımla.
- [ ] Proje sahibi veya Admin'e oluşturma, tüm alanları düzenleme, yeniden atama ve silme yetkisi ver.
- [ ] Atanmış üyeyi yalnız izin verilen durum geçişleriyle sınırla; diğer üyelerin yazma işlemlerini reddet.
- [ ] Sistem `ProjectManager` rolüne bağlı tekil görev silme istisnasını kaldır.
- [ ] Her işlem/aktör kombinasyonu için unit ve integration yetki matrisi testi ekle.

Kabul: Aynı aktörün daha yüksek etkili proje işlemini yapıp tekil görev işleminde anlamsız biçimde reddedildiği bir durum kalmaz.

### B10-03 — Sunucu taraflı logout ve token iptali

- [ ] Refresh token alan ve token ailesini iptal eden `POST /api/auth/logout` endpoint'i ekle.
- [ ] Logout işlemini idempotent yap; süresi dolmuş veya daha önce iptal edilmiş token davranışını tanımla.
- [ ] Çıkış sonrası aynı refresh token'ın kullanılamadığını integration testle doğrula.
- [ ] Audit kaydına token değeri yazmadan logout/revoke olayını ekle.

Kabul: Kullanıcı çıkış yaptıktan sonra ele geçirilmiş eski refresh token yeni access token üretemez.

## P1 — MVP çıkış işleri

### B10-04 — Yorum sayfalama sözleşmesini düzelt

- [ ] API'nin ilk sayfada en yeni yorumları döndürmesini sözleşme olarak seç ve OpenAPI açıklamasına ekle.
- [ ] Stabil sıralama için `CreatedAtUtc` ve `Id` azalan sırasını kullan.
- [ ] 0, 1, 20, 21 ve aynı timestamp'e sahip yorum senaryolarını test et.

### B10-05 — Gecikmiş görev güncellemesini düzelt

- [ ] Update doğrulamasında geçmiş due date'i yalnız tarih gerçekten değiştiriliyorsa reddet.
- [ ] Gecikmiş görevin başlık, açıklama, atanan kişi ve durum değişikliklerini test et.

### B10-06 — Gerçek optimistic concurrency sözleşmesi ekle

- [ ] Görev ve gerekiyorsa proje response'larına opaque `version` veya HTTP `ETag` ekle.
- [ ] Update request'inde versiyon/`If-Match` zorunluluğunu ve hata sözleşmesini belirle.
- [ ] Stale update'i `409` veya `412` ile reddet ve güncel kaydı ezme.
- [ ] İki istemcinin aynı kaydı düzenlediği integration testi ekle.

### B10-07 — Bildirim sözleşmesini tamamla

- [ ] Bildirime navigasyon için `projectId`, `taskId` ve yapılandırılmış `type` alanlarını ekle.
- [ ] Toplam okunmamış sayısı endpoint'i ekle.
- [ ] Tek istekte tüm bildirimleri okundu yapan idempotent endpoint ekle.
- [ ] Kullanıcıya gösterilecek metni backend'de sabitlemek yerine lokalize edilebilir olay verisi taşı.

### B10-08 — Admin proje görünümünü tutarlı yap

- [ ] Admin için bütün aktif projeleri sayfalı listeleyen yönetim endpoint'i ekle veya “tam erişim” iddiasını kaldır.
- [ ] Normal kullanıcı proje listesinin yalnız üyelikleri döndürdüğünü koru.
- [ ] Silinmiş projelerin varsayılan listede yer almadığını test et.

### B10-09 — Operasyon ve güvenlik tabanını düzelt

- [ ] Login için genel API limitinden ayrı, daha sıkı rate limit ve lockout politikası ekle.
- [ ] Production e-posta ve malware scanner adapter'larının stub ile açılmasını startup doğrulamasıyla engelle.
- [ ] Compose içinde Redis host portunu varsayılan olarak yayınlama; gerektiğinde profile/override ile aç.

## Doğrulama

- [ ] `dotnet test` tüm suite için geçer.
- [ ] Silinmiş proje, 20+ yorum, logout revoke, stale update ve yetki matrisi integration testleri CI'da çalışır.
- [ ] OpenAPI çıktısı frontend client üretimiyle uyumludur.
- [ ] PRD ve MVP çıkış kriterlerinin her biri test veya açık manuel doğrulama kaydıyla eşlenmiştir.

## Bağımlılıklar

Frontend Görev 10; B10-02, B10-03, B10-04, B10-06 ve B10-07 sözleşmelerine bağlıdır.
