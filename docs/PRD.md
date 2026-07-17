# Product Requirements Document — TaskManagement

## 1. Ürün özeti

TaskManagement, ekiplerin proje oluşturmasını, üyeleri organize etmesini, görev atamasını ve proje ilerlemesini takip etmesini sağlayan bir web API'sidir. İlk istemci Swagger veya Postman olacak; API daha sonra React, Angular veya Blazor istemcisine hizmet edebilecektir.

## 2. Problem

Küçük ekiplerde görevler mesajlar ve dağınık notlar arasında kaybolabilir. Görevin sahibi, önceliği, son tarihi ve güncel durumu tek bir yerde görülemediğinde sorumluluk ve ilerleme belirsizleşir.

## 3. Hedef kullanıcılar

- **Admin:** Sistem seviyesindeki kullanıcı ve rol yönetiminden sorumludur.
- **ProjectManager:** Proje oluşturur, üyeleri yönetir, görevleri planlar ve ilerlemeyi takip eder.
- **Member:** Üyesi olduğu projeleri ve görevleri görür, yetkisi ölçüsünde görev durumunu günceller ve yorum ekler.

Roller kaba sistem yetkilerini temsil eder. Bir projedeki sahiplik/üyelik ayrıca kaynak bazlı kontrol edilir. Örneğin `Member` rolündeki bir kullanıcı bir projeye eklenmediyse projeyi göremez.

## 4. Ana kullanıcı senaryoları

### Hesap

- Kullanıcı kayıt olur ve giriş yapar.
- Başarılı giriş sonucunda API erişimi için JWT alır.
- Kullanıcı yalnızca yetkili olduğu kaynaklara erişir.

### Proje

- ProjectManager proje oluşturur.
- Proje yöneticisi kullanıcıyı projeye üye olarak ekler veya çıkarır.
- Üye yalnızca dahil olduğu projeleri listeler ve görüntüler.

### Görev

- Yetkili kullanıcı proje içinde görev oluşturur.
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
4. Görevi silme yetkisi proje yöneticisindedir; Admin sistem politikası gereği ayrıca yetkili olabilir.
5. Tamamlanmış bir görevin alanları doğrudan değiştirilmez; önce tanımlı bir yeniden açma işlemi gerekir.
6. Son teslim tarihi geçmiş ve tamamlanmamış görevler gecikmiş kabul edilir. Bu bilgi ilk aşamada tarihten hesaplanır; ayrıca kalıcı bir durum olarak saklanmaz.
7. Proje ilerlemesi `tamamlanan görev / toplam görev` olarak hesaplanır. Görevi olmayan projede oran sıfırdır.
8. Aynı kullanıcı aynı projeye ikinci kez üye eklenemez.

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

## 8. Açık ürün kararları

MVP geliştirilirken aşağıdakiler netleştirilecektir:

- Kullanıcıların kendi kendine `ProjectManager` olmasına izin verilecek mi?
- Projeden çıkarılan üyenin açık görevlerinin yeni sahibi nasıl belirlenecek?
- Dosyalar MVP'de yerel diskte mi, nesne depolamada mı tutulacak?
- Silinen proje ve görevler geri getirilebilecek mi?

