# VESPER: Echoes Below

Een donkere 2D-metroidvania met vijf regio's. De gebieden zijn aparte Unity-scènes die via wortelpoorten verbonden zijn. Twee vaardigheidspoorten geven alternatieve routes terug door de wereld; de kaart toont de regioverbindingen en de kamers die je hebt verkend.

## Openen in Unity

1. Open `UnityProject` met Unity 6.0.6.5f1.
2. Open `Assets/Ruimterobot/Scenes/VesperMenu.unity`.
3. Druk op Play.
4. Bouw de scènes opnieuw via **Vesper > Herbouw de vijf verbonden gebieden**.
5. Exporteer voor Windows via **Vesper > Herbouw en exporteer voor Windows**.

## Besturing

| Actie | Toetsen |
|---|---|
| Bewegen | A/D of pijltjes |
| Springen | Spatie, W of omhoog |
| Dash | Shift of K |
| Melee-combo | J |
| Echo Wave | L, na het vrijspelen |
| Poort / resonantie-dial | E |
| Wereldkaart | M |
| Inventaris | I |
| Skill tree | Tab |
| Pauze en instellingen | Esc |

## Gebieden en vaardigheden

- **Hollow Grove** — dubbele sprong
- **Drowned Archive** — muursprong
- **Ember Veins** — luchtdash
- **Moonlit Canopy** — superdash
- **Rift Heart** — Echo Wave

De hoofdroute gebruikt poorten, vaardigheden en verslagen Wardens. Terugkeren is mogelijk. Een Wall Jump-route verbindt Hollow Grove met Moonlit Canopy; een Air Dash-route verbindt Drowned Archive met Rift Heart.

## Verkenning en voortgang

De M-kaart toont de vijf regio's, alternatieve poorten, ontdekte kamers en de verborgen vault-route. Iedere regio bevat een eigen driedelige resonantiecode. Volg de volgorde en voltooi die binnen tien seconden om de vault en de beloning te openen. Checkpoints, kamers, vaardigheidsvoortgang, bazen, puzzeloplossingen en verzamelde beloningen worden lokaal opgeslagen.

## Inhoud

Vijf afzonderlijke regio's met eigen platformroutes, hoge zijpaden, moving platforms, instortende vloeren, getimede lasers, draaiende gevaren en vallend puin. Er zijn tien vijandgedragingen, een miniboss per regio en vijf regio-eindbazen. Parallax, omgevingsmist, lichtaccenten, particles, camera-effecten, combat audio en object pooling zijn aanwezig.

De achtergronden en enkele effecten worden procedureel gemaakt uit Unity-sprites en polygonen; dit is geen handgeschilderde AAA-art. De Windows-export wordt gebouwd vanuit het Unity-project. De game is niet handmatig volledig in Play Mode doorlopen.
