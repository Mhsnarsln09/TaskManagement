# Görev 11 — MVP sonrası ürün evrimi

## Amaç

MVP güvenlik işleri tamamlandıktan sonra ekip yönetimi, keşfedilebilirlik ve iş birliği
özelliklerini bağımsız teslim edilebilir dilimlere ayırmak.

## Ürün işleri

### B11-01 — Gelişmiş görev sorgulama

- [ ] Tam metin arama sözleşmesini ve aranacak alanları belirle.
- [ ] Atanan kişi, tarih aralığı, gecikenler ve “bana atananlar” filtrelerini ekle.
- [ ] Filtre, sıralama ve sayfalamanın birlikte stabil çalışmasını test et.

### B11-02 — Kanban veri ve eşzamanlılık modeli

- [ ] Durum kolonlarını mevcut task status modeliyle eşleştir.
- [ ] Kolon içi sıralama anahtarı ve yeniden sıralama endpoint'i tasarla.
- [ ] Drag-drop durum geçişlerini mevcut domain kuralları ve optimistic concurrency ile koru.
- [ ] Aynı kolonu eşzamanlı düzenleyen istemciler için conflict davranışını test et.

### B11-03 — Proje içi roller

- [ ] `Owner`, `Manager`, `Contributor`, `Viewer` yetki matrisini ürün kararı olarak onayla.
- [ ] Üyelik modeline proje rolü ekle ve veri migration stratejisini hazırla.
- [ ] Sistem rolleriyle proje rollerinin öncelik sırasını belgeleyip policy testleri yaz.

### B11-04 — Davet, üyelik onayı ve sahiplik devri

- [ ] Süreli davet, kabul/ret, iptal ve yeniden gönderme akışlarını ekle.
- [ ] Proje sahipliği devrini atomik işlem ve audit kaydıyla uygula.
- [ ] Hesap kapatma öncesi sahip olunan projeler için zorunlu çözüm akışı tanımla.

### B11-05 — Veri yaşam döngüsü

- [ ] Proje/görev restore endpoint'leri ve yetkilerini tasarla.
- [ ] Yorum, attachment metadata ve fiziksel dosya için retention politikasını belirle.
- [ ] Süresi dolan fiziksel dosyaları idempotent background job ile temizle.

### B11-06 — İş birliği ve aktivite

- [ ] Yorum düzenleme/silme ve attachment silme kurallarını ekle.
- [ ] Mention, etiket ve alt görevleri ayrı veri modeli değişiklikleri olarak planla.
- [ ] Audit kayıtlarını filtrelenebilir aktivite geçmişi API'sine dönüştür.
- [ ] Yorum, üyelik, görev silme ve proje değişikliği olaylarından bildirim üret.

### B11-07 — Hesap güvenliği ve oturum yönetimi

- [ ] E-posta doğrulama, şifre sıfırlama/değiştirme ve profil düzenleme akışlarını ekle.
- [ ] Aktif oturumları listeleme ve tekil/tüm oturumları iptal etme endpoint'leri ekle.
- [ ] Hesap kapatma için sahiplik, audit ve retention etkilerini çöz.

## Sıralama

1. B11-01 bağımsız teslim edilebilir.
2. B11-02, Backend Görev 10 optimistic concurrency işine bağlıdır.
3. B11-03 tamamlanmadan B11-04 başlatılmaz.
4. B11-05 için restore/retention ürün kararı gerekir.
5. B11-06 ve B11-07 ayrı sürümlere bölünebilir.

## Kabul ilkesi

Her başlık ayrı epic olarak ele alınır; veri modeli, authorization matrisi, API sözleşmesi,
integration testleri ve frontend karşılığı olmadan tamamlandı işaretlenmez.
