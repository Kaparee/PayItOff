<div align="center">
  <img src="img/widok_projektu.png" width="80" alt="PayItOff Logo"/>
  <h1>PayItOff</h1>
  <p><strong>A cross-platform financial settlement engine and bill-splitting mobile app</strong></p>
  <p>
    <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white"/>
    <img src="https://img.shields.io/badge/MAUI-Multiplatform-blue?style=for-the-badge&logo=xamarin&logoColor=white"/>
    <img src="https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white"/>
    <img src="https://img.shields.io/badge/ASP.NET_Core-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white"/>
    <img src="https://img.shields.io/badge/Entity_Framework_Core-EF-blueviolet?style=for-the-badge"/>
    <img src="https://img.shields.io/badge/JWT-Auth-orange?style=for-the-badge&logo=jsonwebtokens&logoColor=white"/>
  </p>
</div>

---

## Roadmapa

Poniżej przedstawiono planowane kierunki rozwoju projektu w kolejnych wersjach.

### Wersja 2.0 – Live & Connect

- [ ] **SignalR (Real-Time Updates):** Podpięcie hubów po stronie API oraz klienta MAUI. Aktualizacje wydatków i sald pojawiają się u wszystkich członków grupy w czasie rzeczywistym bez konieczności odświeżania ekranu.
- [ ] **Powiadomienia Push (FCM):** Integracja z Firebase Cloud Messaging – powiadomienia systemowe na zablokowanym ekranie telefonu przy każdej nowej płatności lub zaproszeniu.

### Wersja 2.1 – Smart Money

- [ ] **Inteligentne skanowanie paragonów (OCR):** Przepuszczenie wgrywanego zdjęcia przez Azure AI Document Intelligence w celu automatycznego wyciągnięcia kwot i nazw produktów.
- [ ] **Wielowalutowość:** Pole `Currency` przy wydatku, pobieranie kursów z API NBP i automatyczne przeliczanie sald na PLN.
- [ ] **Automatyczna kompensacja długów:** Uruchamianie algorytmu Simplify Debts po każdym zaakceptowanym rozliczeniu bez konieczności ręcznego wywołania.

### Wersja 2.2 – Premium UX

- [ ] **Tryb Offline (SQLite):** Buforowanie danych grupy lokalnie na urządzeniu. Aplikacja działa bez internetu i synchronizuje się po odzyskaniu połączenia.
- [ ] **Oś czasu grupy (Audit Trail UI):** Wyciągnięcie istniejących logów `AuditLogInterceptor` na dedykowany ekran z czytelną osią czasu zmian (kto, co i kiedy zmienił).
- [ ] **Eksport do PDF:** Endpoint generujący miesięczny raport wydatków grupy z użyciem biblioteki QuestPDF, wysyłany na e-mail.

---

## O Projekcie

**PayItOff** to zaawansowana, wieloplatformowa aplikacja mobilna (Windows & Android) przeznaczona do zarządzania wspólnymi wydatkami i rozliczeniami w grupach znajomych. Projekt eliminuje problem ręcznego obliczania "kto jest komu winien" po wspólnych wyjściach, wyjazdach czy zakupach.

Zbudowany w oparciu o **Clean Architecture**, system posiada własny silnik finansowy z autorskim algorytmem kompensacji długów, asynchronicznym harmonogramowaniem zadań w tle (Hangfire), pełnym systemem audytu zmian bazy danych oraz rozbudowanym interfejsem MAUI z obsługą Drag & Drop.

---

## Główne Funkcjonalności

