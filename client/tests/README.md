# Testy frontendu

## Polecenia

- `npm test` — jednorazowe uruchomienie wszystkich testów.
- `npm run test:watch` — tryb obserwowania podczas pracy.
- `npm run test:coverage` — testy wraz z raportem pokrycia w `coverage/`.

## Struktura

- `unit/utils` — czyste funkcje, adaptery danych, autoryzacja i komunikacja HTTP.
- `unit/components` — zachowanie współdzielonych komponentów.
- `screens/public` — publiczne ekrany i przepływy użytkowników drużynowych.
- `screens/admin` — ekrany administracyjne i operacje CRUD.
- `integration` — zachowanie obejmujące kilka modułów lub routing aplikacji.
- `fixtures` — współdzielone dane przykładowe, bez logiki testów.
- `mocks` — serwer MSW i współdzielone handlery HTTP.
- `helpers` — wspólne funkcje renderujące i fabryki danych.

## Dodawanie niezależnego zestawu testów

1. Utwórz plik `NazwaModułu.test.ts` albo `NazwaEkranu.test.tsx` w odpowiednim folderze.
2. Dla komponentów używaj `renderWithRouter`, który zapewnia router oraz `LoadingProvider`.
3. Odpowiedzi API definiuj handlerami MSW. Handler specyficzny dla jednego pliku dodaj przez
   `server.use(...)`; współdzielony handler umieść w `mocks/handlers.ts`, gdy taki plik stanie się
   potrzebny.
4. Testuj zachowanie widoczne dla użytkownika, wysyłane żądania i reakcje na błędy. Nie testuj
   wewnętrznego stanu Reacta ani dokładnych zestawów klas Tailwinda.
5. Przed commitem uruchom `npm test`, `npm run lint` oraz `npm run typecheck`.

Każdy kolejny commit może dodawać jeden zamknięty zestaw scenariuszy wraz z fixture'ami i
handlerami wymaganymi wyłącznie przez ten zestaw.
