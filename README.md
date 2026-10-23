# WebStore — projekt ZPI

Sprint 1 obejmuje model sklepu, zapis przez EF Core do SQL Server, pięć usług biznesowych, testy i uruchamialny szkielet React. Operacje biznesowe są dostępne przez interfejsy usług; ekrany ich obsługi i endpointy API będą przedmiotem kolejnych iteracji.

## Wymagania środowiskowe

- .NET SDK 10, wspólny target `net10.0`. Sprawdzono SDK 10.0.402.
- SQL Server Express LocalDB na Windows albo dostępny SQL Server. Sprawdzono LocalDB 2025 (17.0.1000.7).
- Node.js obsługiwany przez Vite 7: co najmniej 20.19 lub 22.12 w odpowiednich liniach wydań; sprawdzono Node 26.5.1 i npm 11.17.0.
- Git. Zalecany edytor z PDF: Visual Studio Code z rozszerzeniami C#, SQL Server (mssql) i Git Graph.

W tej przestrzeni roboczej SDK zainstalowano w sąsiednim katalogu `.tools/dotnet`. Aby użyć go w PowerShell, uruchom w katalogu repozytorium:

```powershell
. .\scripts\Use-LocalSdk.ps1
```

Skrypt zmienia środowisko bieżącej sesji. Jeśli lokalnego SDK nie ma, używane jest SDK dostępne w systemie. Wersje zależności .NET znajdują się w `Directory.Packages.props`, a zależności klienta w `package-lock.json`.

## Przygotowanie i uruchomienie

Wszystkie poniższe polecenia wykonuj w katalogu repozytorium.

```powershell
git switch mateusz_marcinkowski
dotnet restore WebStore.sln
dotnet tool restore
npm --prefix WebStore.Web/ClientApp ci --no-audit --no-fund
dotnet build WebStore.sln
dotnet ef database update --project WebStore.DAL --startup-project WebStore.Web
dotnet run --project WebStore.Web --no-build
```