| Moduł | Opis |
|---|---|
| **Autoryzacja** | Rejestracja z weryfikacją e-mail, logowanie JWT, reset hasła przez link tokenowy, zmiana e-mail z potwierdzeniem |
| **Znajomi & Grupy** | System zaproszeń, RBAC (role Owner/Admin/Member), zarządzanie grupami wydatków, ulubione grupy |
| **Paragony** | Dodawanie rachunków z podziałem na produkty i kategorie, parser wyrażeń matematycznych w polu ceny (`5.50 + 2*3`), wgrywanie zdjęć paragonów |
| **Rozliczenia** | Spłaty brutto/netto, algorytm kompensacji wzajemnych długów (Simplify Debts) do 500 iteracji, przypomnienia o spłacie z zabezpieczeniem antyspamowym (24h) |
| **Powiadomienia** | System alertów w aplikacji z filtrowaniem (nieprzeczytane, wymagające akcji), codzienne podsumowanie zbiorcze (Hangfire), SMTP e-mails (MailKit) |
| **Audit Trail** | Automatyczne logowanie każdej zmiany w bazie przez interceptory EF Core z porównaniem wartości przed i po |
| **Hangfire** | Zadania w tle (Daily Summary Job codziennie o 20:00), Dashboard do monitorowania kolejki |
| **Archiwum** | Miękkie usuwanie (Soft Delete) grup i wydatków, tryb tylko do odczytu dla zarchiwizowanych środowisk |
| **Aktualizacje** | Wbudowany `AppUpdateService` sprawdzający dostępność nowej wersji aplikacji przy starcie |

---

## Architektura

Projekt stosuje **Clean Architecture** z podziałem na 5 niezależnych warstw:

```
PayItOff/
├── PayItOff.Domain/          # Encje, reguły biznesowe, wyjątki domenowe (16 klas)
├── PayItOff.Application/     # Serwisy aplikacyjne, walidatory FluentValidation
├── PayItOff.Infrastructure/  # EF Core, repozytoria, JWT, BCrypt, Hangfire
├── PayItOff.Api/             # ASP.NET Core Web API, kontrolery, middleware
├── PayItOff.Shared/          # 22 Requesty + 20 Responses współdzielone z klientem
└── PayItOff.MauiClient/      # .NET MAUI, MVVM (CommunityToolkit), Drag & Drop
```

### Stos Technologiczny

**Backend:**
- ASP.NET Core 10 Web API + Entity Framework Core 10
- PostgreSQL + Npgsql
- JWT Authentication (HmacSha256) + BCrypt.Net-Next
- Hangfire 1.8 (zadania cykliczne, Dashboard)
- FluentValidation + Swashbuckle (Swagger UI)
- MailKit (SMTP / Mailtrap)
- Humanizer (polskie daty: "10 dni temu")

**Frontend (MAUI):**
- .NET MAUI (Windows + Android)
- CommunityToolkit.Mvvm (MVVM, ObservableProperty, RelayCommand)
- IHttpClientFactory + DelegatingHandler (automatyczna iniekcja JWT)
- SecureStorage (bezpieczne przechowywanie tokenu)
- Drag & Drop (natywny GestureRecognizer)

---

## Zrzuty Ekranu

> **Uwaga:** Poniższe zrzuty ekranu mogą nie odzwierciedlać aktualnego stanu projektu. Interfejs jest w ciągłym rozwoju – część funkcjonalności opisanych w dokumentacji mogła zostać wizualnie zmieniona lub rozszerzona względem przedstawionych screenshotów.

### Dashboard i Nawigacja

<p align="center">
  <img src="img/main/main_pusty.png" width="30%" alt="Dashboard pusty"/>
  &nbsp;
  <img src="img/main/main_po_dodaniu_wydatku.png" width="30%" alt="Dashboard z wydatkami"/>
  &nbsp;
  <img src="img/sidebar.png" width="30%" alt="Menu boczne"/>
</p>

Dashboard prezentuje sumaryczne saldo przychodów i wydatków, karuzelę aktywnych grup oraz ostatnie powiadomienia. Boczne menu (SidebarMenu) podświetla aktualnie otwartą zakładkę i obsługuje wylogowanie z czyszczeniem SecureStorage.

<p align="center">
  <img src="img/main/main_po_akceptacji_i_odrzuceniu_splat.png" width="60%" alt="Dashboard po rozliczeniach"/>
</p>

### Rejestracja i Logowanie

