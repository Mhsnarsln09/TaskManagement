# Frontend Dağıtım

Bu belge TaskManagement frontend'inin production'a alınmasını anlatır. Backend
dağıtımı `backend/` altındaki belgelerde ve `backend/compose.yml` dosyasındadır.

## 1. Environment değişkenleri

| Değişken | Zorunlu | Varsayılan | Açıklama |
| --- | --- | --- | --- |
| `NEXT_PUBLIC_API_BASE_URL` | Evet (prod) | `http://localhost:8080` | API kök adresi, sondaki `/` olmadan. SignalR hub adresi bundan türetilir (`<base>/hubs/notifications`). |

Kurallar:

- `NEXT_PUBLIC_*` değişkenleri **build sırasında** paket içine gömülür ve
  tarayıcıya gider. Buraya gizli bilgi (API anahtarı, signing key, DB parolası)
  yazılmaz.
- Değişken değiştiğinde uygulama **yeniden build edilmelidir**; sadece
  yeniden başlatmak yetmez.
- Değer `zod` ile doğrulanır (`src/lib/env.ts`); geçersiz URL build/başlangıçta
  hata verir.

## 2. Build ve çalıştırma

```bash
npm ci
NEXT_PUBLIC_API_BASE_URL=https://api.ornek.com npm run build
npm run start          # varsayılan port 3000
```

Portu değiştirmek için `npm run start -- --port 8081` veya `PORT=8081 npm run start`.

Çıktı `.next/` altındadır; `npm run start` Node.js sunucusu gerektirir
(uygulama istemci tarafı oturum kullandığı için statik export uygun değildir).

## 3. Docker ile dağıtım

`next.config.ts` içinde `output: "standalone"` etkinleştirilirse imaj küçülür.
Örnek Dockerfile:

```dockerfile
FROM node:22-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci

FROM node:22-alpine AS build
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
ARG NEXT_PUBLIC_API_BASE_URL
ENV NEXT_PUBLIC_API_BASE_URL=$NEXT_PUBLIC_API_BASE_URL
RUN npm run build

FROM node:22-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production
COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static
COPY --from=build /app/public ./public
EXPOSE 3000
CMD ["node", "server.js"]
```

`NEXT_PUBLIC_API_BASE_URL` bir **build argümanıdır**; her ortam için ayrı imaj
üretilir ya da uygulama ters vekil arkasında API ile aynı origin'e alınır
(bkz. §4).

## 4. Backend CORS ve ağ

Frontend, API'yi tarayıcıdan doğrudan çağırır. İki seçenek vardır:

1. **Ayrı origin (varsayılan):** API'nin `Cors:AllowedOrigins` listesine
   frontend origin'i **açıkça** eklenir. Wildcard origin ile credentials
   birlikte kullanılmaz (bkz. docs/FRONTEND-INTEGRATION.md).

   ```json
   { "Cors": { "AllowedOrigins": ["https://app.ornek.com"] } }
   ```

2. **Aynı origin (önerilen):** Ters vekil (nginx/Traefik) `/api` ve `/hubs`
   yollarını backend'e yönlendirir, kalan her şeyi Next.js'e. CORS ve preflight
   tamamen ortadan kalkar; `NEXT_PUBLIC_API_BASE_URL` frontend'in kendi kökü olur.

   SignalR WebSocket için vekilde `Upgrade`/`Connection` başlıkları
   iletilmelidir; aksi halde bildirimler yalnız long-polling ile çalışır ve
   "yeniden bağlanılıyor" bandı sık görünür.

## 5. Yayın öncesi kontrol listesi

```bash
npm run lint
npm run typecheck
npm test
npm run test:e2e
npm run build
```

Ek olarak:

- [ ] `NEXT_PUBLIC_API_BASE_URL` hedef ortamın API adresine ayarlı ve HTTPS.
- [ ] Backend CORS listesinde frontend origin'i var (ya da aynı origin kurulumu).
- [ ] Backend `Jwt:SigningKey` ve veritabanı bilgileri secret yöneticisinden
      geliyor; frontend build'inde gizli bilgi yok.
- [ ] Sağlık kontrolü: `GET /health/ready` (backend) 200 dönüyor.
- [ ] Uygulama açılışında giriş, proje listesi ve görev listesi manuel doğrulandı.

## 6. Bilinen sınırlar

- Access/refresh token `localStorage`'ta tutulur; XSS riski kabul edilmiştir
  (bkz. README "Güvenlik notu" ve DESIGN-DECISIONS.md §9). HttpOnly cookie/BFF
  yaklaşımı post-MVP hedefidir.
- OpenAPI tipleri commit'lenmiş şemadan üretilir; backend sözleşmesi
  değiştiğinde `npm run openapi` çalıştırılıp değişiklik commit'lenmelidir.
- E2E testleri API'yi mock'lar; sözleşme uyumunu backend integration testleri ve
  üretilen tipler korur.
