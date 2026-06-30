# Phase 9 - Quick Reference: Angular Frontend Gateway Integration

## What Changed?

All Angular services now route through **http://localhost:5000 (API Gateway)** instead of direct service calls.

---

## Service API URLs

| Service | Old URL | New URL |
|---------|---------|---------|
| Auth | `https://localhost:7261` | `http://localhost:5000/auth` |
| User | `https://localhost:7261/users/Users` | `http://localhost:5000/auth/users` |
| Donor | `https://localhost:7261/donors/Donors` | `http://localhost:5000/catalog/donors` |
| Gift | `https://localhost:7261/gifts/Gifts` | `http://localhost:5000/catalog/gifts` |
| Order | `https://localhost:7261/Orders/Orders` | `http://localhost:5000/orders` |
| Lottery | `https://localhost:7261/lottery/Lottery` | `http://localhost:5000/lottery` |

---

## Gateway Routes

```
/auth/*       → AuthService (5001)
/catalog/*    → CatalogService (5002)
/orders/*     → OrderService (5003)
/lottery/*    → LotteryService (5004)
```

---

## How It Works

```
Angular App (Port 4200)
    ↓
AuthService/DonorService/GiftService (etc.)
    ↓
HTTP Interceptor (adds JWT token automatically)
    ↓
API Gateway (Port 5000)
    ↓
Route to correct microservice
    ↓
Microservice processes request
    ↓
Return to Angular
```

---

## Files Changed

✅ `authService.ts` - Base URL updated  
✅ `userService.ts` - Base URL + endpoints updated  
✅ `donorService.ts` - Base URL + endpoints updated  
✅ `giftService.ts` - Base URL + endpoints updated  
✅ `orderService.ts` - Base URL + endpoints updated  
✅ `lotteryService.ts` - Base URL + endpoints updated  
✅ `auth.interceptor.ts` - Public routes updated  

---

## Important: Endpoint Names Changed

All endpoint names now use kebab-case:

| Old | New |
|-----|-----|
| `GetByName` | `by-name` |
| `GetByEmail` | `by-email` |
| `GetByGift` | `by-gift` |
| `GetAllWinners` | `winners` |
| `GetAllRevenue` | `revenue` |
| `LotteryDone` | `done` |
| `sendingEmail` | `send-email` |

---

## Token Handling

✅ JWT token automatically added to all requests except:
- `POST /auth/login`
- `POST /auth/register`
- `GET /catalog/gifts/by-name/*`
- `GET /catalog/gifts/by-donor-name/*`

Token is stored in `localStorage['authToket']` after login.

---

## Testing Steps

### 1. Start Services
```powershell
cd server
powershell -File start-services.ps1
```

### 2. Start Angular
```bash
cd client/mecira_sinit_Angular
ng serve
```

### 3. Open Browser
```
http://localhost:4200
```

### 4. Check Network Tab
All API calls should go to:
- `http://localhost:5000/auth/...`
- `http://localhost:5000/catalog/...`
- `http://localhost:5000/orders/...`
- `http://localhost:5000/lottery/...`

**NOT** to individual service ports!

---

## Verification Checklist

- [ ] All services running (ports 5000-5004 listening)
- [ ] Angular app loads on http://localhost:4200
- [ ] Login works (JWT token received)
- [ ] Network tab shows calls to port 5000 only
- [ ] Authorization header includes Bearer token
- [ ] Donors/Gifts/Orders/Lottery endpoints working
- [ ] No 404 errors on endpoints
- [ ] No CORS errors

---

## Common Issues

**Q: Getting 404 errors?**  
A: Check that gateway route starts with `/auth/`, `/catalog/`, `/orders/`, or `/lottery/`

**Q: Token not being sent?**  
A: Check auth interceptor public routes or verify token in `localStorage['authToket']`

**Q: CORS errors?**  
A: Check API Gateway CORS policy in `Program.cs`

**Q: Services appear offline?**  
A: Verify all 5 services started: `Get-NetTCPConnection -State Listen | Where {$_.LocalPort -in 5000..5004}`
