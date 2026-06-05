# US#12 i US#13 – Poradnik testowania (krok po kroku)

Ten dokument tłumaczy **od zera**, jak przetestować dwie nowe funkcje:

- **US#12 – Procedury medyczne** (katalog procedur + koszt świadczeń doliczany do wizyty)
- **US#13 – Dawkowanie leków** (pole „Dawkowanie” przy lekach na wizycie)

---

## Co w ogóle testujemy?

| US | Funkcja | Gdzie widać |
|----|---------|-------------|
| **US#12** | Katalog procedur (CRUD) + dodawanie procedur do wizyty | zakładka **Procedury** + szczegóły wizyty |
| **US#12** | Koszt całkowity wizyty = **leki + procedury** | szczegóły wizyty (Koszt całkowity) |
| **US#13** | Zalecane dawkowanie leku | szczegóły wizyty (tabela leków + formularz) |

---

## Czego potrzebujesz

| Narzędzie | Po co |
|-----------|--------|
| **Docker Desktop** | Baza SQL Server w kontenerze |
| **Visual Studio / terminal** | Uruchomienie aplikacji i migracji |
| **Przeglądarka** | Testy w interfejsie aplikacji |

---

## CZĘŚĆ 1 – Przygotowanie

### Krok 1: Uruchom Docker i bazę

1. Włącz **Docker Desktop** i poczekaj, aż wystartuje.
2. W terminalu (folder projektu `ClinicManager`):

```powershell
docker start clinic-sql
```

> Jeśli kontener nie istnieje, utwórz go raz:
> ```powershell
> docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=ClinicAdmin!2026" -p 1433:1433 --name clinic-sql -d mcr.microsoft.com/mssql/server:2022-latest
> ```

Sprawdzenie: `docker ps` – powinien być widoczny `clinic-sql`.

---

### Krok 2: Zastosuj nowe migracje

Nowe tabele (`Procedures`, `VisitProcedures`) i kolumna `Dosage` trafią do bazy poprzez migracje:

```powershell
dotnet ef database update --project src/ClinicManager.Web --startup-project src/ClinicManager.Web
```

Powinny zastosować się m.in.:
- `20260604175717_AddMedicalProcedures` – tabele procedur
- `20260604175757_AddMedicationDosage` – kolumna dawkowania

> Uwaga: aplikacja przy starcie również sama stosuje migracje (`Database.MigrateAsync()`), więc krok 2 możesz pominąć – wystarczy uruchomić aplikację (krok 3).

---

### Krok 3: Uruchom aplikację

```powershell
dotnet run --project src/ClinicManager.Web
```

Wejdź na adres z terminala (np. `http://localhost:5215`).

**Konta testowe:**

| Rola | Login | Hasło |
|------|-------|-------|
| Admin | `admin@clinic.com` | `Admin123!` |
| Lekarz | `lekarz@clinic.com` | `Lekarz123!` |
| Rejestratorka | `rejestracja@clinic.com` | `Rej123!` |

---

## CZĘŚĆ 2 – Test US#12 (Procedury medyczne)

### Krok 4: CRUD katalogu procedur

1. Zaloguj się jako **Rejestratorka** (`rejestracja@clinic.com` / `Rej123!`).
2. W górnym menu kliknij **Procedury**.
3. Kliknij **Dodaj procedurę** i utwórz np.:
   - Opis: `Konsultacja specjalistyczna`, Koszt: `150`
   - Opis: `USG jamy brzusznej`, Koszt: `200`
   - Opis: `Pobranie krwi`, Koszt: `40`
4. Sprawdź **Edytuj** – zmień koszt jednej procedury i zapisz.
5. Sprawdź **Dezaktywuj** – procedura przejdzie na status „Nieaktywna” (szary wiersz).

✅ **Oczekiwany wynik:** procedury widać na liście, edycja i dezaktywacja działają, komunikaty „Procedura została…”.

> **Walidacja:** spróbuj zapisać procedurę z pustym opisem lub kosztem `0` – powinny pojawić się błędy walidacji.

---

### Krok 5: Uprawnienia (autoryzacja)

1. Zaloguj się jako **Lekarz** (`lekarz@clinic.com` / `Lekarz123!`).
2. Wejdź w **Procedury** – powinieneś **widzieć listę**, ale **bez** przycisków „Dodaj/Edytuj/Dezaktywuj”.

✅ **Oczekiwany wynik:** lekarz tylko przegląda katalog; zarządzanie ma Admin i Rejestratorka.

---

### Krok 6: Dodanie procedury do wizyty + koszt całkowity

1. Zaloguj się jako **Rejestratorka** i utwórz wizytę (**Wizyty → Dodaj**), jeśli nie masz żadnej (data dzisiejsza lub przyszła).
2. Zaloguj się jako **Lekarz** (właściciel tej wizyty).
3. Wejdź w **Wizyty → szczegóły wizyty**.
4. Zjedź do sekcji **🩺 Wykonane procedury medyczne**.
5. Wybierz procedurę z listy, podaj ilość (np. 2) i kliknij **Dodaj procedurę**.

