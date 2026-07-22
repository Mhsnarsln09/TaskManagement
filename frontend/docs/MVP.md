# Frontend MVP

## Durum notu — 22 Temmuz 2026

Ana ekranlar ve akışlar uygulanmıştır; ancak yayınlanabilir MVP tamamlanmış değildir.
Yorum sayfalaması, bildirim sayacı/toplu okuma, gerçek logout, silme metinleri,
yetki görünürlüğü ve ağsız production build [Görev 10](tasks/10-mvp-hardening.md)
tamamlanana kadar çıkış engelidir.

## MVP kapsamı

### Kimlik doğrulama

- Kayıt, giriş, çıkış ve oturum yenileme
- Çıkışta refresh token'ı backend üzerinden iptal etme
- Kayıtta yalnızca e-posta, kullanıcı adı, isteğe bağlı görünen ad ve şifre; rol seçimi yok
- Korunan rotalar ve oturum süresi dolduğunda kontrollü yönlendirme
- Form doğrulama ve Problem Details validation mesajları

### Projeler

- Kullanıcının projelerini listeleme
- Proje oluşturma, görüntüleme, düzenleme ve silme
- Proje üyelerini listeleme, üye ekleme ve çıkarma
- Proje istatistikleri ve tamamlanma yüzdesi

### Görevler

- Sayfalı görev listesi
- Durum ve öncelik filtresi; desteklenen alanlarda sıralama
- Görev oluşturma, görüntüleme, düzenleme ve silme
- Geciken görev, atanan kullanıcı, öncelik ve durum gösterimi

### İş birliği

- Sayfalı yorum listesi ve yorum ekleme
- Dosya yükleme, listeleme ve indirme
- Sayfalı bildirim merkezi, okunmamış durumu ve okundu işaretleme
- SignalR ile yeni bildirimi toast ve liste güncellemesi olarak alma
- Bildirim toplam okunmamış sayısı, sunucu taraflı “tümünü okundu” ve ilgili kaynağa navigasyon

### SuperAdmin

- Yalnızca SuperAdmin rolüne görünen sayfalı kullanıcı listesi ve arama
- Kullanıcının `SuperAdmin`, `Admin`, `ProjectManager`, `Member` rollerini değiştirme
- Son SuperAdmin kuralı ve rol değişiminden sonra hedef kullanıcının oturumunun kapanması

## Kabul kriterleri

- Bütün istekler yapılandırılabilir API base URL üzerinden yapılır.
- Authorization header korunan API çağrılarına eklenir.
- Aynı anda oluşan `401` yanıtları yalnızca bir refresh isteği üretir.
- `400`, `401`, `403`, `404`, `409`, `429` ve ağ hataları anlaşılır şekilde gösterilir.
- Liste ekranlarında loading, empty, error ve pagination durumları vardır.
- Formlar gönderim sırasında tekrar gönderilemez; başarı sonrası ilgili veri yenilenir.
- Yetkiye bağlı eylemler görünürlük ve disabled durumlarıyla tutarlıdır.
- İlk yorum yüklemesi en yeni yorumları gösterir; “daha eski” işlemi kronolojik olarak geriye gider.
- Soft-delete ile kalıcı silme kullanıcı metinlerinde birbirine karıştırılmaz.
- Çıkıştan sonra eski refresh token ile oturum yenilenemez.
- Mobil ve masaüstünde taşma/örtüşme yoktur; temel klavye ve ekran okuyucu kullanımı çalışır.
- Auth, proje listesi, görev yönetimi ve hata işleme kritik testlerle kapsanır.

## Yayına hazır tanımı

- Lint, typecheck, unit/component ve kritik E2E testleri geçer.
- Production build başarılıdır.
- Production build harici font ağına bağlı olmadan başarılıdır.
- Gizli bilgi kaynak koda veya `NEXT_PUBLIC_*` değişkenlerine yazılmaz.
- OpenAPI client üretme komutu ve yerel geliştirme adımları belgelenmiştir.
