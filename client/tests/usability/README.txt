PRZYGOTOWANIE TESTÓW UŻYTECZNOŚCI KOALA

Wymagania: Node.js 22 lub nowszy, Docker CLI, cztery lokalne kontenery KOALA
(backend, PostgreSQL, Redis, Mailpit). Frontend uruchamia moderator samodzielnie.
Polecenia poniżej uruchamiaj z głównego folderu repozytorium.

1. RESET I PEŁNE PRZYGOTOWANIE

node client/tests/usability/prepare.mjs --reset

UWAGA: to celowo usuwa obecną bazę i pliki aplikacji. Skrypt sprawdza nazwy
kontenerów, sieć i montowania, odtwarza wyłącznie koala_database_volume oraz
koala_backend_volume, uruchamia ponownie Redis i czyści lokalny Mailpit.
Zachowuje obrazy, konfigurację kontenerów i opublikowane porty. Nie buduje obrazów
i nie uruchamia Vite. Przed resetem upewnij się, że backend ma właściwą wersję.
Jeżeli brak konfiguracji konta startowego, ustawia ją tylko w kontenerze backendu.
Nie zmienia plików backendu ani migracji. W używanym układzie przyjmuje db/user
oraz lokalny Mailpit na localhost:8025. Nie jest skryptem do produkcji.

Bez --reset skrypt dopuszcza seed tylko w pustej bazie i bez istniejącego manifestu.
Po przerwanym seedzie wykonaj --reset. Nie usuwa ręcznie zmian uczestników przy
zwykłej weryfikacji ani ponownym generowaniu instrukcji.

2. PLIKI I DANE

scenarios.json     - jedyna lista poleceń, przeniesiona z pliku TXT w katalogu głównym.
prepare.mjs        - tworzenie danych, kart, weryfikacja i próba operacji API.
check-scenarios.mjs - sprawdzenie wszystkich kart przez API i przywrócenie danych.
administrator-stacjonarnie.md - kartka administratora na test stacjonarny.
administrator-zdalnie.md      - kartka administratora na test zdalny.
edytor-stacjonarnie.md        - kartka edytora na test stacjonarny.
recenzent-stacjonarnie.md     - kartka recenzenta na test stacjonarny.
recenzent-zdalnie.md          - kartka recenzenta na test zdalny.
dane-zapasowe.md             - rezerwa wszystkich trzech ról, tylko dla moderatorów.
assets/            - gotowe PNG, PDF i CSV do przekazania uczestnikom.
seed.local.json    - identyfikatory, konta, terminy, powiązania oraz dane do kart.

Identyfikatory Axx/Exx/Rxx są trwałymi kluczami poleceń, a nie ich pozycjami w JSON.
Możesz zmienić tekst lub kolejność istniejących poleceń. Dodanie nowego wymaga też
przypisania danych w bindCards(). Skrypt odrzuci polecenie bez przypisanych danych.
Wygenerowane instrukcje, manifest i assety są robocze i ignorowane przez Git.
Skrypt, lista poleceń i ta instrukcja pozostają wersjonowane.

Daty są liczone od momentu seedowania. W dokumentach zapisano konkretne terminy
w strefie Europe/Warsaw. Etapy do ocen mają termin w przeszłości; etapy do edycji
mają początek za 30 dni i koniec za 60 dni. Przed nową rundą po dłuższej przerwie
wygeneruj świeże dane. --docs-only nie przesuwa terminów w bazie.

3. GENEROWANIE SAMYCH INSTRUKCJI

node client/tests/usability/prepare.mjs --docs-only

Nie wymaga działającego Dockera/API. Odczytuje scenarios.json oraz seed.local.json.
Każde polecenie w dokumentach ma pod spodem dokładne nazwy/dane do użycia.
Na każdej z pięciu kartek polecenie jest zapisane raz, a pod nim znajdują się
tylko właściwe dla tej osoby dane w punktorach. Kartki nie zawierają wstępu,
osobnych sekcji sesji ani wzmianki o rezerwie. Zapas jest w osobnym pliku dla nas.
Nie ma rzeczywistych danych osobowych, adresów ani dokumentów zgód.

4. KONTA

