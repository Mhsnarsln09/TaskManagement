# Frontend MVP

## MVP kapsamı

### Kimlik doğrulama

- Kayıt, giriş, çıkış ve oturum yenileme
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

## Kabul kriterleri

- Bütün istekler yapılandırılabilir API base URL üzerinden yapılır.
- Authorization header korunan API çağrılarına eklenir.
- Aynı anda oluşan `401` yanıtları yalnızca bir refresh isteği üretir.
- `400`, `401`, `403`, `404`, `409`, `429` ve ağ hataları anlaşılır şekilde gösterilir.
- Liste ekranlarında loading, empty, error ve pagination durumları vardır.
- Formlar gönderim sırasında tekrar gönderilemez; başarı sonrası ilgili veri yenilenir.
- Yetkiye bağlı eylemler görünürlük ve disabled durumlarıyla tutarlıdır.
- Mobil ve masaüstünde taşma/örtüşme yoktur; temel klavye ve ekran okuyucu kullanımı çalışır.
- Auth, proje listesi, görev yönetimi ve hata işleme kritik testlerle kapsanır.

## Yayına hazır tanımı

- Lint, typecheck, unit/component ve kritik E2E testleri geçer.
- Production build başarılıdır.
- Gizli bilgi kaynak koda veya `NEXT_PUBLIC_*` değişkenlerine yazılmaz.
- OpenAPI client üretme komutu ve yerel geliştirme adımları belgelenmiştir.
