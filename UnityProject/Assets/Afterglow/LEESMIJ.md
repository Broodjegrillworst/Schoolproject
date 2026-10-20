# AFTERGLOW: THE ROOTBOUND BELL

Een 2D dark-fantasy-actieplatformspel over Mara Vale, een menselijke klokbewaarster die door een vergeten oerwoudrijk vecht om haar zus Elin te vinden.

## Openen

1. Open het Unity-project `UnityProject` in Unity 6000.6.5f1.
2. Open `Assets/Afterglow/Scenes/AfterglowMenu.unity`.
3. Druk op Play en kies **Nieuw avontuur**.

Het hoofdstukverhaal verschijnt kort over de speelwereld; de besturing is vanaf de eerste seconde beschikbaar.

## Besturing

- **A / D** — naar links/rechts bewegen
- **Spatie** — springen; vasthouden geeft een hogere sprong
- **Linkermuisknop** — Oathroot-relictkanon vuren; vasthouden laadt een krachtig schot
- **Rechtermuisknop** — Reliekstoot; onderbreekt vijanden dichtbij
- **Shift** — dash; dash naar een met Q gemarkeerd doel of in je bewegingsrichting
- **Q** — Pulsmerk op een rebound-doel; schieten op dit doel laadt extra wortelhartenergie
- **Shift bij inslag** — perfecte rebound tijdens het korte timingvenster
- **E** — een herinnering onderzoeken of een vrijgespeelde worteldoorgang gebruiken
- **R** — Wortelhart-ultiem ontketenen zodra de lading 100% is
- **Esc** — pauzemenu • **Tab** — routekaart • **M** — wereldkaart • **C** — personage

## Verhaal en gebieden

1. **De Vergroeide Schrijn** — de woudreus heeft het belfort en de laatste schuilplaats opgeslokt.
2. **De Verzonken Ark** — een tempelboot ligt vast in het overstroomde ravijn, onder een verborgen waterval.
3. **Het Wortelhart** — een wortelkathedraal en godmachine houden het oerwoud gevangen.

Mara is een menselijke klokbewaarster met een rode familiesjaal en een erfkanon van zwart ijzer, messing en een levende blauwe kristalkern. Pulsschoten, markeringen en rebounds voeden het Wortelhart. De drie zegelhouders schieten eigen projectielpatronen die in latere fasen sneller en dichter worden; laden, positioneren en rebounden geven verschillende manieren om ze te counteren. Vijanden patrouilleren, reageren op zicht en wapengeluid, vechten en doorzoeken de laatst bekende plek van Mara. De drie herinneringen versterken Oathroot achtereenvolgens met een sterker kristal, een doorborend schot en extra kracht op geladen vuur. Elk verborgen herinneringsgebied opent ook een worteldoorgang naar een later deel van het hoofdstuk. De omgevingskunst is eigen SVG-vectorillustratie; de beeldbronnen staan in `Tools/AfterglowArtSource/source/overrides` en worden met `render.js` naar `Assets/Afterglow/Resources/Art` gerasterd. Het zijn geen geschilderde bitmap-texturen.

## Windows-build

Kies in Unity **Afterglow → Build Windows Release**. De uitvoer staat in `Builds/Afterglow_Windows/Afterglow.exe`. De runtime-scènes worden opgebouwd door `LevelManager`; de bestaande scene-entry `AfterglowBootstrap` blijft daarvoor als compatibele afgeleide component bestaan.

De menucredits tonen **Made by Jaiden Asruf**.