<p align="center">
  <img src="img/register/register1.png" width="22%" alt="Rejestracja krok 1"/>
  &nbsp;
  <img src="img/register/register2.png" width="22%" alt="Rejestracja krok 2"/>
  &nbsp;
  <img src="img/login/login1.png" width="22%" alt="Logowanie"/>
  &nbsp;
  <img src="img/login/login_widoczne_haslo.png" width="22%" alt="Podgląd hasła"/>
</p>

Formularz rejestracji weryfikuje hasło (min. 8 znaków, duża/mała litera, znak specjalny), numer telefonu (format PL) i IBAN (26-cyfrowy polski format z opcjonalnym prefixem PL). Po rejestracji system wysyła e-mail z tokenem aktywacyjnym przez SMTP.

<p align="center">
  <img src="img/register/register_mailtrapio.png" width="45%" alt="Weryfikacja e-mail"/>
  &nbsp;
  <img src="img/register/register_success.png" width="45%" alt="Sukces rejestracji"/>
</p>

<p align="center">
  <img src="img/login/login_error.png" width="45%" alt="Błąd logowania"/>
</p>

### Zarządzanie Kontem

<p align="center">
  <img src="img/account/account1.png" width="90%" alt="Profil użytkownika"/>
</p>

<p align="center">
  <img src="img/account/account_zmiana_danych.png" width="45%" alt="Edycja danych"/>
  &nbsp;
  <img src="img/account/account_edycja_ustawien_powiadomien.png" width="45%" alt="Ustawienia powiadomień"/>
</p>

Panel konta umożliwia edycję danych osobowych (nick, IBAN), zmianę awatara, zmianę hasła, zmianę adresu e-mail (z potwierdzeniem tokenowym) oraz zarządzanie preferencjami powiadomień. Każda zmiana ustawień powiadomień jest natychmiast synchronizowana z API.

<p align="center">
  <img src="img/account/account_zmiana_avatara.png" width="45%" alt="Zmiana awatara"/>
  &nbsp;
  <img src="img/account/account_zmiana_hasla.png" width="45%" alt="Zmiana hasła"/>
</p>

<p align="center">
  <img src="img/account/account_zmiana_maila.png" width="90%" alt="Zmiana e-mail"/>
</p>

### Znajomi

<p align="center">
  <img src="img/friend/friend_puste.png" width="45%" alt="Lista znajomych pusta"/>
  &nbsp;
  <img src="img/friend/friend_wyszukiwanie.png" width="45%" alt="Wyszukiwanie znajomych"/>
</p>

<p align="center">
  <img src="img/friend/friend_wyslane_zaproszenia.png" width="45%" alt="Wysłane zaproszenia"/>
  &nbsp;
  <img src="img/friend/friend_oczekujace_zaproszenie.png" width="45%" alt="Oczekujące zaproszenia"/>
</p>

<p align="center">
  <img src="img/friend/friend_nowy_widok_po_akceptacji.png" width="60%" alt="Widok po akceptacji"/>
</p>

<p align="center">
  <img src="img/friend/friend_ranking_po_dodaniu_wydatku.png" width="90%" alt="Ranking znajomych z bilansem"/>
</p>

Moduł znajomych wyświetla zbiorczy bilans finansowy z każdym kontaktem wyliczany w locie z tabeli `GroupDebts`. Wyszukiwarka obsługuje filtrowanie po nicku, e-mailu i numerze telefonu z debouncingiem (CancellationTokenSource) dla płynnego wyszukiwania znak po znaku.

### Grupy Wydatków

<p align="center">
  <img src="img/group/group_pusty_widok.png" width="45%" alt="Lista grup pusta"/>
  &nbsp;
  <img src="img/group/group_tworzenie.png" width="45%" alt="Tworzenie grupy"/>
</p>

<p align="center">
  <img src="img/group/group_utworzona_grupa.png" width="90%" alt="Nowa grupa"/>
</p>

<p align="center">
  <img src="img/group/group_zaproszenie_członkow.png" width="45%" alt="Zapraszanie członków"/>
  &nbsp;
  <img src="img/group/group_szybkie_zaproszenie_znajomego.png" width="45%" alt="Szybkie zaproszenie"/>
</p>

