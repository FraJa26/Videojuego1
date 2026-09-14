using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class AutoSetupTool
{
    private const string TilesFolder = "Assets/Assets/Tiles";
    private const string CharacterSpritePath = "Assets/Sprites/Character/character_berie_idle_1.png";
    private const string PlayerMovementTypeName = "PlayerMovement";
    private const int GroundRow = -3;
    private const int LevelEndX = 30;

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

        TileBase groundTile = GetGroundTile();
        if (groundTile == null) return;

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
        player.tag = "Player";

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

    [MenuItem("Tools/Videojuego1/3) Ajustar escala de sprites (Pixels Per Unit)")]
    public static void FixPixelsPerUnit()
    {
        const float targetPPU = 16f;
        string[] folders = { "Assets/Sprites/Character", "Assets/Sprites/Items", "Assets/Sprites/Obstacles" };
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", folders);
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

        Debug.Log($"Pixels Per Unit actualizado a {targetPPU} en {changed} sprites (personaje, items y obstaculos).");
    }

    [MenuItem("Tools/Videojuego1/4) Ampliar nivel (hueco, plataforma y tramo final)")]
    public static void ExtendLevel()
    {
        Tilemap tilemap = Object.FindFirstObjectByType<Tilemap>();
        if (tilemap == null)
        {
            Debug.LogError("Primero ejecuta 1) Configurar suelo y colisiones.");
            return;
        }

        TileBase groundTile = GetGroundTile();
        if (groundTile == null) return;

        // Hueco que obliga a saltar
        for (int x = 8; x <= 9; x++)
        {
            tilemap.SetTile(new Vector3Int(x, GroundRow, 0), null);
        }

        // Continua el suelo despues del hueco hasta el final del nivel
        for (int x = 10; x <= LevelEndX + 2; x++)
        {
            tilemap.SetTile(new Vector3Int(x, GroundRow, 0), groundTile);
        }

        // Plataforma elevada intermedia
        for (int x = 15; x <= 18; x++)
        {
            tilemap.SetTile(new Vector3Int(x, GroundRow + 2, 0), groundTile);
        }

        MarkSceneDirty();
        Debug.Log("Nivel ampliado: hueco en x=8-9, plataforma elevada en x=15-18, suelo hasta x=" + (LevelEndX + 2) + ".");
    }

    [MenuItem("Tools/Videojuego1/5) Colocar monedas, obstaculo y meta")]
    public static void PlaceItems()
    {
        float platformTopY = GroundRow + 1f;

        CreateCoinRow(new[] { 15.5f, 16.5f, 17.5f }, platformTopY + 2f);
        CreateCoinRow(new[] { 2f, 4f, 6f }, platformTopY + 0.5f);

        CreateHazard(new Vector3(22f, platformTopY, 0f));

        CreateGoal(new Vector3(LevelEndX, platformTopY + 1f, 0f));

        MarkSceneDirty();
        Debug.Log("Monedas, obstaculo (pinchos) y meta (diamante) colocados en el nivel.");
    }

    [MenuItem("Tools/Videojuego1/6) Crear camara con seguimiento")]
    public static void SetupCamera()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("Primero ejecuta 2) Crear jugador con fisica y script.");
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("No se encontro la Main Camera en la escena.");
            return;
        }

        System.Type followType = FindTypeByName("CameraFollow");
        if (followType == null)
        {
            Debug.LogError("No se encontro la clase CameraFollow. Espera a que Unity compile e intenta de nuevo.");
            return;
        }

        Component follow = mainCamera.GetComponent(followType);
        if (follow == null)
        {
            follow = mainCamera.gameObject.AddComponent(followType);
        }

        SerializedObject so = new SerializedObject(follow);
        so.FindProperty("target").objectReferenceValue = player.transform;
        so.FindProperty("minX").floatValue = -6f;
        so.FindProperty("maxX").floatValue = LevelEndX + 4f;
        so.ApplyModifiedProperties();

        MarkSceneDirty();
        Debug.Log("CameraFollow agregado a Main Camera, siguiendo a Player.");
    }

    [MenuItem("Tools/Videojuego1/7) Crear animaciones y Animator del personaje")]
    public static void SetupAnimator()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("Primero ejecuta 2) Crear jugador con fisica y script.");
            return;
        }

        const string folder = "Assets/Animations";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets", "Animations");
        }

        AnimationClip idle = CreateClipFromFrames("Idle", "Assets/Sprites/Character/character_berie_idle_{0}.png", 4, true, folder);
        AnimationClip run = CreateClipFromFrames("Run", "Assets/Sprites/Character/character_berie_run_{0}.png", 6, true, folder);
        AnimationClip jump = CreateClipFromFrames("Jump", "Assets/Sprites/Character/character_berie_jump_{0}.png", 4, false, folder);
        AnimationClip fall = CreateClipFromFrames("Fall", "Assets/Sprites/Character/character_berie_fall_{0}.png", 2, true, folder);

        string controllerPath = folder + "/PlayerAnimator.controller";
        var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        AddParameterIfMissing(controller, "Speed", AnimatorControllerParameterType.Float);
        AddParameterIfMissing(controller, "Grounded", AnimatorControllerParameterType.Bool);
        AddParameterIfMissing(controller, "VerticalVelocity", AnimatorControllerParameterType.Float);

        var rootSM = controller.layers[0].stateMachine;

        var idleState = FindOrAddState(rootSM, "Idle", idle);
        var runState = FindOrAddState(rootSM, "Run", run);
        var jumpState = FindOrAddState(rootSM, "Jump", jump);
        var fallState = FindOrAddState(rootSM, "Fall", fall);

        rootSM.defaultState = idleState;

        AddTransitionFloat(idleState, runState, AnimatorConditionMode.Greater, "Speed", 0.05f);
        AddTransitionFloat(runState, idleState, AnimatorConditionMode.Less, "Speed", 0.05f);

        AddTransitionBool(idleState, jumpState, "Grounded", false);
        AddTransitionBool(runState, jumpState, "Grounded", false);

        AddTransitionFloat(jumpState, fallState, AnimatorConditionMode.Less, "VerticalVelocity", 0f);

        AddTransitionBool(fallState, idleState, "Grounded", true);
        AddTransitionBool(jumpState, idleState, "Grounded", true);

        Animator animator = player.GetComponent<Animator>();
        if (animator == null) animator = player.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        MarkSceneDirty();
        Debug.Log("Animator creado con estados Idle/Run/Jump/Fall y asignado al Player.");
    }

    [MenuItem("Tools/Videojuego1/8) Crear sistemas de juego (GameManager y AudioManager)")]
    public static void SetupGameSystems()
    {
        CreateSingleton("GameManager");
        CreateSingleton("AudioManager");

        MarkSceneDirty();
        Debug.Log("GameManager y AudioManager creados en la escena.");
    }

    [MenuItem("Tools/Videojuego1/9) Crear interfaz de usuario (Canvas)")]
    public static void SetupUI()
    {
        System.Type uiManagerType = FindTypeByName("UIManager");
        if (uiManagerType == null)
        {
            Debug.LogError("No se encontro la clase UIManager. Espera a que Unity compile e intenta de nuevo.");
            return;
        }

        GameObject canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null)
        {
            canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
        }

        if (GameObject.Find("EventSystem") == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        UnityEngine.UI.Text coinsText = CreateLabel(canvasGO.transform, "CoinsText", "Monedas: 0", new Vector2(0f, 1f), new Vector2(10, -10));
        UnityEngine.UI.Text scoreText = CreateLabel(canvasGO.transform, "ScoreText", "Puntaje: 0", new Vector2(0f, 1f), new Vector2(10, -40));
        UnityEngine.UI.Text livesText = CreateLabel(canvasGO.transform, "LivesText", "Vidas: 3", new Vector2(0f, 1f), new Vector2(10, -70));
        UnityEngine.UI.Text timeText = CreateLabel(canvasGO.transform, "TimeText", "Tiempo: 0s", new Vector2(1f, 1f), new Vector2(-10, -10), TextAnchor.UpperRight);

        GameObject winPanel = CreateEndPanel(canvasGO.transform, "WinPanel", "¡NIVEL COMPLETADO!", new Color(0.1f, 0.6f, 0.2f, 0.85f));
        GameObject gameOverPanel = CreateEndPanel(canvasGO.transform, "GameOverPanel", "GAME OVER", new Color(0.6f, 0.1f, 0.1f, 0.85f));

        GameObject uiManagerGO = GameObject.Find("UIManager");
        if (uiManagerGO == null)
        {
            uiManagerGO = new GameObject("UIManager");
        }

        Component uiManager = uiManagerGO.GetComponent(uiManagerType);
        if (uiManager == null) uiManager = uiManagerGO.AddComponent(uiManagerType);

        SerializedObject so = new SerializedObject(uiManager);
        so.FindProperty("coinsText").objectReferenceValue = coinsText;
        so.FindProperty("scoreText").objectReferenceValue = scoreText;
        so.FindProperty("livesText").objectReferenceValue = livesText;
        so.FindProperty("timeText").objectReferenceValue = timeText;
        so.FindProperty("winPanel").objectReferenceValue = winPanel;
        so.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
        so.ApplyModifiedProperties();

        MarkSceneDirty();
        Debug.Log("UI creada: Canvas con monedas/puntaje/vidas/tiempo y paneles de victoria/derrota.");
    }

    [MenuItem("Tools/Videojuego1/10) Agregar muros bajos como obstaculo")]
    public static void AddWalls()
    {
        Tilemap tilemap = Object.FindFirstObjectByType<Tilemap>();
        if (tilemap == null)
        {
            Debug.LogError("Primero ejecuta 1) Configurar suelo y colisiones.");
            return;
        }

        TileBase groundTile = GetGroundTile();
        if (groundTile == null) return;

        int[] wallPositions = { 3, 25 };
        foreach (int x in wallPositions)
        {
            tilemap.SetTile(new Vector3Int(x, GroundRow + 1, 0), groundTile);
            tilemap.SetTile(new Vector3Int(x, GroundRow + 2, 0), groundTile);
        }

        MarkSceneDirty();
        Debug.Log("Muros bajos (2 tiles de alto, saltables) agregados en x=3 y x=25.");
    }

    [MenuItem("Tools/Videojuego1/11) Reforzar muro inicial y moneda sobre el muro")]
    public static void AdjustWalls()
    {
        Tilemap tilemap = Object.FindFirstObjectByType<Tilemap>();
        if (tilemap == null)
        {
            Debug.LogError("Primero ejecuta 1) Configurar suelo y colisiones.");
            return;
        }

        TileBase groundTile = GetGroundTile();
        if (groundTile == null) return;

        // Vuelve a pintar el muro cerca del inicio por si no quedo visible
        tilemap.SetTile(new Vector3Int(3, GroundRow + 1, 0), groundTile);
        tilemap.SetTile(new Vector3Int(3, GroundRow + 2, 0), groundTile);

        // Moneda flotando justo encima del muro en x=25
        float wallTopY = GroundRow + 2 + 1f;
        CreateCoinRow(new[] { 25.5f }, wallTopY + 0.5f);

        MarkSceneDirty();
        Debug.Log("Muro inicial reforzado y moneda agregada sobre el muro de x=25.");
    }

    [MenuItem("Tools/Videojuego1/12) Agregar plataformas a la izquierda del inicio")]
    public static void AddLeftPlatforms()
    {
        Tilemap tilemap = Object.FindFirstObjectByType<Tilemap>();
        if (tilemap == null)
        {
            Debug.LogError("Primero ejecuta 1) Configurar suelo y colisiones.");
            return;
        }

        TileBase groundTile = GetGroundTile();
        if (groundTile == null) return;

        int platformARow = GroundRow + 1;
        for (int x = -10; x <= -8; x++)
        {
            tilemap.SetTile(new Vector3Int(x, platformARow, 0), groundTile);
        }

        int platformBRow = GroundRow + 2;
        for (int x = -6; x <= -4; x++)
        {
            tilemap.SetTile(new Vector3Int(x, platformBRow, 0), groundTile);
        }

        CreateCoinRow(new[] { -9f }, platformARow + 1f + 0.5f);
        CreateCoinRow(new[] { -5f }, platformBRow + 1f + 0.5f);

        MarkSceneDirty();
        Debug.Log("Dos plataformas a distinta altura agregadas a la izquierda del inicio, cada una con una moneda.");
    }

    [MenuItem("Tools/Videojuego1/13) Subir plataforma izquierda al doble de altura")]
    public static void RaiseLeftPlatform()
    {
        Tilemap tilemap = Object.FindFirstObjectByType<Tilemap>();
        if (tilemap == null)
        {
            Debug.LogError("Primero ejecuta 1) Configurar suelo y colisiones.");
            return;
        }

        TileBase groundTile = GetGroundTile();
        if (groundTile == null) return;

        const int oldRow = GroundRow + 1;
        const int newRow = GroundRow + 4; // el doble de alto que la plataforma de la derecha (GroundRow+2)

        for (int x = -10; x <= -8; x++)
        {
            tilemap.SetTile(new Vector3Int(x, oldRow, 0), null);
            tilemap.SetTile(new Vector3Int(x, newRow, 0), groundTile);
        }

        float oldCoinY = oldRow + 1f + 0.5f;
        float newCoinY = newRow + 1f + 0.5f;

        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.gameObject.name == "Coin" && t.position.x < -6f && Mathf.Abs(t.position.y - oldCoinY) < 0.3f)
            {
                t.position = new Vector3(t.position.x, newCoinY, t.position.z);
            }
        }

        MarkSceneDirty();
        Debug.Log($"Plataforma izquierda subida a y={newRow}, el doble que la de la derecha (y={GroundRow + 2}).");
    }

    [MenuItem("Tools/Videojuego1/14) Devolver plataforma izquierda a su altura anterior")]
    public static void RevertLeftPlatform()
    {
        Tilemap tilemap = Object.FindFirstObjectByType<Tilemap>();
        if (tilemap == null)
        {
            Debug.LogError("Primero ejecuta 1) Configurar suelo y colisiones.");
            return;
        }

        TileBase groundTile = GetGroundTile();
        if (groundTile == null) return;

        const int currentRow = GroundRow + 4;
        const int previousRow = GroundRow + 1;

        for (int x = -10; x <= -8; x++)
        {
            tilemap.SetTile(new Vector3Int(x, currentRow, 0), null);
            tilemap.SetTile(new Vector3Int(x, previousRow, 0), groundTile);
        }

        float currentCoinY = currentRow + 1f + 0.5f;
        float previousCoinY = previousRow + 1f + 0.5f;

        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.gameObject.name == "Coin" && t.position.x < -6f && Mathf.Abs(t.position.y - currentCoinY) < 0.3f)
            {
                t.position = new Vector3(t.position.x, previousCoinY, t.position.z);
            }
        }

        MarkSceneDirty();
        Debug.Log($"Plataforma izquierda devuelta a y={previousRow}.");
    }

    [MenuItem("Tools/Videojuego1/Fix6) Reconstruir UI desde cero (arregla duplicados)")]
    public static void RebuildUI()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas != null) Object.DestroyImmediate(canvas);

        GameObject uiManager = GameObject.Find("UIManager");
        if (uiManager != null) Object.DestroyImmediate(uiManager);

        SetupUI();
        Debug.Log("UI reconstruida desde cero, sin duplicados.");
    }

    [MenuItem("Tools/Videojuego1/Fix5) Hacer clickeables los paneles de fin de nivel")]
    public static void MakePanelsClickable()
    {
        System.Type restartButtonType = FindTypeByName("RestartButton");
        if (restartButtonType == null)
        {
            Debug.LogError("No se encontro la clase RestartButton. Espera a que Unity compile e intenta de nuevo.");
            return;
        }

        int updated = 0;
        foreach (string panelName in new[] { "WinPanel", "GameOverPanel" })
        {
            GameObject panel = GameObject.Find(panelName);
            if (panel == null) continue;

            if (panel.GetComponent<UnityEngine.UI.Button>() == null)
            {
                panel.AddComponent<UnityEngine.UI.Button>();
            }

            if (panel.GetComponent(restartButtonType) == null)
            {
                panel.AddComponent(restartButtonType);
            }

            updated++;
        }

        MarkSceneDirty();
        Debug.Log($"Paneles ahora clickeables para reiniciar: {updated}.");
    }

    [MenuItem("Tools/Videojuego1/Fix4) Eliminar objetos duplicados (Hazard, Goal, Coin)")]
    public static void RemoveDuplicateLevelObjects()
    {
        int removed = 0;
        removed += RemoveDuplicatesByNameAndPosition("Hazard_Spike");
        removed += RemoveDuplicatesByNameAndPosition("Goal");
        removed += RemoveDuplicatesByNameAndPosition("Coin");

        MarkSceneDirty();
        Debug.Log($"Limpieza completa: {removed} objeto(s) duplicado(s) eliminado(s).");
    }

    private static int RemoveDuplicatesByNameAndPosition(string name)
    {
        var all = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t => t.gameObject.name == name)
            .ToList();

        var seen = new System.Collections.Generic.List<Vector3>();
        int removed = 0;

        foreach (var t in all)
        {
            if (t == null) continue;
            bool duplicate = seen.Any(p => Vector3.Distance(p, t.position) < 0.2f);
            if (duplicate)
            {
                Object.DestroyImmediate(t.gameObject);
                removed++;
            }
            else
            {
                seen.Add(t.position);
            }
        }

        return removed;
    }

    [MenuItem("Tools/Videojuego1/Fix3) Agregar texto de reinicio a paneles")]
    public static void AddRestartHintToPanels()
    {
        int added = 0;
        foreach (string panelName in new[] { "WinPanel", "GameOverPanel" })
        {
            GameObject panel = GameObject.Find(panelName);
            if (panel == null) continue;
            if (panel.transform.Find("RestartHint") != null) continue;

            GameObject hintGO = new GameObject("RestartHint", typeof(RectTransform));
            hintGO.transform.SetParent(panel.transform, false);
            RectTransform hintRect = hintGO.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.5f, 0.5f);
            hintRect.anchorMax = new Vector2(0.5f, 0.5f);
            hintRect.pivot = new Vector2(0.5f, 0.5f);
            hintRect.anchoredPosition = new Vector2(0f, -70f);
            hintRect.sizeDelta = new Vector2(600, 50);

            UnityEngine.UI.Text hint = hintGO.AddComponent<UnityEngine.UI.Text>();
            hint.text = "Espacio, R o clic para reiniciar";
            hint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hint.fontSize = 22;
            hint.alignment = TextAnchor.MiddleCenter;
            hint.color = Color.white;
            added++;
        }

        MarkSceneDirty();
        Debug.Log($"Texto de reinicio agregado a {added} panel(es).");
    }

    [MenuItem("Tools/Videojuego1/Fix2) Agregar zona de caida")]
    public static void AddKillZone()
    {
        System.Type hazardType = FindTypeByName("Hazard");
        if (hazardType == null)
        {
            Debug.LogError("No se encontro la clase Hazard. Espera a que Unity compile e intenta de nuevo.");
            return;
        }

        if (GameObject.Find("KillZone") != null)
        {
            Debug.LogWarning("Ya existe una KillZone en la escena.");
            return;
        }

        GameObject killZone = new GameObject("KillZone");
        killZone.transform.position = new Vector3(LevelEndX / 2f, GroundRow - 6f, 0f);

        BoxCollider2D col = killZone.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(LevelEndX + 60f, 2f);

        killZone.AddComponent(hazardType);

        MarkSceneDirty();
        Debug.Log("KillZone creada: si el jugador cae fuera del nivel, pierde una vida y vuelve al inicio.");
    }

    [MenuItem("Tools/Videojuego1/Fix) Bajar plataforma elevada y monedas")]
    public static void LowerElevatedPlatform()
    {
        Tilemap tilemap = Object.FindFirstObjectByType<Tilemap>();
        if (tilemap == null)
        {
            Debug.LogError("No hay Tilemap en la escena.");
            return;
        }

        TileBase groundTile = GetGroundTile();
        if (groundTile == null) return;

        const int oldRow = GroundRow + 3;
        const int newRow = GroundRow + 2;

        for (int x = 15; x <= 18; x++)
        {
            tilemap.SetTile(new Vector3Int(x, oldRow, 0), null);
            tilemap.SetTile(new Vector3Int(x, newRow, 0), groundTile);
        }

        float oldY = GroundRow + 1f + 3f;
        float newY = GroundRow + 1f + 2f;
        int moved = 0;

        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t.gameObject.name == "Coin" && Mathf.Abs(t.position.y - oldY) < 0.5f)
            {
                t.position = new Vector3(t.position.x, newY, t.position.z);
                moved++;
            }
        }

        MarkSceneDirty();
        Debug.Log($"Plataforma bajada de y={oldRow} a y={newRow}. {moved} moneda(s) reubicadas.");
    }

    private static TileBase GetGroundTile()
    {
        TileBase[] tiles = AssetDatabase.FindAssets("t:TileBase", new[] { TilesFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(p => p)
            .Select(AssetDatabase.LoadAssetAtPath<TileBase>)
            .Where(t => t != null)
            .ToArray();

        if (tiles.Length == 0)
        {
            Debug.LogError($"No se encontraron tiles en {TilesFolder}. Revisa que la ruta sea correcta.");
            return null;
        }

        return tiles[0];
    }

    private static void CreateCoinRow(float[] xPositions, float y)
    {
        System.Type coinType = FindTypeByName("Coin");
        Sprite coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/collectibles_coin_gold_1.png");
        if (coinType == null || coinSprite == null)
        {
            Debug.LogError("No se encontro el script Coin o el sprite de la moneda.");
            return;
        }

        foreach (float x in xPositions)
        {
            GameObject coin = new GameObject("Coin");
            coin.transform.position = new Vector3(x, y, 0f);

            var sr = coin.AddComponent<SpriteRenderer>();
            sr.sprite = coinSprite;
            sr.sortingOrder = 1;

            var col = coin.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = coinSprite.bounds.extents.x;

            coin.AddComponent(coinType);
        }
    }

    private static void CreateHazard(Vector3 position)
    {
        System.Type hazardType = FindTypeByName("Hazard");
        Sprite spikeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Obstacles/trap_spike_1.png");
        if (hazardType == null || spikeSprite == null)
        {
            Debug.LogError("No se encontro el script Hazard o el sprite de los pinchos.");
            return;
        }

        GameObject hazard = new GameObject("Hazard_Spike");
        hazard.transform.position = position + new Vector3(0f, spikeSprite.bounds.extents.y, 0f);

        var sr = hazard.AddComponent<SpriteRenderer>();
        sr.sprite = spikeSprite;
        sr.sortingOrder = 1;

        var col = hazard.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = spikeSprite.bounds.size * 0.8f;

        hazard.AddComponent(hazardType);
    }

    private static void CreateGoal(Vector3 position)
    {
        System.Type goalType = FindTypeByName("Goal");
        Sprite diamondSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Items/collectibles_treasure_diamond_1.png");
        if (goalType == null || diamondSprite == null)
        {
            Debug.LogError("No se encontro el script Goal o el sprite del diamante.");
            return;
        }

        GameObject goal = new GameObject("Goal");
        goal.transform.position = position;
        goal.transform.localScale = Vector3.one * 1.5f;

        var sr = goal.AddComponent<SpriteRenderer>();
        sr.sprite = diamondSprite;
        sr.sortingOrder = 1;

        var col = goal.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = diamondSprite.bounds.size;

        goal.AddComponent(goalType);
    }

    private static void CreateSingleton(string typeName)
    {
        GameObject existing = GameObject.Find(typeName);
        if (existing != null) return;

        System.Type type = FindTypeByName(typeName);
        if (type == null)
        {
            Debug.LogError($"No se encontro la clase {typeName}. Espera a que Unity compile e intenta de nuevo.");
            return;
        }

        GameObject go = new GameObject(typeName);
        go.AddComponent(type);
    }

    private static UnityEngine.UI.Text CreateLabel(Transform parent, string name, string text, Vector2 anchor, Vector2 anchoredPosition, TextAnchor alignment = TextAnchor.UpperLeft)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(300, 30);

        UnityEngine.UI.Text label = go.AddComponent<UnityEngine.UI.Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 24;
        label.alignment = alignment;
        label.color = Color.white;
        label.horizontalOverflow = UnityEngine.HorizontalWrapMode.Overflow;

        var outline = go.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        return label;
    }

    private static GameObject CreateEndPanel(Transform parent, string name, string message, Color backgroundColor)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = panel.AddComponent<UnityEngine.UI.Image>();
        image.color = backgroundColor;

        panel.AddComponent<UnityEngine.UI.Button>();
        System.Type restartButtonType = FindTypeByName("RestartButton");
        if (restartButtonType != null) panel.AddComponent(restartButtonType);

        GameObject textGO = new GameObject("Message", typeof(RectTransform));
        textGO.transform.SetParent(panel.transform, false);
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(800, 100);

        UnityEngine.UI.Text text = textGO.AddComponent<UnityEngine.UI.Text>();
        text.text = message;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 48;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        GameObject hintGO = new GameObject("RestartHint", typeof(RectTransform));
        hintGO.transform.SetParent(panel.transform, false);
        RectTransform hintRect = hintGO.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 0.5f);
        hintRect.anchorMax = new Vector2(0.5f, 0.5f);
        hintRect.pivot = new Vector2(0.5f, 0.5f);
        hintRect.anchoredPosition = new Vector2(0f, -70f);
        hintRect.sizeDelta = new Vector2(600, 50);

        UnityEngine.UI.Text hint = hintGO.AddComponent<UnityEngine.UI.Text>();
        hint.text = "Espacio, R o clic para reiniciar";
        hint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hint.fontSize = 22;
        hint.alignment = TextAnchor.MiddleCenter;
        hint.color = Color.white;

        panel.SetActive(false);
        return panel;
    }

    private static AnimationClip CreateClipFromFrames(string name, string pathPattern, int frameCount, bool loop, string folder)
    {
        string clipPath = $"{folder}/{name}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        clip.frameRate = 10f;

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        var keyframes = new ObjectReferenceKeyframe[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            string spritePath = string.Format(pathPattern, i + 1);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / clip.frameRate,
                value = sprite
            };
        }

        EditorCurveBinding binding = new EditorCurveBinding
        {
            path = "",
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        return clip;
    }

    private static void AddParameterIfMissing(UnityEditor.Animations.AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        if (controller.parameters.Any(p => p.name == name)) return;
        controller.AddParameter(name, type);
    }

    private static AnimatorState FindOrAddState(AnimatorStateMachine sm, string name, Motion clip)
    {
        foreach (var child in sm.states)
        {
            if (child.state.name == name)
            {
                child.state.motion = clip;
                return child.state;
            }
        }

        AnimatorState state = sm.AddState(name);
        state.motion = clip;
        return state;
    }

    private static void AddTransitionFloat(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, string param, float threshold)
    {
        if (from.transitions.Any(t => t.destinationState == to)) return;

        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.05f;
        transition.AddCondition(mode, threshold, param);
    }

    private static void AddTransitionBool(AnimatorState from, AnimatorState to, string param, bool value)
    {
        if (from.transitions.Any(t => t.destinationState == to)) return;

        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.05f;
        transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, param);
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
