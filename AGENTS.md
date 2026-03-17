# Cel projektu

Celem projektu jest zrobienie systemu sterowania planszami interaktywnymi. 
Ma być aplikacja uruchomiona na komputerze, która wyśle sygnał startu do wszystkich podłączonych plansz wraz z określoną sekwencją gry.
Następnie każda plansza z osobna będzie testować szybkość reakcji gracza w odebranej wcześniej sekwencji.
Gdy cała sekwencja zostanie ukończona przez gracza wyniki mają być przesłane spowrotem do komputera i tam wyświetlone.

Aplikacja obsługująca plansze będzie pracowała pod kontrolą linuxa na Raspberry Pi i tam za pomocą fizycznych złącz sterowałą planszą.

# Narzędzia 
Masz dostępne narzędzia takie jak: 
- git - system kontroli wersji
- github - miejsce, gdzie projekt jest przechowywany
- VS Code oraz Visual Studio 2026 - podstawowe IDE
- build.ps1 służy do budowania aplikacji całkowicie od podstaw. Łatwiej może być korzystać z poleceń `dotnet build`, `dotnet test`.
- Aplikacja zawiera testy End2End, które muszą być wywoływane poprzez `build.ps1`. Wymagają one odpowiedniego użycia dockera. `build.ps1` zawsze używa się z odpowiednim celem wykonania, np. `Compile`, `E2ETests`, `UnitTests`.

# Środowisko
Rozwój aplikacji normalnie robiony jest na najnowszym Windows 11. Natomiast sama aplikacja musi pracować zarówno na Windows 11 jak i na Rapsberry Pi pod kontrolą linuxa.

# Otoczenie biznesowe
Aplikacja aktualnie to MVP (Minimal Valuable Product), który ma zostać dostarczony w miarę tanio i szybko, po to aby zobaczyć, czy jest popyt na podobne rozwiazanie.

Jednocześnie chcemy zachować przejrzystą strukturę, aby można było się tu łatwo odnaleźć po dłuższym czasie. 

# Teoria i praktytki dostarczania
Projekt jest robiony w .Net z użyciem języka C#. 
Pipelineny są na Githubie i jest włączone podstawowe CI/CD. 

# Struktura projektu
Rozwiązanie skłąda się z następujących projektów .Net
- build\_build.csproj - projekt, który odpowiada za budowanie całego rozwiązania lokalnie i w CI/CD.
- templates\ZtrBoardGame.Console\ZtrBoardGame.Console.csproj - główny projekt rozwiązania, gdzie znajduje się logika aplikacyjna i domenowa
- templates\ZtrBoardGame.Console.Tests\ZtrBoardGame.Console.Tests.csproj - projekt z testami jednostkowymi
- templates\ZtrBoardGame.Configuration.Shared\ZtrBoardGame.Configuration.Shared.csproj - projekt wspóldzielony nawet z _build.csproj. Zawiera rzeczy, które muszą być uwspólnione. Aktualnie tylko i wyłącznie reprezentacje ustawień.


# Zależności
Aktualnie jesteśmy zależni od zespołu projektowego elektroniki, z którą nasza aplikacja będzie musiała się łączyć pracując na Raspberry Pi. Na razie szczegóły elektroniki nie są znane.

// @Droid-Review | Rule: 04-yagni | Confidence: 10/10
// Problem:
//    The file contains project guidelines and not source code. Therefore, no YAGNI violations are expected.
// Suggestion:
//    No changes needed. The file adheres to the YAGNI principle by focusing on current project needs.