<p align="center">
  <img src="img/group/group_widok_czlonka.png" width="90%" alt="Panel zarządzania członkami"/>
</p>

<p align="center">
  <img src="img/group/group_zmiana_roli_admin.png" width="30%" alt="Zmiana roli"/>
  &nbsp;
  <img src="img/group/group_udana_zmiana_roli.png" width="30%" alt="Udana zmiana roli"/>
  &nbsp;
  <img src="img/group/group_opuszczenie_grupy.png" width="30%" alt="Opuszczenie grupy"/>
</p>

System RBAC (Role-Based Access Control) wymusza hierarchię Owner > Admin > Member. Właściciela grupy nie można wyrzucić ani zdegradować. Usunięcie grupy jest blokowane jeśli istnieją w niej nieuregulowane długi (`HasActiveGroupDebt`).

<p align="center">
  <img src="img/group/group_widok_po_dodaniu_wydatków.png" width="90%" alt="Widok grupy z wydatkami"/>
</p>

<p align="center">
  <img src="img/group/group_widok_listy_transakcji.png" width="90%" alt="Historia transakcji w grupie"/>
</p>

### Dodawanie Paragonów

<p align="center">
  <img src="img/new_expense/expense_pusty.png" width="90%" alt="Pusty kreator wydatku"/>
</p>

<p align="center">
  <img src="img/new_expense/new_expense_stworzenie_kategorii.png" width="45%" alt="Tworzenie kategorii"/>
  &nbsp;
  <img src="img/new_expense/new_expense_stworzenie_grupy_produktow.png" width="45%" alt="Grupowanie produktów"/>
</p>

<p align="center">
  <img src="img/new_expense/new_expense_wpisane_przedmioty_przydzieleni_wiezyciele_operacje_matematyczne.png" width="45%" alt="Parser matematyczny i przypisanie dłużników"/>
  &nbsp;
  <img src="img/new_expense/new_expense_podzial_wydatkow.png" width="45%" alt="Podział wydatków"/>
</p>

Kreator paragonów obsługuje parser wyrażeń matematycznych (`System.Data.DataTable.Compute`) wbudowany bezpośrednio w pole ceny – wystarczy wpisać `5.50 + 2*3` zamiast ręcznie liczyć. Algorytm **Penny-Drop** rozwiązuje problem reszty z dzielenia (np. 10 zł / 3 osoby) rozdzielając nadmiarowe grosze kolejnym uczestnikom po zaokrągleniu w dół.

<p align="center">
  <img src="img/new_expense/new_expense_usuniecie_uczestnika_zmiana_kwot.png" width="30%" alt="Usunięcie uczestnika"/>
  &nbsp;
  <img src="img/new_expense/new_expense_przywracanie.png" width="30%" alt="Przywracanie uczestnika"/>
  &nbsp;
  <img src="img/new_expense/new_expense_zatwierdzenie_przedmiotow.png" width="30%" alt="Zatwierdzenie"/>
</p>

<p align="center">
  <img src="img/new_expense/new_expense_blad_przy_nieprzypisaniu_wszytskiego.png" width="45%" alt="Blokada przy nieprzypisaniu"/>
  &nbsp;
  <img src="img/new_expense/new_expense_success.png" width="45%" alt="Sukces"/>
</p>

Przycisk zapisu jest zablokowany dopóki suma przypisanych kwot nie wynosi dokładnie 0 złotych różnicy wobec kwoty całkowitej paragonu. Cały kreator reaguje na Drag & Drop – produkty przeciągane są na awatary uczestników.

### Edycja Historycznych Wydatków

<p align="center">
  <img src="img/group/group_klikniecie_w_przedmiot_na_liscie.png" width="90%" alt="Szczegóły wydatku Drill-Down"/>
</p>

<p align="center">
  <img src="img/group/group_edycja_wydatku.png" width="30%" alt="Edycja wydatku"/>
  &nbsp;
  <img src="img/group/group_edycja_wydatku_brak_rozlozenia.png" width="30%" alt="Błąd rozłożenia"/>
  &nbsp;
  <img src="img/group/group_edycja_wydatku_poprawne.png" width="30%" alt="Poprawna edycja"/>