✅ **Oczekiwany wynik:**
- Procedura pojawia się w tabeli (Ilość, Koszt jedn., Razem).
- Wiersz **Koszt procedur** sumuje pozycje.
- Pasek **Koszt całkowity wizyty (leki + procedury)** rośnie.

6. Kliknij **Usuń** przy procedurze – koszt całkowity zmaleje.

---

## CZĘŚĆ 3 – Test US#13 (Dawkowanie)

### Krok 7: Dodanie leku z dawkowaniem

1. Jako **Lekarz** w szczegółach wizyty zjedź do sekcji **💊 Przepisane leki**.
2. W formularzu:
   - Wybierz lek z listy.
   - W polu **Dawkowanie** wpisz np. `1 tabletka 2x dziennie po posiłku`.
   - Podaj ilość.
   - Kliknij **Przepisz lek**.

✅ **Oczekiwany wynik:**
- W tabeli leków pojawia się kolumna **Dawkowanie** z wpisaną treścią.
- Jeśli zostawisz pole puste, w kolumnie będzie `—`.

> Jeśli dodasz **ten sam lek ponownie** z nowym dawkowaniem, ilość się zsumuje, a dawkowanie zostanie zaktualizowane na nowe.

---

## CZĘŚĆ 4 – Weryfikacja w bazie (opcjonalnie)

Jeśli chcesz potwierdzić zmiany w bazie (SSMS / Azure Data Studio), połącz się z `localhost,1433` (login `sa`, hasło `ClinicAdmin!2026`) i uruchom:

```sql
USE ClinicManagerDb;

-- US#12: nowe tabele
SELECT * FROM Procedures;
SELECT * FROM VisitProcedures;

-- US#13: nowa kolumna Dosage
SELECT Id, VisitId, MedicationId, Quantity, UnitPrice, Dosage
FROM VisitMedications;

-- Sprawdzenie kosztu całkowitego wizyty (leki + procedury)
SELECT v.Id,
       (SELECT ISNULL(SUM(vm.UnitPrice * vm.Quantity), 0) FROM VisitMedications vm WHERE vm.VisitId = v.Id) AS KosztLekow,
       (SELECT ISNULL(SUM(vp.UnitCost  * vp.Quantity), 0) FROM VisitProcedures  vp WHERE vp.VisitId = v.Id) AS KosztProcedur,
       v.TotalCost AS KosztCalkowity
FROM Visits v;
```

✅ **Oczekiwany wynik:** kolumna `KosztCalkowity` = `KosztLekow` + `KosztProcedur`.

---

## Lista kontrolna (odhaczaj)

```
US#12 – Procedury
  □ Dodanie procedury (Rejestratorka/Admin)
  □ Edycja procedury
  □ Dezaktywacja procedury
  □ Lekarz widzi listę, ale nie zarządza
  □ Dodanie procedury do wizyty
  □ Koszt całkowity rośnie o koszt procedur
  □ Usunięcie procedury z wizyty obniża koszt

US#13 – Dawkowanie
  □ Lek z dawkowaniem widoczny w tabeli
  □ Puste dawkowanie pokazuje „—”
```

---

## Najczęstsze problemy

| Problem | Rozwiązanie |
|---------|-------------|
| Brak zakładki **Procedury** | Zaloguj się jako Admin/Lekarz/Rejestratorka |
| `Invalid object name 'Procedures'` | Nie zastosowano migracji – uruchom `dotnet ef database update` lub po prostu odpal aplikację |
| Lista procedur pusta przy dodawaniu do wizyty | Dodaj najpierw procedurę w katalogu (musi być **aktywna**) |
| Koszt całkowity się nie zmienia | Odśwież stronę szczegółów wizyty po dodaniu pozycji |
| Nie łączy się z bazą | `docker ps` → `docker start clinic-sql`, port `1433` |

---

## Powiązane pliki w repo

- `src/ClinicManager.Web/Models/Procedure.cs`, `VisitProcedure.cs` – encje (US#12)
- `src/ClinicManager.Web/Models/VisitMedication.cs` – pole `Dosage` (US#13)
- `src/ClinicManager.Web/Services/ProcedureService.cs` – CRUD procedur
- `src/ClinicManager.Web/Services/VisitService.cs` – `RecalculateTotalCost` (leki + procedury)
- `src/ClinicManager.Web/Controllers/ProceduresController.cs`, `VisitsController.cs`
- `src/ClinicManager.Web/Views/Procedures/`, `Views/Visits/Details.cshtml`
- `src/ClinicManager.Web/Migrations/20260604175717_AddMedicalProcedures.cs`
- `src/ClinicManager.Web/Migrations/20260604175757_AddMedicationDosage.cs`