Aplikacja uruchamia się pod adresem [localhost:5080](http://localhost:5080). Budowanie projektu Web tworzy pliki React w `WebStore.Web/wwwroot`. Baza aplikacji jest tworzona migracją; uruchomienie hosta nie wykonuje migracji automatycznie.

Domyślne połączenie to SQL Server `(localdb)\MSSQLLocalDB`, baza `WebStore`, z uwierzytelnianiem Windows. LocalDB może uruchomić instancję automatycznie przy pierwszym połączeniu. Narzędzie `SqlLocalDB.exe` znajduje się standardowo w `C:\Program Files\Microsoft SQL Server\170\Tools\Binn` dla wersji 2025; jego polecenia `info` i `start MSSQLLocalDB` pozwalają sprawdzić instancję.

Inny serwer lub bazę skonfiguruj zmienną środowiskową `ConnectionStrings__WebStore`. Narzędzie migracji również odczytuje tę zmienną. Dla lokalnego środowiska można użyć ignorowanego przez Git `WebStore.Web/appsettings.Local.json` z sekcją `ConnectionStrings`. Sekrety nie należą do repozytorium.

Do pracy nad klientem z odświeżaniem zmian:

```powershell
npm --prefix WebStore.Web/ClientApp run dev
```

Serwer Vite działa na [127.0.0.1:5173](http://127.0.0.1:5173). W tym sprincie klient jest szkieletem i nie wywołuje API biznesowego.

## Testy

```powershell
dotnet test WebStore.sln
dotnet test WebStore.Tests --filter UpdateProductTest
dotnet test WebStore.Tests --filter FullyQualifiedName~ProductServiceUnitTests
```

Testy usług korzystają z osobnych baz EF Core InMemory i kontrolowanych danych: dostawca, kategoria, dwa produkty, klienci, sklep i pracownik. Każdy test ma własny kontener DI i bazę; AutoMapper, logowanie i Identity są rzeczywiście konfigurowane.

Testy SQL Server są oznaczone kategorią `SqlServer`. Bez poniższej zmiennej są jawnie pomijane, a nie traktowane jako zaliczone. Aby wykonać cały zestaw na LocalDB:

```powershell
$env:WEBSTORE_SQL_TEST_CONNECTION = 'Server=(localdb)\MSSQLLocalDB;Integrated Security=true;Encrypt=true;TrustServerCertificate=true'
dotnet test WebStore.sln
```

Każdy test SQL tworzy przez migrację własną bazę `WebStore_Test_` z losowym identyfikatorem i usuwa ją po zakończeniu. Testy nie usuwają bazy aplikacji `WebStore`. Konto musi mieć uprawnienia do tworzenia i usuwania tych baz testowych. Sprawdzane są między innymi TPH, relacje, klucze obce, role adresów, unikatowość i brak kaskadowego usuwania.

Weryfikacja programisty 23.10.2026: **75 testów zaliczonych, 0 niepowodzeń, 0 pominiętych** — 67 na InMemory i 8 na rzeczywistym SQL Server. Kompilacja zakończyła się bez ostrzeżeń i błędów. Kontrola npm nie wykazała podatności w zainstalowanych zależnościach.

## Publikacja

```powershell
dotnet publish WebStore.Web -c Release -o .artifacts/publish
dotnet .artifacts/publish/WebStore.Web.dll --contentRoot "$PWD/.artifacts/publish" --urls http://127.0.0.1:5080
```

Sprawdzono start opublikowanej aplikacji i załadowanie React w przeglądarce. Katalogi `bin`, `obj`, `node_modules`, `wwwroot` oraz `.artifacts` są wynikami budowania i nie są wersjonowane.

## Warstwy i usługi

| Projekt | Odpowiedzialność |
| --- | --- |
| WebStore.Model | Encje, hierarchia User/Customer/Supplier/StationaryStoreEmployee i typ roli adresu. |
| WebStore.DAL | ApplicationDbContext, konfiguracja SQL Server i Identity, relacje oraz migracja InitialWebStore. |
| WebStore.ViewModels | Wejścia i wyniki operacji w katalogu VM; walidacja wymaganych pól. |
| WebStore.Services | BaseService, AutoMapper, DI, ProductService, OrderService, InvoiceService, StoreService i AddressService. |
| WebStore.Web | Host ASP.NET Core i szkielet klienta React. |
| WebStore.Tests | Testy kontraktów i funkcjonalności usług oraz odrębne testy SQL Server. |

Każda usługa ma interfejs i implementację oraz operacje dodania/aktualizacji, pojedynczego odczytu i odczytu listy z opcjonalnym predykatem. Brak pojedynczego wyniku daje `null`, a brak wyników listy pustą kolekcję. Aktualizacja nieistniejącego ID zgłasza `KeyNotFoundException`; błędne dane `ArgumentException`, a brak obowiązkowego argumentu `ArgumentNullException`. Błędy są logowane i przekazywane wywołującemu.

Zamówienie wylicza kwotę z cen produktów przy zapisie, a późniejsza zmiana ceny produktu nie zmienia zapisanej kwoty. Faktura może obejmować wiele zamówień jednego klienta; zamówienie objęte fakturą jest chronione przed edycją. Ilość w ProductVm sumuje zapisy ProductStock. Adres ma jednego właściciela i rolę; klient ma najwyżej jeden adres rozliczeniowy i jeden dostawy. Operacje usuwania, rezerwacja stanów i obsługa płatności nie wchodzą w ten sprint.

## AutoMapper

Użyto AutoMapper 15.1.1, wersji poprawiającej zgłoszenie [GHSA-rvv3-g6hj-g44x](https://github.com/advisories/GHSA-rvv3-g6hj-g44x). Biblioteka umożliwia konfigurację klucza przez `AutoMapper__LicenseKey`; klucz pozostaje poza repozytorium. Bez klucza biblioteka loguje komunikat licencyjny; jej dokumentacja opisuje [konfigurację i warunki licencjonowania](https://docs.automapper.io/en/stable/License-configuration.html). Wykonane lokalnie testy nie stanowią potwierdzenia skonfigurowania licencji.

## Status procesu

User stories wersji 1.0 zostały zaakceptowane przez Lidera 23.10.2026. Wyniki powyżej pochodzą ze sprawdzeń programisty. Etap recenzenta rozpocznie się po osobnej akceptacji przekazanego kodu przez Lidera.
