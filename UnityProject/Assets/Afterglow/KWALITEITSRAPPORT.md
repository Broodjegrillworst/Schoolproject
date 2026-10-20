# AFTERGLOW — revisie schooldemo

## Richting en inhoud

De campagne is benoemd en aangekleed als een verloren junglefantasy-rijk: de Vergroeide Schrijn, de Verzonken Ark en het Wortelhart. De achtergronden gebruiken reuzenbomen, overwoekerde ruïnes, wortelbruggen en een nieuwe watervaltempel als landmark. Mara blijft een menselijk personage met een eigen familiesjaal en lantaarn.

Het herkenbare wapen is de Oathroot: een erfkanon van donker ijzer, messing en een blauwe kristalkern. De nieuwe `PlayerCombat` neemt muisvuur, laden, secundaire vaardigheid en ultiem input voor zijn rekening; `WeaponSystem` regelt vuursnelheid, projectielkracht en drie upgrades die vrijkomen met de herinneringen. Schoten geven mondingslicht, impactfeedback, geluid en cameratrilling. Vijanden hebben nu een afzonderlijke `EnemyAI` met patrouille-, alarm-, gevechts- en zoektoestand. Zij reageren op Mara's positie, wapengeluid en treffers. De `BossController` bouwt elk gevecht op uit drie fases; unieke projectielpatronen versnellen en verdichten, en een perfecte rebound kan de aanval counteren. `UIManager` verzorgt menu, HUD, journal, pauze, bossintro's en credits. `LevelManager` bouwt de drie hoofdstukken; elk bevat een verborgen herinneringskamer en een worteldoorgang naar een later punt. Herinneringen verbeteren het wapen permanent.

## Besturing

- A/D bewegen; Spatie springen (vasthouden geeft een hogere sprong).
- Linkermuisknop vuurt; vasthouden laadt. Rechtermuisknop gebruikt de Reliekstoot.
- Q markeert een doel; Shift dash of begint een rebound. Shift op de inslag geeft een perfecte rebound.
- E onderzoekt herinneringen en gebruikt de vrijgespeelde worteldoorgang; R gebruikt het Wortelhart zodra dat vol is.
- Esc pauze; Tab routekaart; M wereldkaart; C personage.

## Art en beperkingen

De campagne-illustraties zijn eigen, handgemaakte SVG-vectorbronnen en worden in Unity als PNG-texturen gebruikt. De art is geen geschilderde bitmap-set. De nieuwe watervaltempel is een extra herkenbaar achtergrondmoment. Dit vervangt nog niet elk bestaand platformsegment en garandeert geen commerciële of Steam-presentatiekwaliteit. De bijgevoegde voorbeeldafbeelding is alleen als algemene sfeerreferentie gebruikt.

## Controle

Unity bouwt de scènes programmatisch vanuit `LevelManager` (de bestaande scene-entry `AfterglowBootstrap` leidt hiervan af) en exporteert de Windows-demo naar `Builds/Afterglow_Windows`. De gameplay-systemen zijn opnieuw ingedeeld in afzonderlijke scripts. Een succesvolle Unity-build controleert compilatie en export, maar is geen volledige handmatige gameplaytest. De drie hoofdstukken en alle vijand- en baaspatronen zijn niet volledig uitgespeeld; de campagnekunst blijft vectorillustratie in plaats van geschilderde bitmapkunst.

De menu- en eindcredits blijven **Made by Jaiden Asruf** tonen.
