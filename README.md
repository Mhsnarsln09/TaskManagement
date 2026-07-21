# TaskManagement

Repository iki bağımsız geliştirme alanına ayrılmıştır:

```text
backend/   ASP.NET Core API, testler, Docker ve backend dokümantasyonu
frontend/  Next.js web istemcisi ve frontend dokümantasyonu
```

Tüm uygulamayı Docker ile çalıştırmak için:

```bash
cd backend
cp .env.example .env
docker compose up -d --build frontend
```

`frontend` servisi başlatıldığında API, Postgres ve Redis bağımlılıkları da
zincirleme başlar. Frontend varsayılan olarak `http://localhost:3000`, API ise
`http://localhost:8080` üzerinden yayınlanır.

Backend geliştirme notları `backend/docs`, frontend kapsamı ve entegrasyon
notları `frontend/docs` altındadır.