</p>

Edycja archiwalnego wydatku transakcyjnie cofa stary podział długów z bazy, przelicza i zapisuje nowy. Modal blokuje zapis dopóki `RemainingAmountToSplit` nie wynosi 0.

### Rozliczenia (Settlement)

<p align="center">
  <img src="img/settlement/settlement_widok_i_podzial_na_filtry.png" width="45%" alt="Panel rozliczeń z filtrami"/>
  &nbsp;
  <img src="img/settlement/settlement_oplacenie_dlugow_gdy_ich_nie_ma.png" width="45%" alt="Czysta karta"/>
</p>

<p align="center">
  <img src="img/settlement/settlement_oplacenie_brutto.png" width="30%" alt="Spłata brutto"/>
  &nbsp;
  <img src="img/settlement/settlement_oplacenie_netto.png" width="30%" alt="Spłata netto"/>
  &nbsp;
  <img src="img/settlement/settlement_success_wyslania_oplacenie.png" width="30%" alt="Potwierdzenie wysłania"/>
</p>

Panel rozliczeń obsługuje dwa tryby spłat. Tryb **brutto** spłaca konkretny dług z konkretnej grupy. Tryb **netto** agreguje wszystkie wzajemne długi między dwiema osobami z wszystkich wspólnych grup, kompensuje je (`CompensateBilateralDebtsAsync`, max 500 iteracji) i tworzy jedną zbiorczą spłatę.

<p align="center">
  <img src="img/settlement/settlement_przypomnienie.png" width="45%" alt="Przypomnienie o spłacie"/>
  &nbsp;
  <img src="img/settlement/settlement_powoadomienie_osoby_do_ktorej_poszlo_przypomnienie.png" width="45%" alt="Powiadomienie dłużnika"/>
</p>

<p align="center">
  <img src="img/settlement/settlement_wyslana_splata_do_akceptacji.png" width="90%" alt="Spłata oczekująca na akceptację"/>
</p>

<p align="center">
  <img src="img/settlement/settlement_potwierdzenie_splaty.png" width="30%" alt="Potwierdzenie spłaty"/>
  &nbsp;
  <img src="img/settlement/settlement_potwierdzenie_splaty_success.png" width="30%" alt="Sukces potwierdzenia"/>
  &nbsp;
  <img src="img/settlement/settlement_odrzucenie_splaty.png" width="30%" alt="Odrzucenie spłaty"/>
</p>

Wierzyciel musi zatwierdzić lub odrzucić każdą deklarację spłaty. Odrzucenie przywraca oryginalny stan długu w tabeli `GroupDebts`. Przypomnienia o spłacie mają wbudowane zabezpieczenie antyspamowe – nie można wysłać kolejnego w ciągu 24 godzin.

### Powiadomienia

<p align="center">
  <img src="img/powiadomienia/powiadomienia_widok_glowny.png" width="90%" alt="Centrum powiadomień"/>
</p>

<p align="center">
  <img src="img/powiadomienia/powiadomienia_widok_po_kliknieicu_w_powiadomienie.png" width="45%" alt="Szczegóły powiadomienia"/>
  &nbsp;
  <img src="img/powiadomienia/powiadomienia_oznacz_wszytskie.png" width="45%" alt="Oznacz wszystkie"/>
</p>

<p align="center">
  <img src="img/powiadomienia/powiadomienia_zbiorcze_eytkieta.png" width="45%" alt="Etykieta powiadomienia zbiorczego"/>
  &nbsp;
  <img src="img/powiadomienia/powiadomienia_zbiorcze_popup.png" width="45%" alt="Popup powiadomienia zbiorczego"/>
</p>

Powiadomienia typu `NeedAction` (zaproszenia, spłaty) można obsłużyć bezpośrednio z listy bez wchodzenia do modułu grupy czy znajomych. Codziennie o 20:00 Hangfire generuje zbiorcze podsumowanie dnia agregując ukryte mikropowiadomienia.

### Archiwum