Adresy pięciu uczestników odczytywane są z .env w tym folderze. Skopiuj
.env.example do .env i wpisz prywatne adresy w pięciu zmiennych. Bez wpisu
skrypt używa fikcyjnego adresu @koala.test. Nie wpisuj prywatnych adresów
do plików .mjs, scenarios.json ani tego README.

Przy nowym seedzie konta od razu powstaną z adresami z .env. Aby podmienić
adresy już istniejących kont bez resetowania danych, po edycji .env uruchom:

node client/tests/usability/prepare.mjs --sync-emails

To zachowuje ID, role, hasła i przydziały; aktualizuje maile kont i profili,
manifest, powiązania w kartach oraz sprawdza logowanie każdej z pięciu osób.
Nie wysyła zaproszeń. Po zmianie adresów zaloguj się w przeglądarce ponownie.
--docs-only odrzuci niezgodność konfiguracji z manifestem zamiast drukować
adres, którym nie da się zalogować. Krok logowania ma wyłącznie e-mail i hasło.
.env, kartki, manifest i raporty są ignorowane przez Git; .env.example zawiera
wyłącznie fikcyjne przykłady i może być wersjonowany.

Domyślne, fikcyjne adresy przed uzupełnieniem .env:

Stacjonarnie: admin.lokalna@koala.test, edytor.lokalna@koala.test,
              recenzent.lokalna@koala.test.
Zdalnie:      admin.zdalna@koala.test, recenzent.zdalna@koala.test.
Rezerwa:      admin.rezerwa@koala.test, edytor.rezerwa@koala.test,
              recenzent.rezerwa@koala.test.
Hasło dla wszystkich kont tej próby: KoalaProba2026!
Techniczne konto moderatora: moderator@koala.test (to samo hasło).

Pozostałe konta to aktorzy danych: kapitanowie, opiekunowie, partnerzy oceny,
zastępcy i wskazane konta do edycji/usuwania. Nie są dodatkowymi uczestnikami.
Zapas jest jeden na scenariusz/rolę, wspólny dla rund lokalnej i zdalnej.
Karty rezerwowe mają osobne obiekty i konta; po wykorzystaniu rezerwy nie jest
ona automatycznie resetowana. Zaproszenia trafiają wyłącznie do lokalnego Mailpita.
Setup czyści zużyte wiadomości rejestracyjne, by nie zaśmiecały skrzynki.
Oddzielne osoby muszą używać oddzielnych profili przeglądarki, nie tylko kart.

5. NIEZALEŻNOŚĆ SCENARIUSZY I JEDYNY GLOBALNY WARUNEK

Tworzenie i edycja używają różnych obiektów. Zadania do oceny, sprawdzarki,
przydziału, zastępstwa, zamknięcia i rankingów mają osobne etapy. Publikowany
ranking jest już wygenerowany. Zastępstwo ma gotową parę i częściowe oceny.
Recenzent ma osobno cztery nieocenione przykłady i piąty z konfliktem.
Wnioski RODO do przeglądu i rozpatrzenia dotyczą różnych osób/drużyn.
Dokument do usunięcia nie jest tym dodawanym przez uczestnika. Tak samo obraz.
Regulamin/historia pozostają wspólnymi stronami, ale rezerwa ma własne sekcje.
Nie wykonuj równoczesnej edycji tego samego regulaminu/historii na dwóch kontach.

Backend pozwala na tylko jedną aktywną edycję. Dlatego PRZED A10 moderator:

node client/tests/usability/prepare.mjs --prepare A10 --variant lokalna

Dla administratora zdalnego użyj --variant zdalna, dla zapasu --variant rezerwa.
To przygotowuje wyłącznie globalny kontekst; nie usuwa odpowiedzi ani kont.
PO A10 (także jeśli pominięto/nie ukończono polecenia), PRZED dalszymi testami:

node client/tests/usability/prepare.mjs --prepare A11 --variant lokalna

Przywraca edycję pokazową. Wykonaj to również przed rozpoczęciem sesji edytora.
Przygotowanie restartuje backend; zaczekaj kilka sekund i odśwież stronę.
Nie uruchamiaj tego w trakcie równoległej pracy innych uczestników. A10 zaplanuj
poza ich pracą. Nie zamykaj edycji pokazowej przyciskiem „Zakończ edycję”:
backend usuwa wówczas dane konkursowe. Zakończenie etapu w A23 jest bezpieczne,
bo korzysta z osobnego etapu przygotowanego wyłącznie do tego zadania.

