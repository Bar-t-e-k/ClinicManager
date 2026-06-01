# US#9 – Poradnik testowania indeksów (krok po kroku)

Ten dokument tłumaczy **od zera**, jak przetestować optymalizację bazy (indeksy PESEL + lekarz) i zrobić raport z screenshotami.

---

## Co w ogóle testujemy?

Dodaliśmy 2 indeksy, żeby SQL Server **szybciej** znajdował:

1. **Pacjenta po PESEL** – tabela `Patients`
2. **Wizyty danego lekarza** – tabela `Visits`

Porównujesz dwa stany:
- **PRZED** – bez indeksów (wolniej, skan całej tabeli)
- **PO** – z indeksami (szybciej, precyzyjne wyszukiwanie)

Różnicę widać w **Execution Plan** (plan wykonania zapytania) w SSMS.

---

## Czego potrzebujesz

| Narzędzie | Po co |
|-----------|--------|
| **Docker Desktop** | Baza SQL Server w kontenerze |
| **Visual Studio / terminal** | Uruchomienie aplikacji i migracji |
| **SSMS** (SQL Server Management Studio) | Uruchamianie zapytań i planów wykonania |
| **Word / LibreOffice** (opcjonalnie) | Składanie PDF z screenshotów |

SSMS pobierz: https://aka.ms/ssmsfullsetup

---

## CZĘŚĆ 1 – Przygotowanie (jednorazowo)

### Krok 1: Uruchom Docker i bazę

1. Włącz **Docker Desktop** i poczekaj, aż się uruchomi.
2. Otwórz terminal w folderze projektu (`ClinicManager`).
3. Wklej:

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=ClinicAdmin!2026" -p 1433:1433 --name clinic-sql -d mcr.microsoft.com/mssql/server:2022-latest
```

> Jeśli kontener już istnieje: `docker start clinic-sql`

Sprawdzenie: `docker ps` – powinien być widoczny `clinic-sql`.

---

### Krok 2: Connection string (User Secrets)

W terminalu (w folderze projektu):

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=ClinicManagerDb;User Id=sa;Password=ClinicAdmin!2026;TrustServerCertificate=True;MultipleActiveResultSets=true" --project src/ClinicManager.Web
dotnet user-secrets set "SeedData:AdminPassword" "Admin123!" --project src/ClinicManager.Web
dotnet user-secrets set "SeedData:DoctorPassword" "Lekarz123!" --project src/ClinicManager.Web
dotnet user-secrets set "SeedData:RegPassword" "Rej123!" --project src/ClinicManager.Web
```

---

### Krok 3: Migracje bazy

```bash
dotnet ef database update --project src/ClinicManager.Web --startup-project src/ClinicManager.Web
```

Powinno przejść bez błędu. Baza `ClinicManagerDb` ma już indeksy.

---

### Krok 4: Uruchom aplikację i dodaj dane testowe

```bash
dotnet run --project src/ClinicManager.Web
```

Wejdź w przeglądarce na adres z terminala (np. `http://localhost:5215`).

1. Zaloguj się jako rejestratorka: `rejestracja@clinic.com` / `Rej123!`
2. **Pacjenci** → dodaj pacjenta z PESEL np. `85010112345`
3. **Wizyty** → dodaj wizytę dla tego pacjenta i lekarza `lekarz@clinic.com`

> Bez pacjenta i wizyty zapytania SQL zwrócą puste wyniki – plan i tak zobaczysz, ale lepiej mieć dane.

---

### Krok 5: Połącz SSMS z bazą

1. Otwórz **SSMS**.
2. **Connect** → Server type: Database Engine.
3. Server name: `localhost,1433`
4. Authentication: **SQL Server Authentication**
5. Login: `sa` | Password: `ClinicAdmin!2026`
6. Connect.

Po lewej: **Databases** → **ClinicManagerDb** – powinna istnieć.

---

## CZĘŚĆ 2 – Test „PRZED” (bez indeksów)

### Krok 6: Włącz plan wykonania w SSMS

1. W SSMS: menu **Query** → **Include Actual Execution Plan**
2. Albo skrót: **Ctrl + M**
3. Ikona powinna być „wciśnięta” / podświetlona.

---

### Krok 7: Usuń indeksy (symulacja stanu przed optymalizacją)

1. **File** → **Open** → **File**
2. Otwórz: `docs/sql/00-usun-indeksy-przed-testem.sql`
3. Kliknij **Execute** (F5).

W oknie Messages powinno być: *Indeksy usunięte*.

---

### Krok 8: Uruchom zapytania PRZED

1. Otwórz: `docs/sql/01-query-plan-przed-optymalizacja.sql`
2. **Nie uruchamiaj całego pliku naraz** – SSMS robi to po `GO`, ale do screenshotów lepiej **pojedynczo**:

**Zapytanie 1 (PESEL – czy istnieje):** zaznacz blok od `DECLARE @Pesel` do pierwszego `GO`, naciśnij **F5**.

**Zapytanie 2 (lista po PESEL):** zaznacz drugi blok, **F5**.

**Zapytanie 3 (wizyty lekarza):** zaznacz trzeci blok, **F5**.

---

### Krok 9: Screenshot planu PRZED

Po każdym zapytaniu:

1. Na dole okna wyników pojawi się zakładka **Execution Plan** (Plan wykonania).
2. Kliknij ją – zobaczysz diagram (prostokąty ze strzałkami).
3. Szukaj napisu **Clustered Index Scan** lub **Index Scan** – to „wolna” wersja.
4. Zrób screenshot: **Win + Shift + S**, zaznacz plan.
5. Zapisz np. jako:
   - `pesel-przed.png`
   - `lista-pesel-przed.png`
   - `lekarz-przed.png`

**Gdzie patrzeć w Messages:** linia typu  
`Table 'Patients'. Scan count 1, logical reads XX` – zapisz liczbę **logical reads** (przyda się do PDF).

---

## CZĘŚĆ 3 – Test „PO” (z indeksami)

### Krok 10: Przywróć indeksy

1. Otwórz: `docs/sql/03-przywroc-indeksy.sql`
2. **Execute** (F5).

---

### Krok 11: Uruchom zapytania PO

1. Otwórz: `docs/sql/02-query-plan-po-optymalizacji.sql`
2. Najpierw uruchom pierwszy blok (weryfikacja indeksów) – powinny być:
   - `IX_Patients_Pesel`
   - `IX_Visits_DoctorId_ScheduledDate`
3. Potem **po kolei** te same 3 zapytania co w kroku 8 (zaznacz blok → F5).

---

### Krok 12: Screenshot planu PO

Znowu zakładka **Execution Plan**.

Tym razem szukaj:
- **Index Seek** na `IX_Patients_Pesel` (zapytania 1 i 2)
- **Index Seek** na `IX_Visits_DoctorId_ScheduledDate` (zapytanie 3)
- Przy lekarzu **nie powinno** być osobnego klocka **Sort** (sortowanie idzie z indeksu)

Zapisz screenshoty:
- `pesel-po.png`
- `lista-pesel-po.png`
- `lekarz-po.png`

Porównaj **logical reads** z wersją PRZED – zwykle jest mniej.

---

## CZĘŚĆ 4 – Co wpisać w raporcie (PDF)

Minimalna zawartość na zaliczenie:

| Strona | Treść |
|--------|--------|
| 1 | Tytuł: US#9 Optymalizacja bazy – indeksy. Krótko: po co PESEL i lekarz. |
| 2 | Zapytanie PESEL – screenshot PRZED + PRZED logical reads |
| 3 | Zapytanie PESEL – screenshot PO + PO logical reads |
| 4 | Wizyty lekarza – PRZED vs PO (2 screenshoty obok siebie lub pod sobą) |
| 5 | Podsumowanie: „Po dodaniu indeksów plan zmienił się ze Scan na Seek” |

Zapisz jako: `docs/us9-raport-indeksy.pdf`

---

## Ściąga – co znaczą klocki w planie

| Napis w planie | Proste znaczenie |
|----------------|------------------|
| **Clustered Index Scan** | Przegląda **całą** tabelę – wolno |
| **Index Scan** | Przegląda dużo wierszy indeksu – średnio |
| **Index Seek** | Od razu trafia w konkretne wiersze – **szybko** ✅ |
| **Sort** | Dodatkowe sortowanie w pamięci – wolniej |
| **Key Lookup** | Po indeksie musi jeszcze dociągnąć kolumny – OK, ale gorsze niż sam Seek |

**Cel US#9:** PRZED = Scan, PO = Seek.

---

## Test w aplikacji (opcjonalnie, dla pewności)

1. Uruchom aplikację (`dotnet run`).
2. Zaloguj jako rejestratorka.
3. **Pacjenci** → w polu szukaj wpisz pełny PESEL `85010112345` → powinien znaleźć pacjenta.
4. Zaloguj jako lekarz (`lekarz@clinic.com` / `Lekarz123!`).
5. **Wizyty** → widzisz tylko swoje wizyty posortowane po dacie.

To potwierdza, że kod działa – ale **dowód optymalizacji** to screenshoty z SSMS.

---

## Najczęstsze problemy

| Problem | Rozwiązanie |
|---------|-------------|
| Docker nie startuje | Włącz Docker Desktop ręcznie, poczekaj 1–2 min |
| Nie łączy się SSMS | Sprawdź `docker ps`, hasło `ClinicAdmin!2026`, port `1433` |
| Brak zakładki Execution Plan | Włącz **Ctrl+M** przed F5 |
| `@DoctorId` = NULL | Uruchom aplikację raz – seed tworzy `lekarz@clinic.com` |
| Zapytanie zwraca 0 wierszy | Dodaj pacjenta z PESEL `85010112345` i wizytę w aplikacji |
| Po teście aplikacja dziwnie działa | Uruchom `03-przywroc-indeksy.sql` albo `dotnet ef database update` |

---

## Kolejność plików (pamiętajka)

```
1. 00-usun-indeksy-przed-testem.sql     ← usuń indeksy
2. 01-query-plan-przed-optymalizacja.sql ← zapytania + screenshoty PRZED
3. 03-przywroc-indeksy.sql              ← przywróć indeksy
4. 02-query-plan-po-optymalizacji.sql   ← zapytania + screenshoty PO
5. Word/PDF → docs/us9-raport-indeksy.pdf
```

---

## Powiązane pliki w repo

- `docs/US9-analiza-indeksow.md` – opis techniczny (dla dokumentacji projektu)
- `src/ClinicManager.Web/Data/ClinicDbContext.cs` – definicja indeksów w kodzie
