# Dane do testów responsywności GUI

Zestaw lokalny dla `fix/frontend-responsiveness-fixes`. Wszystkie osoby, szkoły, opisy i dokumenty oznaczone GUI są fikcyjne. Regulamin oraz historia zawierają tekst testowy, a PDF-y RODO nie są rzeczywistymi zgodami.

## Logowanie

Wspólne hasło poniższych kont: `KoalaTest2026!`.

| Konto                         | Logowanie      | Scenariusz                                                                            |
| ----------------------------- | -------------- | ------------------------------------------------------------------------------------- |
| `admin@koala.test`            | `/admin/login` | Wszystkie istniejące sekcje administracyjne                                           |
| `edytor@koala.test`           | `/admin/login` | Wpisy, strony statyczne, sponsorzy, koalicjanci, pliki                                |
| `recenzent@koala.test`        | `/admin/login` | Konto recenzenta; dedykowany ekran oceniania nie jest jeszcze zaimplementowany        |
| `kapitan.pelny@koala.test`    | `/login`       | Drużyna z dwoma dodatkowymi uczestnikami, długimi nazwiskami, opiekunem i dokumentami |
| `guardian.pelny@koala.test`   | `/login`       | Cztery drużyny; selektor drużyny, podgląd i podmiana RODO, różne stany zgód           |
| `kapitan.bez-zgod@koala.test` | `/login`       | Dwóch uczestników, przypisany opiekun, brak dokumentów RODO                           |
| `gui.kapitan.03@koala.test`   | `/login`       | Drużyna bez dodatkowych uczestników i bez opiekuna                                    |
| `kapitan.pusty@koala.test`    | `/login`       | Konto bez drużyny; przycisk i modal tworzenia, duża tabela szkół                      |
| `guardian.pusty@koala.test`   | `/login`       | Konto bez drużyn; formularz dołączenia po kodzie                                      |

Dodatkowi kapitanowie: `gui.kapitan.04@koala.test` do `gui.kapitan.12@koala.test`.
Pozostali opiekunowie: `gui.guardian.02@koala.test` i `gui.guardian.03@koala.test`.
W tabeli użytkowników są też `gui.redakcja.01@koala.test`–`gui.redakcja.12@koala.test`, z różnymi kombinacjami ról, oraz jedno konto z bardzo długim adresem e-mail.

Przy zmianie konta najpierw się wyloguj. Interfejs i cookie sesji dotyczą jednego bieżącego użytkownika.

## Co jest wgrane

| Dane GUI             | Liczba | Co sprawdzać                                                                                        |
| -------------------- | -----: | --------------------------------------------------------------------------------------------------- |
| Szkoły               |    120 | Tabele, filtry, przewijanie, długie nazwy i adresy; również w modalu tworzenia drużyny              |
| Edycje w zestawie    |      4 | Obecna aktywna edycja, dwie archiwalne i jedna nieaktywna przyszła                                  |
| Wpisy                |     24 | Krótkie i wielowierszowe tytuły, krótkie komunikaty, rozbudowany Markdown; trzy wpisy ukryte        |
| Sponsorzy            |     10 | Osiem widocznych i dwóch ukrytych; różne proporcje logo i długości nazw                             |
| Koalicjanci          |     12 | Dziesięć widocznych i dwóch ukrytych; zdjęcia o różnych proporcjach i długie opisy                  |
| Obrazy PNG           |     12 | Panorama, portret, kwadrat, bardzo szeroki i wysoki obraz, mała miniatura, logo z przezroczystością |
| Publiczne PDF-y      |     24 | Po sześć osobnych plików na edycję, trzy sekcje dokumentów; dokumenty od jednej do trzech stron     |
| Drużyny GUI          |     12 | Długie nazwy, nazwy zaakceptowane i oczekujące, drużyna bez opiekuna i drużyny z opiekunem          |
| Dodatkowi uczestnicy |     22 | Krótkie i długie imiona/nazwiska, różne stany RODO                                                  |
| Dokumenty RODO       |     20 | 7 oczekujących, 7 zaakceptowanych, 6 odrzuconych; 2 uczestników bez dokumentu                       |