To jedyny scenariusz wymagający przełączenia kontekstu przez moderatora.
Odseparowanie nie oznacza automatycznego odtwarzania obiektu po udanej operacji.
Jeśli uczestnik zmieni/usunie niewłaściwy obiekt, użyj odpowiedniej karty zapasowej.

6. WERYFIKACJA

node client/tests/usability/check-scenarios.mjs

Pełna kontrola kart: 32 polecenia administratora w trzech wariantach, 7 poleceń
recenzenta w trzech wariantach i 13 poleceń edytora w dwóch wariantach, łącznie
143 wykonania (91 dla pięciu uczestników i 52 dla rezerwy). Działa na kontach
uczestników, korzysta z dokładnych tekstów, plików i terminów kart; terminy
interpretuje w Europe/Warsaw. Sprawdza nie tylko status HTTP, ale też zapisane
dane, publikację/ukrycie treści i skutki usunięcia lub zastępstwa.

Przed operacjami zachowuje bazę, pliki i zawartość Redis. W bloku finally
przywraca ten sam stan, a nie ponownie generowany zestaw z innymi identyfikatorami.
Porównuje sumy kontrolne wszystkich tabel account/core/cms i plików. Tymczasowo
zatrzymuje backend na czas przywracania. Nie uruchamiaj podczas sesji uczestnika.
Oczekuje pustego Mailpita i po kontroli usuwa tylko powstałe w tej próbie maile.

Wyniki: api-scenarios.local.md (tabela scenariuszy) i api-scenarios.local.json
(scenariusze, statusy HTTP i wysłane payloady; hasła są zamaskowane). To pliki
robocze ignorowane przez Git. Kontrola nie potwierdza działania GUI: np. A04
polega tylko na znalezieniu menu zmiany hasła i nie zmienia hasła przez API.

Gdyby proces przerwano, pozostaje rollback.local.json. Przywróć go poleceniem:

node client/tests/usability/check-scenarios.mjs --restore

Nie kasuj tej kopii przed przywróceniem. Jest automatycznie usuwana po udanej
kontroli sum i uruchomieniu backendu. To kopia lokalna wyłącznie danych próbnych.

node client/tests/usability/prepare.mjs --verify

Sprawdza przez API gotowość bazowego zestawu: przydziały, braki ocen, RODO,
ukryte rankingi, profile i pliki. Nie zmienia danych konkursowych (tworzy sesje).
Po wykonaniu zadań przez uczestników część warunków bazowych przestanie zachodzić
i --verify zgłosi różnicę: nie musi ona oznaczać błędu aplikacji.

node client/tests/usability/prepare.mjs --exercise

Wykonuje rzeczywiste operacje na REZERWIE: zaproszenie, zmianę ról i usunięcie
konta, tworzenie/edycję/aktywację edycji i etapów, tworzenie/edycję/usunięcie zadań,
szkoły i import CSV, zatwierdzenie RODO, sprawdzarkę, przydział, zastępstwo,
ocenianie, zamknięcie etapu, generowanie i publikację rankingu oraz operacje
edytora na wpisach, stronach, sponsorach, profilach i plikach. Sprawdza także,
że dotychczasowe oceny pozostają przy poprzednim recenzencie i blokada konfliktu
działa. Rezerwa zostaje zużyta. Po tej próbie wykonaj --reset, aby oddać uczestnikom
świeże dane. --exercise nie jest testem GUI ani pomiarem użyteczności.

Skrypt używa API do tworzenia kont, szkół, edycji, etapów, zadań, kluczy, zgód,
plików i treści. SQL służy do odczytu tokenów z lokalnej poczty/rejestracji,
ustawienia gotowych odpowiedzi/ocen oraz stanów historycznych. Celowo nie
udaje, że tworzenie historycznych danych SQL testuje przepływ przesyłania
odpowiedzi przez kapitana. --exercise weryfikuje operacje użytkowników przez API.
