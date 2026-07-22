# Product Requirements Document — TaskManagement

## 1. Ürün özeti

TaskManagement, ekiplerin proje oluşturmasını, üyeleri organize etmesini, görev atamasını ve proje ilerlemesini takip etmesini sağlayan bir web API'sidir. İlk istemci Swagger veya Postman olacak; API daha sonra React, Angular veya Blazor istemcisine hizmet edebilecektir.

## 2. Problem

Küçük ekiplerde görevler mesajlar ve dağınık notlar arasında kaybolabilir. Görevin sahibi, önceliği, son tarihi ve güncel durumu tek bir yerde görülemediğinde sorumluluk ve ilerleme belirsizleşir.

## 3. Hedef kullanıcılar ve yetki modeli

- **Admin:** Sistem seviyesindeki kullanıcı ve rol yönetiminden sorumludur.
- **ProjectManager:** Birden fazla projeyi operasyonel olarak yönetmesi beklenen kullanıcıyı ifade eden sistem rolüdür. MVP'de proje içindeki yetkinin kaynağı tek başına bu rol değildir.
- **Member:** Proje oluşturabilir; üyesi olduğu projeleri ve görevleri görür, kendisine atanan görevin durumunu günceller, yorum ve ek paylaşır.
- **Proje sahibi:** Projeyi oluşturan kullanıcıdır. Sistem rolünden bağımsız olarak proje bilgilerini, üyeleri ve proje içindeki bütün görevleri yönetir.

Sistem rolleri kaba platform yetkilerini, proje sahipliği ve üyeliği ise kaynak bazlı yetkileri temsil eder. MVP'de her kayıtlı kullanıcı proje oluşturabilir ve oluşturduğu projenin sahibi olur. `ProjectManager` rolü gelecekte yönetilen proje ataması veya proje içi rol modeli gelene kadar tek başına kaynak yetkisi vermemelidir.

## 4. Ana kullanıcı senaryoları

### Hesap

- Kullanıcı kayıt olur ve giriş yapar.
- Başarılı giriş sonucunda API erişimi için JWT alır.
- Kullanıcı yalnızca yetkili olduğu kaynaklara erişir.

### Proje

- Kayıtlı kullanıcı proje oluşturur ve proje sahibi olur.
- Proje sahibi kullanıcıyı projeye üye olarak ekler veya çıkarır.
- Üye yalnızca dahil olduğu projeleri listeler ve görüntüler.
- Admin yönetim görünümünde aktif projelerin tamamını listeleyebilir.

### Görev

- Proje sahibi veya Admin proje içinde görev oluşturur, düzenler, yeniden atar ve siler.
- Atanmış üye kendi görevinin durumunu izin verilen geçişlerle günceller.
- Göreve yalnızca aynı projenin bir üyesi atanabilir.
- Görevin başlık, açıklama, durum, öncelik ve son teslim tarihi bilgileri bulunur.
- Görev `Todo`, `InProgress` ve `Completed` durumları arasında kurallara uygun taşınır.
- Yetkili kullanıcı görevleri durum, öncelik ve tarih gibi alanlarla filtreler; sonuçları sayfalı alır.

### İş birliği

- Proje üyesi göreve yorum ekler.
- Proje üyesi göreve izin verilen tür ve boyutta dosya ekler.
- Kullanıcı proje ilerleme özetini görür.

## 5. Temel iş kuralları

1. Projeyi oluşturan kullanıcı projenin yöneticisi ve ilk üyesidir.
2. Kullanıcı yalnızca üyesi olduğu projeleri görebilir.
3. Görev, yalnızca ait olduğu projenin bir üyesine atanabilir.
4. Görev oluşturma, tüm alanları düzenleme, yeniden atama ve silme yetkisi proje sahibi veya Admin'dedir. Atanmış üye yalnızca görev durumunu değiştirebilir.
5. Tamamlanmış bir görevin alanları doğrudan değiştirilmez; önce tanımlı bir yeniden açma işlemi gerekir.
6. Son teslim tarihi geçmiş ve tamamlanmamış görevler gecikmiş kabul edilir. Bu bilgi ilk aşamada tarihten hesaplanır; ayrıca kalıcı bir durum olarak saklanmaz.
7. Proje ilerlemesi `tamamlanan görev / toplam görev` olarak hesaplanır. Görevi olmayan projede oran sıfırdır.
8. Aynı kullanıcı aynı projeye ikinci kez üye eklenemez.
9. Soft-delete edilmiş proje, görev ve bunların alt kaynakları hiçbir normal sorgu veya yetki kontrolü üzerinden erişilebilir olmamalıdır.
10. Proje sahibinin üyeliği sahiplik devredilmeden kaldırılamaz. Sahiplik devri MVP sonrası kapsamdır.

## 6. Fonksiyonel olmayan gereksinimler

- API tutarlı HTTP durum kodları ve Problem Details hata gövdesi döndürmelidir.
- Şifre, JWT anahtarı ve bağlantı bilgileri repoya yazılmamalıdır.
- Liste uçları sayfalama kullanmalıdır.
- Tarihler UTC saklanmalı ve ISO 8601 biçiminde taşınmalıdır.
- Önemli istekler yapılandırılmış olarak loglanmalıdır; token ve şifre loglanmamalıdır.
- Veritabanı şeması EF Core migration'ları ile tekrar üretilebilir olmalıdır.
- Kritik iş kuralları otomatik testlerle korunmalıdır.

## 7. Başarı ölçütleri

- Yeni kullanıcı kayıt olup giriş yapabiliyor.
- Proje yöneticisi proje ve üyelerini yönetebiliyor.
- Proje üyesine görev atanabiliyor ve görev süreci takip edilebiliyor.
- Yetkisiz kullanıcı başka bir projenin verisini göremiyor veya değiştiremiyor.
- İlerleme oranı doğru hesaplanıyor.
- Kritik senaryoların unit ve integration testleri geçiyor.
- Silinmiş proje ve görevlerin alt kaynaklarına eski üyelik veya bilinen kimliklerle erişilemiyor.

## 8. MVP sonrası ürün kararları

MVP sonrasında aşağıdakiler netleştirilecektir:

- Sistem seviyesindeki `ProjectManager` rolü kaldırılacak mı, yoksa yönetilen proje atamasıyla mı anlamlandırılacak?
- Proje içi `Owner`, `Manager`, `Contributor`, `Viewer` gibi roller eklenecek mi?
- Projeden çıkarılan üyenin açık görevlerinin yeni sahibi nasıl belirlenecek?
- Dosyalar MVP'de yerel diskte mi, nesne depolamada mı tutulacak?
- Silinen proje ve görevler için geri yükleme ve saklama süresi nasıl çalışacak?
- Davet, üyelik onayı ve proje sahipliği devri hangi akışla uygulanacak?

## 9. Kapsam ve olgunluk notu

Kodda bir endpoint veya ekranın bulunması tek başına ürün gereksiniminin tamamlandığı anlamına gelmez. MVP yayın kararı, [MVP](MVP.md) belgesindeki çıkış kriterleri ile [Görev 10](tasks/10-mvp-hardening.md) içindeki yayın engellerinin tamamlanmasına bağlıdır. Kanban, gelişmiş arama, proje içi roller, davetler, sahiplik devri ve aktivite geçmişi MVP sonrası ürün geliştirmeleridir.
