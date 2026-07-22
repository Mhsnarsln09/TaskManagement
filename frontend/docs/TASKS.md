# Frontend Görevleri

> Bu dosyadaki işaretler ilk uygulama kapsamının kodlandığını gösterir; yayın olgunluğu
> anlamına gelmez. Güncel yayın engelleri [10 — MVP sertleştirme](tasks/10-mvp-hardening.md),
> yeni ürün özellikleri [11 — Ürün evrimi](tasks/11-product-evolution.md) dosyasındadır.

## 00 - Tasarım

- [x] Claude Design prompt'unu kullanarak tüm MVP ekranlarını üret
- [x] Masaüstü ve mobil varyantları netleştir
- [x] Loading, empty, error, permission ve confirmation durumlarını tamamla
- [x] Auth, üyeler, bildirimler, proje ayarları ve SuperAdmin ekranları için mobil varyantları tamamla
- [x] `400`, `401`, `403`, `404`, `409`, `429` ve ağ hataları için ortak Problem Details gösterim matrisini netleştir (bkz. DESIGN-DECISIONS.md §8)
- [x] Başarı, submitting/disabled ve veri yenileme sonrası toast durumlarını ekran bazında netleştir
- [x] Tasarım tokenlarını ve kullanılacak shadcn bileşenlerini çıkar
- [x] Her ekranı backend request/response alanlarıyla karşılaştır; sözleşmede olmayan kontrolü kaldır (kararlar: DESIGN-DECISIONS.md)

## 01 - Temel kurulum

- [x] Next.js App Router + TypeScript projesini oluştur
- [x] Tailwind CSS, shadcn/ui ve Lucide icons yapılandır
- [x] ESLint, formatter, test ve environment doğrulamasını ekle
- [x] Route grupları, layout'lar ve feature tabanlı klasör yapısını kur
- [x] Önce ortak UI primitive ve composite componentleri oluştur; feature ekranlarında bunları kullan

## 02 - API altyapısı

- [x] OpenAPI'den TypeScript tipleri/client üret (`npm run openapi`)
- [x] API base URL, Problem Details ve istek iptalini yapılandır
- [x] Auth session ve tekil refresh kuyruğunu uygula
- [x] SignalR client, reconnect ve cache yenileme davranışını ekle

## 03 - Auth

- [x] Giriş ve kayıt ekranlarını uygula
- [x] Korumalı rotaları ve çıkış akışını uygula
- [x] Validation, expired session ve unauthorized durumlarını test et

## 04 - Uygulama kabuğu

- [x] Responsive sidebar/header ve proje seçiciyi uygula
- [x] SuperAdmin için sidebar'da Yönetim/Kullanıcılar bağlantısını, diğer roller için gizli durumunu uygula
- [x] Bildirim merkezi ve kullanıcı menüsünü uygula
- [x] Hesap menüsü içinde kullanıcı bilgisi, çıkış ve oturum süresi doldu yönlendirme akışını uygula
- [x] Global loading, toast, error boundary, 403 ve 404 ekranlarını ekle

## 05 - Projeler

- [x] Proje listesi ve oluşturma/düzenleme/silme akışlarını uygula
- [x] Proje satır eylem menüsünde görüntüle, düzenle ve onaylı silme davranışlarını uygula
- [x] Proje özetini ve istatistiklerini uygula
- [x] Üye listesi, ekleme ve çıkarma akışlarını uygula (ekleme isimle arama ile; bkz. DESIGN-DECISIONS.md §1)

## 06 - Görevler

- [x] Sayfalı, filtrelenebilir ve sıralanabilir görev listesini uygula
- [x] Görev oluşturma, detay, düzenleme ve silme akışlarını uygula
- [x] Durum, öncelik, atanan kişi, due date ve overdue sunumunu uygula

## 07 - İş birliği

- [x] Yorum listesi ve yorum eklemeyi uygula
- [x] Ek dosya yükleme, listeleme ve indirmeyi uygula
- [x] Gerçek zamanlı bildirim davranışlarını tamamla
- [x] Bildirimleri tek tek ve topluca okundu işaretleme aksiyonlarını uygula (toplu: istemci tarafı paralel istek — DESIGN-DECISIONS.md §3)

## 08 - Kalite ve yayın

- [x] Responsive ve erişilebilirlik kontrollerini tamamla
- [x] Unit/component testlerini ekle
- [x] Kritik E2E testlerini ekle (Playwright; auth, projeler, görevler, yetki/hata ve mobil varyantlar — API route mock'larıyla)
- [x] Lint, typecheck, test ve build adımlarını CI'a ekle
- [x] Kurulum ve geliştirme belgelerini tamamla (frontend/README.md)
- [x] Deployment belgesini tamamla (docs/DEPLOYMENT.md)

## 09 - SuperAdmin

- [x] SuperAdmin'e özel kullanıcı listesi, arama ve pagination ekranını uygula
- [x] Çoklu rol değiştirme formunu ve son SuperAdmin conflict durumunu uygula
- [x] SuperAdmin olmayan kullanıcılardan navigasyonu ve route'u koru
- [x] SuperAdmin yönetim ekranının mobil görünümünü ve dar tablo davranışını uygula
- [x] Rolü değişen hedef kullanıcının geçersiz oturum davranışını test et (401 → tekil refresh → başarısızsa oturum-süresi-doldu yönlendirmesi; client.test.ts)

## Devam eden işler

- [ ] [10 — MVP sertleştirme ve sözleşme düzeltmeleri](tasks/10-mvp-hardening.md)
- [ ] [11 — MVP sonrası ürün evrimi](tasks/11-product-evolution.md)
