# Product Requirements Document - TaskManagement Frontend

## 1. Ürün özeti

TaskManagement, küçük ekiplerin projelerini, üyelerini ve görevlerini tek bir web
arayüzünden yönetmesini sağlayan çalışma uygulamasıdır. Frontend mevcut ASP.NET Core
API'nin istemcisidir; iş kurallarını yeniden tanımlamaz ve backend yetkilendirmesini
tek doğruluk kaynağı kabul eder.

## 2. Hedef kullanıcılar

- **ProjectManager:** Proje oluşturur, düzenler, üyeleri ve görevleri yönetir.
- **Member:** Üyesi olduğu projeleri ve görevleri görür; izin verilen görevleri
  günceller, yorum ve ek dosya paylaşır.
- **Admin:** Mevcut backend'de proje kaynakları için sistem seviyesi yetkiye sahiptir.
  MVP'de kullanıcı/rol yönetim paneli yoktur.

Public kayıt sırasında rol seçilmez; `email`, `userName`, `password` ve isteğe bağlı
`displayName` gönderilir ve backend kullanıcıyı otomatik `Member` yapar. SuperAdmin
ve rol yönetimi henüz uygulanmamıştır; backend `09-super-admin-role-management`
görevinin kapsamındadır ve tamamlanana kadar frontend'de gösterilmez.

## 3. Temel kullanıcı akışları

1. Kullanıcı kayıt olur veya giriş yapar.
2. Üyesi olduğu projeleri görür ve bir proje seçer.
3. Proje özetinde ilerlemeyi, geciken işleri ve görev dağılımını inceler.
4. Görevleri arar, filtreler, sıralar ve sayfalar arasında gezer.
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

## 7. MVP dışı

- SuperAdmin ve kullanıcı/rol yönetim paneli (backend görevi tamamlanana kadar)
- Takvim ve Gantt görünümü
- Sürükle-bırak Kanban
- Genel kullanıcı arama API'si gerektiren üye keşfi
- Takım sohbeti, mention, etiket ve alt görevler
- Tema kişiselleştirme ve ileri seviye raporlar

## 8. Başarı ölçütleri

- Kullanıcı temel akışları API dokümanı açmadan tamamlayabilir.
- Yetkisiz eylemler arayüzde sunulmaz; backend hataları doğru biçimde ele alınır.
- Masaüstü ve mobil kritik akışları erişilebilir ve klavye ile kullanılabilirdir.
- MVP kabul kriterleri otomatik testlerle korunur.