<p align="center">
  <img src="img/archiwum/archiwum_pusty_widok.png" width="30%" alt="Puste archiwum"/>
  &nbsp;
  <img src="img/archiwum/archiwum_usunieta_grupa.png" width="30%" alt="Usunięta grupa w archiwum"/>
  &nbsp;
  <img src="img/archiwum/archiwum_grupa_read-only.png" width="30%" alt="Tryb read-only"/>
</p>

### API – Swagger UI

<p align="center">
  <img src="img/swagger1.png" width="45%" alt="Swagger – Expense, Friend, Group"/>
  &nbsp;
  <img src="img/swagger2.png" width="45%" alt="Swagger – GroupMember, Notification, Seeder"/>
</p>

<p align="center">
  <img src="img/swagger3.png" width="45%" alt="Swagger – Settlement"/>
  &nbsp;
  <img src="img/swagger4.png" width="45%" alt="Swagger – User"/>
</p>

### Hangfire Dashboard

<p align="center">
  <img src="img/hangfire.png" width="90%" alt="Hangfire Dashboard"/>
</p>

### Schemat Bazy Danych (ERD)

<p align="center">
  <img src="img/diagram_erd.png" width="90%" alt="Diagram ERD bazy danych"/>
</p>

Baza składa się z 12 tabel połączonych kluczami obcymi z `DeleteBehavior.Restrict` (brak kaskadowego usuwania). Każda wartość finansowa (`decimal`) ma narzuconą precyzję `18,2`. Tabela `GroupDebts` posiada `IsConcurrencyToken()` na polu `Amount` chroniący przed Race Conditions.

---

## Kluczowe Algorytmy

### Penny-Drop Problem (Algorytm podziału groszowego)
Zapobiega gubienia grosza przy dzieleniu kwot na uczestników:
```csharp
decimal baseAmount = Math.Floor((totalAmount / count) * 100) / 100;
decimal remainder = totalAmount - baseAmount * count;
int pennies = (int)Math.Round(remainder * 100);

foreach (var participant in sortedParticipants)
{
    decimal amount = baseAmount + (pennies-- > 0 ? 0.01m : 0);
    // przypisanie kwoty do uczestnika
}
```

### Automatyczna iniekcja JWT (DelegatingHandler)
```csharp
public class AuthHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await SecureStorage.Default.GetAsync("jwt_token");
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}
```

### Kompensacja Długów (Simplify Debts)
System automatycznie redukuje wzajemne zobowiązania: jeśli A wisi B 50 zł, a B wisi A 30 zł, algorytm w pętli (max 500 iteracji) sprowadza to do jednego długu A→B w wysokości 20 zł.

### Parser Wyrażeń Matematycznych
Pole ceny produktu akceptuje wyrażenia takie jak `5.50 + 2*3` używając wbudowanego silnika `System.Data.DataTable.Compute` bez żadnych zewnętrznych zależności.

---

## Uruchomienie Lokalne

### Wymagania
- .NET 10 SDK
- Docker Desktop
- Visual Studio 2022 / JetBrains Rider

### Krok 1 – Uruchomienie bazy danych i API (Docker)

Plik `docker-compose.yml` definiuje dwa serwisy: bazę PostgreSQL (port `5442`) oraz skonteneryzowane API (port `8080`). Wszystkie sekrety (hasło DB, klucz JWT) są wstrzykiwane przez zmienne środowiskowe – nie wymagają zmian w `appsettings.json`.

```bash
docker-compose up -d
```

Po uruchomieniu API jest dostępne pod adresem `http://localhost:8080`.  
Swagger UI dostępny pod: `http://localhost:8080/swagger`  
Hangfire Dashboard dostępny pod: `http://localhost:8080/hangfire`

### Krok 2 – Migracje bazy danych

Po pierwszym uruchomieniu kontenerów należy zastosować migracje EF Core. Baza PostgreSQL w Docker nasłuchuje lokalnie na porcie `5442`.

```bash
dotnet ef database update --project PayItOff.Infrastructure --startup-project PayItOff.Api
```

### Krok 3 – Konfiguracja e-mail (opcjonalnie)

