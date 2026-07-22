# Görev 10 — Frontend MVP sertleştirme

## Amaç

Mevcut arayüzü backend sözleşmesiyle tutarlı, güvenli ve gerçek kullanıcı akışlarında
doğru çalışan yayınlanabilir MVP haline getirmek.

## P0 — Yayın engelleri

### F10-01 — Yorum zaman çizelgesini düzelt

- [ ] Backend'in “en yeni ilk sayfa” sözleşmesine göre sayfaları birleştir.
- [ ] Görünümde eski → yeni kronolojik akışı korurken “daha eski” düğmesini gerçekten eski sayfalara bağla.
- [ ] Yeni yorum sonrası son yorumu görünür yap ve duplicate üretme.
- [ ] 20 ve 21+ yorum için component ve gerçek API contract testi ekle.

### F10-02 — Gerçek logout ve sekmeler arası refresh koordinasyonu

- [ ] Logout endpoint'ini çağır; sonuçtan bağımsız olarak istemci belleğini güvenli biçimde temizle.
- [ ] Refresh işlemini `BroadcastChannel` veya eşdeğer mekanizmayla sekmeler arasında koordine et.
- [ ] Aynı token ailesini iki sekmenin eşzamanlı yenilemesi senaryosunu test et.
- [ ] Logout olayını diğer açık sekmelere yay.

### F10-03 — Yetki görünürlüğünü backend matrisiyle eşleştir

- [ ] Proje sahibi/Admin için görev oluşturma, tüm alanları düzenleme, atama ve silme aksiyonlarını göster.
- [ ] Atanmış üyeye yalnız durum değiştirme kontrolünü göster.
- [ ] Diğer üyeler için yazma aksiyonlarını gizle veya salt okunur göster.
- [ ] Route mock yerine gerçek backend ile owner, assignee, member ve Admin senaryolarını test et.

### F10-04 — Silme dilini veri yaşam döngüsüyle eşleştir

- [ ] Proje ve görev için “kalıcı silme” iddiasını kaldır; erişimden kaldırıldığını doğru anlat.
- [ ] Yorum ve eklerde silme endpoint'i yoksa arayüzde silme vaadi sunma.
- [ ] Restore/retention uygulanana kadar kullanıcıya kesin saklama süresi bildirme.

## P1 — MVP çıkış işleri

### F10-05 — Bildirim merkezini düzelt

- [ ] Zil rozetini ilk 10 kayıttan değil backend unread count endpoint'inden besle.
- [ ] “Tümünü okundu” işlemini tek backend endpoint'iyle bütün kayıtlar için çalıştır.
- [ ] Yapılandırılmış bildirim tiplerini Türkçe UI metnine dönüştür.
- [ ] `projectId`/`taskId` ile ilgili proje veya göreve navigasyon ekle.
- [ ] Boş durumdaki “yorumlar burada görünür” metnini gerçek üretilen olaylarla tutarlı yap.

### F10-06 — Optimistic concurrency deneyimi

- [ ] Edit formunda API version/ETag değerini sakla ve update isteğine ekle.
- [ ] Conflict durumunda kullanıcının girdisini kaybetmeden güncel veriyi karşılaştırma/yenileme akışı sun.
- [ ] Stale task edit E2E testi ekle.

### F10-07 — Production build'i ağdan bağımsız yap

- [ ] Google fontlarını repoda barındırılan font veya güvenli system font stack ile değiştir.
- [ ] Ağ erişimi olmayan ortamda `npm run build` çalıştır.

### F10-08 — Tarayıcı güvenlik tabanı

- [ ] CSP, `X-Content-Type-Options`, referrer ve frame politikalarını Next.js headers ile ekle.
- [ ] Token saklama hedefini backend cookie sözleşmesiyle birlikte değerlendir; localStorage devam edecekse XSS riskini ve geçiş planını belgele.
- [ ] Inline script/style ihtiyaçlarını production CSP ile doğrula.

### F10-09 — Gerçek full-stack kalite kapısı

- [ ] En az auth, proje silme izolasyonu, görev yetkisi, yorum sayfalama ve logout için gerçek backend E2E suite ekle.
- [ ] “Yetkisiz görev silme” testini adı ve aktörüyle uyumlu hale getir.
- [ ] Route-mock suite'i hızlı UI testi olarak koru; contract kanıtı olarak etiketleme.

## Doğrulama

- [ ] Lint, typecheck, unit/component ve E2E testleri geçer.
- [ ] Production build ağsız ortamda geçer.
- [ ] OpenAPI client Backend Görev 10 sözleşmelerinden yeniden üretilmiştir.
- [ ] Masaüstü ve mobilde yorum, bildirim, yetki ve conflict durumları doğrulanmıştır.

## Bağımlılıklar

F10-01, F10-02, F10-05 ve F10-06 sırasıyla Backend B10-04, B10-03, B10-07 ve
B10-06 tamamlandıktan sonra kapanabilir.
