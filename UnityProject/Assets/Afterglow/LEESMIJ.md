# AFTERGLOW: THE ROOTBOUND BELL

Een 2D dark-fantasy-actieplatformspel over Mara Vale, een menselijke klokbewaarster die door een vergeten oerwoudrijk vecht om haar zus Elin te vinden.

## Openen

1. Open het Unity-project `C:\Users\Jaysk\Gamedevelopment junior` in Unity 6000.6.5f1.
2. Open `Assets/Afterglow/Scenes/AfterglowMenu.unity`.
3. Druk op Play en kies **Nieuw avontuur**.

Het hoofdstukverhaal verschijnt kort over de speelwereld; de besturing is vanaf de eerste seconde beschikbaar.

## Besturing

- **A / D** — naar links/rechts bewegen
- **Spatie** — springen; vasthouden geeft een hogere sprong
- **Linkermuisknop** — Oathroot-relictkanon vuren; vasthouden laadt een krachtig schot
- **Rechtermuisknop** — Reliekstoot; onderbreekt vijanden dichtbij. Bij een vol Wortelhart ontketent RMB de ultieme puls.
- **E** — een herinnering onderzoeken, een worteldoorgang gebruiken of een gebarsten zegel openen
- **Esc** — pauzemenu

De optionele **Shift-dash** is een extra ontwijking; je hoeft geen doel te markeren of een rebound uit te voeren om te vechten of een baas te verslaan.

## Verhaal en gebieden

1. **De Vergroeide Schrijn** — de woudreus heeft het belfort en de laatste schuilplaats opgeslokt.
2. **De Verzonken Ark** — een tempelboot ligt vast in het overstroomde ravijn, onder een verborgen waterval.
3. **Het Wortelhart** — een wortelkathedraal en godmachine houden het oerwoud gevangen.

Mara is een menselijke klokbewaarster met een rode familiesjaal, koperen lantaarn en erfkanon van zwart ijzer, messing en een levende blauwe kristalkern. Elke eindbaas heeft drie fases met een eigen aanvalspatroon en een korte opening: blijf bewegen, herken de waarschuwing en raak het blootgelegde hart. Een geladen schot doet meer schade, maar gewone pulsschoten blijven effectief. De drie herinneringen versterken Oathroot en openen elk een verborgen worteldoorgang. De omgevingskunst is eigen SVG-vectorillustratie; de beeldbronnen staan in `Tools/AfterglowArtSource/source/overrides` en worden met `render.js` naar `Assets/Afterglow/Resources/Art` gerasterd.

## Windows-build

Kies in Unity **Afterglow → Build Windows Release**. De uitvoer staat in `Builds/Afterglow_Windows/Afterglow.exe`. De runtime-scènes worden opgebouwd door `LevelManager`; de bestaande scene-entry `AfterglowBootstrap` blijft daarvoor als compatibele afgeleide component bestaan.

De menucredits tonen **Made by Jaiden Asruf**.