System wysyła e-maile weryfikacyjne przez SMTP. Aby działało to lokalnie, załóż darmowe konto na [Mailtrap.io](https://mailtrap.io) i uzupełnij plik `appsettings.json`:

```json
"EmailSettings": {
  "Host": "sandbox.smtp.mailtrap.io",
  "Port": 2525,
  "UserName": "TWOJ_MAILTRAP_USERNAME",
  "Password": "TWOJE_MAILTRAP_HASLO"
}
```

### Krok 4 – Uruchomienie klienta MAUI

Klient MAUI automatycznie wykrywa platformę i używa odpowiedniego adresu API:
- **Windows:** `http://localhost:8080/api/`
- **Emulator Android:** `http://10.0.2.2:8080/api/` (specjalny alias dla localhost hosta)

Otwórz projekt w Visual Studio i uruchom z wybraną platformą docelową (Windows lub Android), lub przez CLI:

```bash
# Windows
dotnet build -t:Run -f net10.0-windows10.0.19041.0 PayItOff.MauiClient/PayItOff.MauiClient.csproj

# Android (wymaga podłączonego urządzenia lub uruchomionego emulatora)
dotnet build -t:Run -f net10.0-android PayItOff.MauiClient/PayItOff.MauiClient.csproj
```


---

## Struktura Projektu

```
PayItOff/
├── PayItOff.Api/
│   ├── Controllers/          # ExpenseController, GroupController, SettlementController...
│   ├── Middleware/           # ExceptionMiddleware (mapowanie błędów domenowych na HTTP 4xx)
│   └── appsettings.json      # Konfiguracja (bez sekretów)
├── PayItOff.Application/
│   ├── Services/             # ExpenseService, SettlementService, NotificationService...
│   ├── Helpers/              # DebtCalculator (Penny-Drop), PhoneNumberHelper
│   └── Validators/           # FluentValidation: IBAN, Telefon, Hasło
├── PayItOff.Domain/
│   ├── Entities/             # Expense, Group, Settlement, GroupDebt, User...
│   ├── Exceptions/           # 16 niestandardowych wyjątków domenowych
│   └── DomainServices/       # DebtCalculator
├── PayItOff.Infrastructure/
│   ├── Repositories/         # Pattern Repozytorium dla każdej encji
│   ├── Migrations/           # Historia migracji EF Core
│   └── Interceptors/         # AuditLogInterceptor (śledzi wszystkie zmiany w bazie)
├── PayItOff.Shared/
│   ├── Requests/             # 22 klasy DTO żądań
│   └── Responses/            # 20 klas DTO odpowiedzi
├── PayItOff.MauiClient/
│   ├── Views/                # 10+ ekranów XAML
│   ├── ViewModels/           # MVVM z CommunityToolkit
│   ├── Services/             # Klienci HTTP (AuthService, ExpenseService...)
│   └── Controls/             # AppButton, SidebarMenu
├── docker-compose.yml
└── AndroidSigning.props.example  # Przykładowa konfiguracja podpisywania APK
```

---

## Bezpieczeństwo

- **Hasła:** Hashowane z BCrypt (bez Plain-Text nigdzie w bazie)
- **Tokeny JWT:** Symetryczny podpis HMAC-SHA256, przechowywane przez `SecureStorage`
- **Walidacja IBAN:** Regex wymuszający polski format 26-cyfrowy (`PL00 0000 0000...`)
- **Walidacja Hasła:** Min. 8 znaków, wymagana duża/mała litera i znak specjalny
- **Audit Trail:** Każda operacja `INSERT/UPDATE/DELETE` logowana automatycznie przez EF Core Interceptor
- **Blokady kaskadowe:** `DeleteBehavior.Restrict` – brak możliwości przypadkowego skasowania historii finansowej
- **Race Conditions:** `IsConcurrencyToken()` na polu `Amount` w tabeli `GroupDebts`

---

## Autor

**Jakub Płocica**  
Projekt zaliczeniowy – Programowanie Obiektowe 2  
Uniwersytet Rzeszowski, Wydział Nauk Ścisłych i Technicznych, 2026
