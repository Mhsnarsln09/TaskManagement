# Tasarım → Implementation Kararları (00)

Bu doküman, `TaskManagement app design/` altındaki tasarım ile backend sözleşmesi
(`TaskManagement.Application/Contracts`, controller ve validator'lar) arasındaki
farkları implementation kararına çevirir. Çelişki durumunda **backend sözleşmesi
tek doğruluk kaynağıdır**.

## 1. Kullanıcı adı gösterimi (UserDisplay)

> **Güncelleme (2026-07-21):** GUID yapıştırarak üye eklemek kullanılabilir bir
> arayüz değildi. Backend'e iki ekleme yapıldı ve bu bölümün ilk hâli geçersiz
> kaldı: (a) `GET /api/users?search=` — oturum açmış her kullanıcı kişileri
> **isimle** arayabilir, (b) `GET /api/projects/{id}/members` yanıtındaki her satır
> artık `user` özetini taşır. Her ikisi de `UserSummaryResponse` döner:
> `id, userName, displayName` — **e-posta ve sistem rolleri yoktur**. Roller
> yalnız SuperAdmin'e `/api/admin/users` üzerinden görünür; bir kişi seçicide rol
> göstermek, her kullanıcıya kimin yetkili olduğunu sızdırırdı ve zaten proje
> yetkisini sistem rolü değil sahiplik belirler.

Sözleşme gerçeği: görev `assigneeUserId` ve proje `ownerUserId` yalnız GUID
döndürür. Ad döndüren yerler: auth yanıtındaki `user`, yorum `author`, ek
`uploadedBy`, üye `user`, kullanıcı arama ve SuperAdmin listesi.

**Karar:** İstemcide oturum boyunca yaşayan bir *kullanıcı dizini* (user directory
cache) tutulur; auth yanıtı, yorum/ek yazarları ve (SuperAdmin ise) admin listesi bu
dizini besler. `UserDisplay` bileşeni:

- Dizinde eşleşme varsa `displayName ?? userName` gösterir; GUID tooltip'te taşınır.
- Eşleşme yoksa kısaltılmış GUID (`2c9e4b7a…8a12`) + kopyalama düğmesi gösterir.
- Oturum kullanıcısıyla eşleşince "siz" rozeti eklenir.

Tasarımdaki "Ayşe Demir / Mehmet Kaya" örnekleri dizin dolu senaryodur; boş dizin
durumu kısaltılmış ID'dir (tasarım §07 notuyla uyumlu).

## 2. Bildirim içeriği ve gezinme

Sözleşme: `NotificationResponse = { id, taskItemId, type, message, isRead,
createdAtUtc, readAtUtc }`. Aktör adı ve proje adı **yok**; ayrıca görev rotaları
`projectId` gerektirdiği için `taskItemId` tek başına bir görev linkine
çevrilemez (task → project çözen endpoint yok).

**Karar:** Bildirim satırı yalnız `message` + göreli zaman + okunmamış noktası
gösterir. Tasarımdaki "Mehmet Kaya … · Web Sitesi Yenileme" dekorasyonu ve
toast'taki "Görüntüle" gezinme eylemi **uygulanmaz**; toast yalnız mesajı gösterir.
Bildirime tıklama yalnız okundu işaretler.

## 3. "Tümünü okundu işaretle"

Sözleşmede toplu endpoint yok; yalnız `PUT /api/notifications/{id}/read` var.

**Karar:** Buton, o ana kadar yüklenmiş okunmamış bildirimler için paralel
`PUT …/read` çağrıları yapar (istek başına hata toleranslı; başarısızlar okunmamış
kalır ve toast ile bildirilir). Buton işlem sırasında disabled + spinner.

## 4. Dosya yükleme limitleri

Tasarım metni "10 MB; png, jpg, pdf, har, log, zip" der. Backend
(`appsettings.json → FileUpload`): **5 MB** (5.242.880 bayt) ve
`.png .jpg .jpeg .gif .pdf .txt .csv`.

**Karar:** Backend değerleri tek kaynaktır; frontend'te sabit
(`lib/config/upload.ts`) olarak tutulur ve hata metinlerinde bu değerler yazılır.
İstemci ön-doğrulaması (boyut+uzantı) yapılır, backend 400'ü yine de işlenir.

## 5. 429 ve Retry-After

Backend fixed-window limiter (120 istek/dk, kullanıcı/IP başına) `Retry-After`
başlığı **göndermez** (varsayılan rejection).

**Karar:** 429 alındığında `Retry-After` varsa kullanılır; yoksa 15 sn'lik sabit
geri sayım gösterilir. Geri sayım bitene kadar "Tekrar dene" disabled (tasarım §04
deseninin başlıksız fallback'i).

## 6. Görev formu alanları

- `CreateTaskRequest` **status içermez** (backend Todo atar). Tasarımdaki "Yeni
  görev" diyaloğundaki Durum seçicisi **yalnız düzenlemede** gösterilir; oluşturma
  formunda gösterilmez.
- `dueDate` `DateOnly` (`yyyy-MM-dd`) formatında gönderilir ve hem create hem
  update'te *bugünden eski olamaz*. Sonuç: geçmiş tarihli (overdue) bir görev
  düzenlenirken tarih değiştirilmeden kaydedilemez. Form, geçmiş tarih seçiliyken
  satır içi uyarı gösterir; backend 400'ü alanla eşlenir.
- `assigneeUserId` proje üyeleri arasından seçilir (üye listesi + kullanıcı dizini
  ile adlandırılır); üye olmayan GUID backend'de 400 döner.
- Uzunluk limitleri: title ≤ 200, description ≤ 4000 (proje: name ≤ 160,
  description ≤ 2000; yorum ≤ 2000).

## 7. Yetki matrisi (UI görünürlüğü) — B10-02 / F10-03 ile güncellendi

Backend kuralları (`ProjectAuthorizationService`, `TaskService`; `lib/permissions.ts`
aynası):

| Eylem | Kural |
| --- | --- |
| Proje görüntüleme, görev/yorum/ek okuma | Proje üyesi veya `Admin` |
| Yorum ekleme, ek yükleme | Her proje üyesi |
| **Görev oluşturma, tüm alanları düzenleme, yeniden atama, silme** | **Proje sahibi veya `Admin`** |
| **Görev durumunu değiştirme** | Sahip/Admin **veya** görevin atanmış üyesi (izin verilen geçişlerle) |
| Diğer üyenin göreve yazması | Yok (salt okunur) |
| Proje düzenleme/silme, üye ekleme/çıkarma | Proje sahibi veya `Admin` |
| Kullanıcı yönetimi | Yalnız `SuperAdmin` |

Sistem `ProjectManager` rolü **tek başına** proje içi yetki vermez. Eski görev-silme
`ProjectManager + owner` istisnası B10-02 ile kaldırıldı.

**UI türetimi (`taskPermissions`):**

- **Sahip/Admin:** "Yeni görev", "Düzenle" (tüm alanlar), yeniden atama, "Sil".
- **Atanmış üye:** yalnız durum seçici (izin verilen geçişler); PUT tüm alanları aynen
  + yeni durum + `version` ile gönderir (backend `EnsureOnlyStatusChanged`).
- **Diğer üye:** yazma kontrolü yok, "Salt okunur" rozeti; yorum/ek açık.

**Karar:** UI görünürlüğü bu matristen türetilir (`ownerUserId === session.id`,
roller auth yanıtından). Emin olunamayan uç durumlarda eylem gösterilir ve backend
403'ü ProblemDetailsAlert ile işlenir. Üye olmayanın proje erişimi backend gereği
**404** döner; 403 sayfası yalnız "üye ama yetkisiz" durumundadır.

## 8. Problem Details gösterim matrisi (tasarım §13 onaylandı)

Tek bileşen: `ProblemDetailsAlert` (severity red/amber + başlık + detay + isteğe
bağlı eylem). Eşleme:

| Durum | Sunum |
| --- | --- |
| 400 | Alan hataları (`errors`) ilgili FormField altına `role=alert` ile; alansız doğrulama form üstü kırmızı alert |
| 401 | Girişte form üstü hata; oturum içinde tekil refresh → başarısızsa oturum-süresi-doldu yönlendirmesi (`/login?expired=1&returnUrl=…`) |
| 403 | Tam sayfa ErrorState; eylem bazlıysa kontrol gizlenir |
| 404 | Tam sayfa "Sayfa bulunamadı"; diyalog içindeyse alert + liste yenileme |
| 409 | Bağlama özgü amber alert: görev çakışması (§05), yinelenen üye (§07), son SuperAdmin (§11) |
| 429 | Amber alert + geri sayım (bkz. karar 5) |
| Ağ | Çevrimdışı bandı + yeniden dene; bağlanınca "yenilendi" toast |

## 9. Kimlik doğrulama ve oturum

- Çıkış endpoint'i yok → çıkış istemcide token temizliği + `/login`.
- Refresh tekil kuyruk: eşzamanlı 401'ler tek `POST /api/auth/refresh` paylaşır;
  yeni access+refresh token atomik yazılır; refresh başarısızsa oturum kapatılır.
- Token saklama: MVP'de refresh token `localStorage`'ta tutulur (API JSON döndürür;
  FRONTEND-INTEGRATION.md'deki XSS uyarısı README'de belgelenir, BFF post-MVP).
- Kayıt formu alanları birebir: displayName? + userName (≥3) + email + password
  (≥8, büyük/küçük harf + rakam); rol seçici yok.

## 10. Liste sözleşmeleri

- `GET /api/projects` **sayfasızdır** (düz dizi) → proje listesinde pagination yok.
- Görev/yorum/bildirim/ek listeleri `PagedResponse { items, page, pageSize,
  totalCount, totalPages }`.
- Görev sıralama alanları: `title, status, priority, dueDate, createdAtUtc`;
  yön `asc|desc`. Metin araması yok.
- Enumlar string serileşir: `Todo|InProgress|Completed|Cancelled`,
  `Low|Medium|High|Critical`.
- Yorum listesinde "daha fazla yükle" deseni kullanılır (pageSize 20).
- Ek listesi endpoint'i **sayfasızdır** (düz dizi döner) → tasarım §13'teki
  "Daha fazla ek yükle (2/9)" deseni uygulanmaz; tüm ekler tek listede gösterilir.

## 11. SignalR

Hub `/hubs/notifications`, event `notificationReceived`, token
`accessTokenFactory` ile. `withAutomaticReconnect`; ilk bağlantıda ve reconnect
sonrasında `GET /api/notifications` yeniden çekilir. Kopukta üst bara
"Bağlantı koptu / yeniden bağlanılıyor" bandı.

## 12. OpenAPI istemci üretimi

`openapi-typescript` ile `/openapi/v1.json`'dan tip üretilir; şema snapshot'ı
`frontend/src/lib/api/openapi.json` olarak commit'lenir, üretilen tipler
`frontend/src/lib/api/schema.d.ts` (elle düzenlenmez). Komut: `npm run openapi`
(canlı API'den çeker + tip üretir). Backend tarafında
`dotnet build -p:OpenApiGenerateDocumentsOnBuild=true` ile build-time üretim de
mümkündür.

## 13. Tasarım tokenları ve shadcn eşlemesi (tasarım §00/§12 onaylandı)

- Nötr: slate; vurgu: teal-700 (#0f766e, hover #115e59); semantik: red/amber/
  green/blue — renk asla tek başına değil, metin/ikonla birlikte.
- Radius: kart 8px, kontrol 6px. Font: Inter. Kırılımlar: sm 640 / md 768 / lg 1024.
- Primitifler (shadcn): Button, Input, Textarea, Label, Select, Checkbox, Badge,
  Tooltip, Dialog, AlertDialog, Sheet, DropdownMenu, Table, Tabs, Progress,
  Skeleton, Sonner(Toast), Pagination, Form, Popover, Calendar(DatePicker).
- Kompozitler: AppShell, SidebarNav, MobileNav, PageHeader, DataTable, FilterBar,
  EmptyState, ErrorState, LoadingState, ConfirmDialog, ProblemDetailsAlert,
  StatusBadge, PriorityBadge, UserDisplay, DateDisplay, NotificationItem,
  FileAttachmentItem, StatCard, RoleBadge.

## 14. Ekran bazlı netleştirmeler

- **Proje genel bakış (§03):** yalnız istatistik endpoint'i + proje meta;
  "geciken görevler" listesi `GET tasks?status=&sortBy=dueDate&sortDirection=asc`
  üzerinden ilk sayfadan `isOverdue` filtrelenerek gösterilir (ayrı endpoint yok);
  "Görevlerde filtrele →" görev listesine yönlendirir.
- **Üyeler (§07):** ekleme, `/api/users` arama ucuyla isimle yapılır; seçim
  sonrası API'ye yalnız `userId` gider. Zaten üye olanlar seçim listesinde
  gösterilmez. Sahip kendini kaldıramaz (backend kuralı) → satırında kaldır
  düğmesi yok. GUID artık ikincil bilgidir (ad üzerinde tooltip).
- **Proje silme (§09):** ad-doğrulamalı AlertDialog; diyalogdaki "24 görev, 41
  yorum…" sayıları istatistik endpoint'inden yalnız görev sayısı olarak alınır
  (yorum/ek sayısı endpoint'i yok → metin "Tüm görevler, yorumlar ve ekler kalıcı
  olarak silinecek" şeklinde sayısızdır; görev sayısı istatistikten eklenir).
- **SuperAdmin (§11):** arama debounce 300 ms; rol kaydetmede en az bir rol
  istemci doğrulaması; başarı toast'ı "Roller güncellendi; kullanıcının oturumu
  sonlandırıldı". SuperAdmin olmayan için rota istemcide 404 sayfasına düşer
  (sunucu da 403 döner; UI 404 gösterimini tercih eder — kaynak sızdırmama
  ilkesiyle uyumlu).
- **Kayıt sonrası:** auth yanıtı token içerdiği için kayıt otomatik giriştir →
  proje listesine yönlendirilir.
