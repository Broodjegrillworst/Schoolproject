using UnityEngine;

namespace Afterglow
{
    /// <summary>One illustrated sprite per actor, so each silhouette reads as a designed character.</summary>
    public static class AfterglowCharacterArt
    {
        public static Transform Hero(Transform parent)
        {
            var root = new GameObject("Mara Vale • Keeper of the Last Bell").transform;
            root.SetParent(parent, false);
            AddSprite(root, AfterglowArt.AuthoredSprite("Character_Mara", 340f), 20);
            root.gameObject.AddComponent<AfterglowLanternSignal>().Initialize();
            return root;
        }

        public static Transform Enemy(Transform parent, EnemyKind kind)
        {
            string name = kind == EnemyKind.Stalker ? "Temple Warden" :
                kind == EnemyKind.RiftHound ? "Root Hound" :
                kind == EnemyKind.HexCaster ? "Moss Seer" : "Stonebound Guardian";
            string art = kind == EnemyKind.Stalker ? "Enemy_Stalker" :
                kind == EnemyKind.RiftHound ? "Enemy_Rifthound" :
                kind == EnemyKind.HexCaster ? "Enemy_Scribe" : "Enemy_Breaker";
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            AddSprite(root, AfterglowArt.AuthoredSprite(art, 360f), 10);
            return root;
        }

        public static Transform Boss(Transform parent, BossStyle style)
        {
            string name = style == BossStyle.Vandal ? "WARDEN HALDEN • THE LAST TOLL" :
                style == BossStyle.CinderMatriarch ? "SERA VOSS • THE ROOT MOTHER" :
                "ASTER NULL • THE CROWNLESS SAINT";
            string art = style == BossStyle.Vandal ? "Boss_Halden" :
                style == BossStyle.CinderMatriarch ? "Boss_Sera" : "Boss_Aster";
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            Transform image = AddSprite(root, AfterglowArt.AuthoredSprite(art, 320f), 12);
            image.localPosition = new Vector3(0f, -.9f, 0f);
            return root;
        }

        private static Transform AddSprite(Transform parent, Sprite sprite, int sortingOrder)
        {
            var image = new GameObject("Illustrated artwork").transform;
            image.SetParent(parent, false);
            var renderer = image.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            if (AfterglowArt.SpriteMaterial != null) renderer.sharedMaterial = AfterglowArt.SpriteMaterial;
            return image;
        }
    }
}
