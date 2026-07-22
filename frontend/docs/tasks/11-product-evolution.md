# Görev 11 — Frontend MVP sonrası ürün evrimi

## Amaç

MVP sertleştirmesi tamamlandıktan sonra yeni ürün kabiliyetlerini backend epic'leriyle
eşleşen, bağımsız doğrulanabilir frontend işleri olarak planlamak.

## Ürün işleri

### F11-01 — Gelişmiş görev bulma

- [ ] Başlık/açıklama araması ekle.
- [ ] Atanan kişi, tarih aralığı, gecikenler ve “bana atananlar” filtrelerini ekle.
- [ ] Filtreleri URL query state'inde paylaşılabilir ve geri tuşuyla uyumlu tut.
- [ ] Filtre kombinasyonları, boş sonuç ve pagination testlerini ekle.

### F11-02 — Kanban görünümü

- [ ] Liste/Kanban görünüm seçimini segmented control ile ekle.
- [ ] Status kolonları, görev kartları ve kolon içi sıralamayı erişilebilir drag-drop ile uygula.
- [ ] Klavye ile taşıma ve mobil için drag-drop dışı durum değiştirme yolu sağla.
- [ ] Optimistic update, rollback ve eşzamanlı conflict durumlarını tasarla ve test et.

### F11-03 — Proje rolleri ve davetler

- [ ] Üye listesinde proje rolü görüntüleme/değiştirme akışını ekle.
- [ ] Davet gönderme, bekleyen davetler, kabul/ret, iptal ve yeniden gönderme ekranlarını ekle.
- [ ] Sahiplik devri için güçlü onay ve yeni sahip kabul akışı ekle.
- [ ] Kullanıcının gerçek proje rolüne göre bütün aksiyon görünürlüklerini test et.

### F11-04 — Veri yaşam döngüsü

- [ ] Silinen proje/görev görünümü ve geri yükleme akışını ekle.
- [ ] Saklama süresini ve kalıcı silinme tarihini anlaşılır biçimde göster.
- [ ] Yorum ve ek silme işlemlerinde backend yaşam döngüsüyle aynı dili kullan.

### F11-05 — İş birliği ve aktivite

- [ ] Yorum düzenleme/silme ve attachment silme kontrollerini ekle.
- [ ] Mention autocomplete, etiketler ve alt görevleri ayrı feature dilimleri olarak uygula.
- [ ] Proje/görev aktivite geçmişini filtrelenebilir zaman çizelgesi olarak sun.
- [ ] Yeni bildirim tipleri için kaynak navigasyonu ve lokalize sunum ekle.

### F11-06 — Hesap ve oturum güvenliği

- [ ] E-posta doğrulama, şifre sıfırlama/değiştirme ve profil düzenleme ekranlarını ekle.
- [ ] Aktif oturumları cihaz/zaman bilgisiyle listele ve iptal aksiyonlarını ekle.
- [ ] Hesap kapatma öncesi sahip olunan projeler için çözüm akışını göster.

## Sıralama ve bağımlılık

Her frontend başlığı aynı numaralı backend capability tamamlanıp OpenAPI client yeniden
üretildikten sonra başlar. Kanban, Backend B11-02 sıralama ve concurrency sözleşmesi
netleşmeden yalnız görsel prototip olarak kalır; tamamlandı işaretlenmez.

## Kabul ilkesi

Her özellik mobil ve masaüstü davranışı, loading/empty/error/conflict durumları,
erişilebilir klavye akışı ve gerçek backend E2E testiyle birlikte teslim edilir.
