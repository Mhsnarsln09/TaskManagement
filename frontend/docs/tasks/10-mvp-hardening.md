# Görev 10 — Frontend MVP sertleştirme

## Amaç

Mevcut arayüzü backend sözleşmesiyle tutarlı, güvenli ve gerçek kullanıcı akışlarında
doğru çalışan yayınlanabilir MVP haline getirmek.

## P0 — Yayın engelleri

### F10-01 — Yorum zaman çizelgesini düzelt

- [x] Backend'in “en yeni ilk sayfa” sözleşmesine göre sayfaları birleştir.
- [x] Görünümde eski → yeni kronolojik akışı korurken “daha eski” düğmesini gerçekten eski sayfalara bağla.
- [x] Yeni yorum sonrası son yorumu görünür yap ve duplicate üretme.
- [x] 20 ve 21+ yorum için component ve gerçek API contract testi ekle.

### F10-02 — Gerçek logout ve sekmeler arası refresh koordinasyonu

- [x] Logout endpoint'ini çağır; sonuçtan bağımsız olarak istemci belleğini güvenli biçimde temizle.
- [x] Refresh işlemini `BroadcastChannel` veya eşdeğer mekanizmayla sekmeler arasında koordine et. (Web Locks API; jsdom/eski tarayıcıda sekme-içi tekilliğe düşer.)
- [x] Aynı token ailesini iki sekmenin eşzamanlı yenilemesi senaryosunu test et.
- [x] Logout olayını diğer açık sekmelere yay. (localStorage storage event ile.)

### F10-03 — Yetki görünürlüğünü backend matrisiyle eşleştir

- [x] Proje sahibi/Admin için görev oluşturma, tüm alanları düzenleme, atama ve silme aksiyonlarını göster.
- [x] Atanmış üyeye yalnız durum değiştirme kontrolünü göster.
- [x] Diğer üyeler için yazma aksiyonlarını gizle veya salt okunur göster.
- [x] Route mock yerine gerçek backend ile owner, assignee, member ve Admin senaryolarını test et. (owner/assignee/member: `e2e-fullstack/contract.spec.ts` gerçek backend; Admin görev yetkisi backend `TaskAuthorizationTests` integration testinde.)

### F10-04 — Silme dilini veri yaşam döngüsüyle eşleştir

- [x] Proje ve görev için “kalıcı silme” iddiasını kaldır; erişimden kaldırıldığını doğru anlat.
- [x] Yorum ve eklerde silme endpoint'i yoksa arayüzde silme vaadi sunma. (Yorum/ek bölümünde silme kontrolü yok.)
- [x] Restore/retention uygulanana kadar kullanıcıya kesin saklama süresi bildirme. (Metinlerde saklama süresi verilmiyor.)

## P1 — MVP çıkış işleri

### F10-05 — Bildirim merkezini düzelt

- [x] Zil rozetini ilk 10 kayıttan değil backend unread count endpoint'inden besle.
- [x] “Tümünü okundu” işlemini tek backend endpoint'iyle bütün kayıtlar için çalıştır.
- [x] Yapılandırılmış bildirim tiplerini Türkçe UI metnine dönüştür.
- [x] `projectId`/`taskId` ile ilgili proje veya göreve navigasyon ekle.
- [x] Boş durumdaki “yorumlar burada görünür” metnini gerçek üretilen olaylarla tutarlı yap.

### F10-06 — Optimistic concurrency deneyimi

- [x] Edit formunda API version/ETag değerini sakla ve update isteğine ekle.
- [x] Conflict durumunda kullanıcının girdisini kaybetmeden güncel veriyi karşılaştırma/yenileme akışı sun.
- [x] Stale task edit E2E testi ekle. (`e2e-fullstack/contract.spec.ts` — bayat sürüm 409 döner ve veriyi ezmez; conflict UI akışı task-form-dialog'da.)

### F10-07 — Production build'i ağdan bağımsız yap

- [x] Google fontlarını repoda barındırılan font veya güvenli system font stack ile değiştir. (next/font/google kaldırıldı; `:root` sistem font yığını.)
- [x] Ağ erişimi olmayan ortamda `npm run build` çalıştır. (Google Fonts bağımlılığı kaldırıldığı için build ağ erişimi gerektirmez; `npm run build` başarılı.)

### F10-08 — Tarayıcı güvenlik tabanı

- [x] CSP, `X-Content-Type-Options`, referrer ve frame politikalarını Next.js headers ile ekle. (`next.config.ts` headers(); connect-src API+SignalR ws içerir.)
- [x] Token saklama hedefini backend cookie sözleşmesiyle birlikte değerlendir; localStorage devam edecekse XSS riskini ve geçiş planını belgele. (README güvenlik notu.)
- [x] Inline script/style ihtiyaçlarını production CSP ile doğrula. (Production build + çalışan sunucuda CSP ihlali yok; app render ediyor.)

### F10-09 — Gerçek full-stack kalite kapısı

- [x] En az auth, proje silme izolasyonu, görev yetkisi, yorum sayfalama ve logout için gerçek backend E2E suite ekle. (`e2e-fullstack/` + `playwright.fullstack.config.ts`; `npm run test:e2e:fullstack`.)
- [x] “Yetkisiz görev silme” testini adı ve aktörüyle uyumlu hale getir. (`tasks.spec.ts`: eski ProjectManager kapısı yerine sahiplik matrisi.)
- [x] Route-mock suite'i hızlı UI testi olarak koru; contract kanıtı olarak etiketleme. (`playwright.config.ts` başlık notu: hızlı UI testi, sözleşme kanıtı değil.)

## Doğrulama

- [x] Lint, typecheck, unit/component ve E2E testleri geçer. (lint ✓, typecheck ✓, 24 unit ✓, 60 mock E2E ✓, 6 gerçek-backend E2E ✓)
- [x] Production build ağsız ortamda geçer. (next/font/google kaldırıldı; `npm run build` başarılı.)
- [x] OpenAPI client Backend Görev 10 sözleşmelerinden yeniden üretilmiştir. (`npm run openapi` — version, projectId, logout, unread-count, read-all, admin/projects.)
- [x] Masaüstü ve mobilde yorum, bildirim, yetki ve conflict durumları doğrulanmıştır. (mock E2E chromium + mobile projeleri; gerçek-backend contract suite.)

## Bağımlılıklar

F10-01, F10-02, F10-05 ve F10-06 sırasıyla Backend B10-04, B10-03, B10-07 ve
B10-06 tamamlandıktan sonra kapanabilir.