Dotychczasowe rekordy pozostają w bazie, więc całkowite liczby mogą być większe. Obecna aktywna edycja jest wykorzystywana również przez nowe drużyny. Jeżeli jej okres rejestracji był zamknięty, skrypt otwiera go na potrzeby testów.

Regulamin i historia mają nagłówki, długie akapity, listy, cytaty i tabele. Wpisy zawierają ponadto ilustracje, szeroki fragment kodu i celowo długi ciąg bez spacji. To miejsca, gdzie warto szukać poziomego przewijania całej strony, nachodzenia tekstów i przycisków oraz niepoprawnego skalowania obrazów.

Każdy PDF jest przypisany do jednej edycji, żeby usunięcie dokumentu nie psuło listy w innej edycji. Obrazy są współdzielone pomiędzy wpisami, sponsorami i koalicjantami; usunięcie obrazu z publicznej biblioteki może więc pozostawić odwołania do niego w treści.

## Sugerowana kolejność przeklikiwania

1. Bez logowania: aktualności, historia z archiwami, regulamin, zadania/PDF-y, KOALicjA i stopka sponsorów.
2. Administrator: listy i modale wpisów, sponsorów, koalicjantów, szkół, edycji i użytkowników; biblioteka obrazów, dokumenty, lista drużyn i panel RODO z filtrami.
3. Pusty kapitan: otwarcie modala tworzenia drużyny i wybieranie szkoły z dużej tabeli.
4. Pełny kapitan: nagłówek drużyny, edycja nazwy, kod dołączenia i wiersze uczestników.
5. Pełny guardian: przełączanie drużyn, etykiety statusów, podgląd wielostronicowego PDF-a, wybór i podmiana pliku.
6. Pusty guardian i kapitan bez zgód: widoki początkowe, formularze oraz przyciski dodawania.

Sprawdź oba motywy, szerokości około 360, 390, 768, 1024 i 1440 px oraz powiększenie przeglądarki 125%, 150% i 200%. Szczególnie obserwuj menu, modale, tabele Markdown, długie e-maile, selektor drużyn i rzędy przycisków RODO.

Usuwanie samego dokumentu RODO pozostaje wyłączone z powodu znanej kaskady backendowej. Przycisk usuwania uczestnika lub drużyny rzeczywiście usuwa dane. Linki stron sponsorów prowadzą do fikcyjnej domeny `example.test`; służą do sprawdzania wyglądu, nie zewnętrznych witryn.

## Ponowne przygotowanie zestawu

Backend na `localhost:8080`, PostgreSQL w kontenerze `koala_postgres_container`, konto startowe `admin` / `admin`:

```powershell
cd client
node scripts/seed-ui-data.mjs
```

Same wygenerowane grafiki można odświeżyć bez ponownego wypełniania danych:

```powershell
node scripts/seed-ui-data.mjs --refresh-images
```

Ta opcja zachowuje identyfikatory plików oraz wszystkie odnośniki. Po odświeżeniu obrazów wykonaj pełne przeładowanie strony (`Ctrl+F5`).

Skrypt wymaga Node.js z `fetch`, `FormData` i `Blob` oraz dostępu do Dockera. Opcjonalnie można ustawić lokalny adres API przez `KOALA_API_URL`. Używa aktualnego API i kontraktu `contentJson` z `markdownBody`, `description` itd.

Ponowne uruchomienie korzysta z istniejących kont i plików oraz aktualizuje nazwane wpisy, sponsorów i strony testowe. Nie tworzy drugiego kompletu tych samych rekordów. Zmodyfikowane ręcznie wpisy GUI oraz treść regulaminu/historii zostaną ponownie zastąpione tekstem testowym. Istniejących haseł kont nie resetuje. Częściowo zmienione drużyny można nadal oglądać, ale skrypt nie odtwarza każdej ręcznej zmiany ich składu.

Bezpośredni SQL jest używany do odczytu tokenów aktywacji, przygotowania archiwalnych edycji bez zamykania bieżącej, utworzenia koalicjantów bez endpointu POST i ustawienia przykładowych statusów nazw drużyn. Kod i schemat backendu nie są zmieniane, woluminy nie są czyszczone.
