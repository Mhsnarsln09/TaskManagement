# Product Requirements Document - TaskManagement Frontend

## 1. Ürün özeti

TaskManagement, küçük ekiplerin projelerini, üyelerini ve görevlerini tek bir web
arayüzünden yönetmesini sağlayan çalışma uygulamasıdır. Frontend mevcut ASP.NET Core
API'nin istemcisidir; iş kurallarını yeniden tanımlamaz ve backend yetkilendirmesini
tek doğruluk kaynağı kabul eder.

## 2. Hedef kullanıcılar

- **Proje sahibi:** Sistem rolünden bağımsız olarak oluşturduğu projeyi, üyeleri ve
  bütün proje görevlerini yönetir.
- **ProjectManager:** MVP'de tek başına proje içi yetki vermez; sistem seviyesinde
  operasyonel kullanıcı sınıflandırmasıdır.
- **Member:** Proje oluşturabilir; üyesi olduğu projeleri ve görevleri görür, kendisine
  atanan görevin durumunu günceller, yorum ve ek dosya paylaşır.
- **Admin:** Mevcut backend'de proje kaynakları için sistem seviyesi yetkiye sahiptir.
  Kullanıcı rollerini değiştiremez.
- **SuperAdmin:** Sayfalı kullanıcı listesini görür, kullanıcı arar ve sistem rollerini
  yönetir. Son SuperAdmin'in rolü kaldırılamaz.

Public kayıt sırasında rol seçilmez; `email`, `userName`, `password` ve isteğe bağlı
`displayName` gönderilir ve backend kullanıcıyı otomatik `Member` yapar. Rol yönetimi
yalnızca oturum açmış `SuperAdmin` kullanıcısına gösterilir.

## 3. Temel kullanıcı akışları

1. Kullanıcı kayıt olur veya giriş yapar.
2. Üyesi olduğu projeleri görür ve bir proje seçer.
3. Proje özetinde ilerlemeyi, geciken işleri ve görev dağılımını inceler.
4. Görevleri durum ve önceliğe göre filtreler, sıralar ve sayfalar arasında gezer.
5. Yetkisi varsa görev oluşturur, düzenler, atar veya siler.
6. Görev detayında yorumları ve dosya eklerini kullanır.
7. Bildirimleri gerçek zamanlı alır ve okundu olarak işaretler.

## 4. Ekran kapsamı

- Giriş ve kayıt
- Proje listesi, boş durum ve proje oluşturma/düzenleme
- Proje çalışma alanı: özet, görevler ve üyeler
- Görev liste görünümü ve görev detay paneli/sayfası
- Yorumlar ve dosya ekleri
- Bildirim merkezi
- SuperAdmin için kullanıcı arama ve rol yönetimi
- Hesap/oturum menüsü
- 403, 404, genel hata ve bağlantı kesilmesi durumları

## 5. Deneyim ilkeleri

- Operasyonel bir araç gibi sakin, yoğun ama rahat taranabilir olmalıdır.
- Sık işlemler az adımlı; yıkıcı işlemler onaylı olmalıdır.
- Mobilde tüm ana akışlar kullanılabilir kalmalıdır.
- Loading, skeleton, boş, hata, disabled ve başarı durumları tasarlanmalıdır.
- Tarihler kullanıcı saat diliminde gösterilmeli, API'ye sözleşmedeki formatta gitmelidir.
- Renk tek başına durum veya öncelik aktarmamalıdır.

## 6. Teknik sınırlar

- Next.js App Router, TypeScript, Tailwind CSS ve shadcn/ui kullanılacaktır.
- API tipleri OpenAPI sözleşmesinden üretilecektir.
- API hataları Problem Details sözleşmesine göre gösterilecektir.
- SignalR bildirimleri kalıcı veri kaynağı değildir; reconnect sonrası liste yenilenir.
- Access/refresh token yenileme sırasında paralel `401` yanıtları tek refresh isteğini paylaşır.
- Yetki görünürlüğü backend'in proje sahibi/Admin/atanmış üye sözleşmesiyle aynı olmalıdır.

## 7. MVP dışı

- Takvim ve Gantt görünümü
- Sürükle-bırak Kanban
- Tam metin görev araması; tarih aralığı, atanan kişi, gecikenler ve “bana atananlar” filtreleri
- Genel kullanıcı arama API'si gerektiren üye keşfi
- Takım sohbeti, mention, etiket ve alt görevler
- Tema kişiselleştirme ve ileri seviye raporlar

## 8. Başarı ölçütleri

- Kullanıcı temel akışları API dokümanı açmadan tamamlayabilir.
- Yetkisiz eylemler arayüzde sunulmaz; backend hataları doğru biçimde ele alınır.
- Masaüstü ve mobil kritik akışları erişilebilir ve klavye ile kullanılabilirdir.
- MVP kabul kriterleri otomatik testlerle korunur.

## 9. Olgunluk ve kapsam notu

Mevcut ekranların varlığı yayınlanabilir MVP anlamına gelmez. [Frontend MVP](MVP.md)
çıkış kriterleri ve [Görev 10](tasks/10-mvp-hardening.md) yayın engelleri tamamlanmalıdır.
Kanban ve gelişmiş arama, mevcut MVP'nin eksik işi değil, [Görev 11](tasks/11-product-evolution.md)
kapsamındaki yeni ürün özellikleridir.
