using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class AutoSetupTool
{
    private const string TilesFolder = "Assets/Assets/Tiles";
    private const string CharacterSpritePath = "Assets/Sprites/Character/character_berie_idle_1.png";
    private const string PlayerMovementTypeName = "PlayerMovement";
    private const int GroundRow = -3;

    [MenuItem("Tools/Videojuego1/1) Configurar suelo y colisiones")]
    public static void SetupGround()
    {
        Grid grid = Object.FindFirstObjectByType<Grid>();
        if (grid == null)
        {
            GameObject gridGO = new GameObject("Grid", typeof(Grid));
            grid = gridGO.GetComponent<Grid>();
        }

        Tilemap tilemap = grid.GetComponentInChildren<Tilemap>();
        if (tilemap == null)
        {
            GameObject tilemapGO = new GameObject("Tilemap", typeof(Tilemap), typeof(TilemapRenderer));
            tilemapGO.transform.SetParent(grid.transform);
            tilemap = tilemapGO.GetComponent<Tilemap>();
        }

        if (tilemap.gameObject.GetComponent<TilemapCollider2D>() == null)
        {
            tilemap.gameObject.AddComponent<TilemapCollider2D>();
        }

        TileBase[] tiles = AssetDatabase.FindAssets("t:TileBase", new[] { TilesFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(p => p)
            .Select(AssetDatabase.LoadAssetAtPath<TileBase>)
            .Where(t => t != null)
            .ToArray();

        if (tiles.Length == 0)
        {
            Debug.LogError($"No se encontraron tiles en {TilesFolder}. Revisa que la ruta sea correcta.");
            return;
        }

        TileBase groundTile = tiles[0];
        for (int x = -12; x <= 12; x++)
        {
            tilemap.SetTile(new Vector3Int(x, GroundRow, 0), groundTile);
        }

        MarkSceneDirty();
        Debug.Log("Suelo pintado (fila y=" + GroundRow + ") y TilemapCollider2D agregado. " +
                   "El tile usado es solo de referencia: si no te gusta como se ve, repintalo a mano con la Tile Palette, la colision seguira funcionando igual.");
    }

    [MenuItem("Tools/Videojuego1/2) Crear jugador con fisica y script")]
    public static void SetupPlayer()
    {
        GameObject existing = GameObject.Find("Player");
        if (existing != null)
        {
            Debug.LogWarning("Ya existe un GameObject 'Player' en la escena, no se creo uno nuevo.");
            Selection.activeGameObject = existing;
            return;
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CharacterSpritePath);
        if (sprite == null)
        {
            Debug.LogError($"No se encontro el sprite en {CharacterSpritePath}");
            return;
        }

        System.Type movementType = FindTypeByName(PlayerMovementTypeName);
        if (movementType == null)
        {
            Debug.LogError("No se encontro la clase PlayerMovement. Espera a que Unity termine de compilar los scripts e intenta de nuevo.");
            return;
        }

        GameObject player = new GameObject("Player");

        SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 1;

        float platformTopY = GroundRow + 1f;
        player.transform.position = new Vector3(0f, platformTopY + sprite.bounds.extents.y, 0f);

        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f;
        rb.freezeRotation = true;

        CapsuleCollider2D col = player.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(sprite.bounds.size.x * 0.6f, sprite.bounds.size.y * 0.9f);

        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform);
        groundCheck.transform.localPosition = new Vector3(0f, -sprite.bounds.extents.y, 0f);

        Component movement = player.AddComponent(movementType);
        SerializedObject so = new SerializedObject(movement);
        so.FindProperty("groundCheck").objectReferenceValue = groundCheck.transform;
        so.FindProperty("groundLayer").intValue = -1; // "Everything": suficiente para probar el salto ahora mismo
        so.ApplyModifiedProperties();

        Selection.activeGameObject = player;
        MarkSceneDirty();
        Debug.Log("Player creado con SpriteRenderer, Rigidbody2D, CapsuleCollider2D, GroundCheck y PlayerMovement ya asignado.");
    }

    [MenuItem("Tools/Videojuego1/3) Ajustar escala del personaje (Pixels Per Unit)")]
    public static void FixCharacterPixelsPerUnit()
    {
        const float targetPPU = 16f;
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Sprites/Character" });
        int changed = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || Mathf.Approximately(importer.spritePixelsPerUnit, targetPPU)) continue;

            importer.spritePixelsPerUnit = targetPPU;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            changed++;
        }

        Debug.Log($"Pixels Per Unit actualizado a {targetPPU} en {changed} sprites del personaje.");
    }

    private static System.Type FindTypeByName(string name)
    {
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = asm.GetType(name);
            if (type != null) return type;
        }

        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in asm.GetTypes())
            {
                if (type.Name == name) return type;
            }
        }

        return null;
    }

    private static void MarkSceneDirty()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
