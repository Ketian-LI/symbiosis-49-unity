using UnityEngine;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

// A self-contained visual trial. It does not touch the construction controller,
// scoring, navigation, or any existing scene.
public sealed class Symbiosis49ArtStyleTrial : MonoBehaviour
{
    private const float CanvasWidth = 1920f;
    private const float CanvasHeight = 1080f;
    private const float BoardX = 500f;
    private const float BoardY = 80f;
    private const float Cell = 184f;

    private static readonly int[] PlotX = { 94, 418, 736, 1051, 1370 };
    private static readonly Rect[,] Pose =
    {
        { new Rect(144, 104, 172, 285), new Rect(560, 94, 186, 296), new Rect(1015, 94, 182, 296), new Rect(1428, 102, 230, 307) },
        { new Rect(105, 540, 257, 254), new Rect(527, 540, 308, 254), new Rect(965, 530, 300, 262), new Rect(1394, 540, 306, 254) }
    };

    // Pigeon, squirrel, hedgehog, fox. Rectangles are top-left pixel coordinates.
    private static readonly Rect[,] WildlifePose =
    {
        { new Rect(58, 95, 254, 270), new Rect(380, 110, 289, 255), new Rect(733, 149, 294, 216) },
        { new Rect(47, 360, 263, 345), new Rect(371, 418, 315, 291), new Rect(741, 360, 271, 351) },
        { new Rect(54, 766, 270, 260), new Rect(391, 770, 285, 243), new Rect(742, 773, 271, 250) },
        { new Rect(27, 1072, 310, 286), new Rect(360, 1097, 339, 243), new Rect(722, 1108, 330, 244) }
    };

    // 0 oak, 1 meadow, 2 shrub, 3 residence, 4 workshop,
    // 5 restaurant, 6 supermarket, 7 waste, 8 street, 9 square.
    private static readonly int[,] Board =
    {
        { -1, -1, -1, -1, -1 },
        { -1,  1,  8,  3, -1 },
        {  9,  0,  8,  4,  2 },
        { -1,  6,  8,  5,  7 },
        { -1, -1, -1, -1, -1 }
    };

    private static readonly Color Navy = Hex(0x172D3F);
    private static readonly Color Gold = Hex(0xC9A968);
    private static readonly Color Paper = Hex(0xF2DFBC);
    private static readonly Color PaperLight = Hex(0xF9EACC);
    private static readonly Color Sage = Hex(0x70866F);
    private static readonly Color Grid = Hex(0xE8D8B1);

    private Texture2D plots;
    private Texture2D peopleAndCar;
    private Texture2D wildlife;
    private float elapsed;
    private bool paused;

    private void OnEnable() => LoadTextures();

    private void Update()
    {
        if (!Application.isPlaying) return;
        if (Input.GetKeyDown(KeyCode.Space)) paused = !paused;
        if (Input.GetKeyDown(KeyCode.R)) elapsed = 0f;
        if (!paused) elapsed += Time.unscaledDeltaTime;
    }

    private void LoadTextures()
    {
        if (plots == null) plots = Resources.Load<Texture2D>("ArtStyleExperiment/urban-plots-v1");
        if (peopleAndCar == null) peopleAndCar = Resources.Load<Texture2D>("ArtStyleExperiment/resident-car-poses-v2");
        if (wildlife == null) wildlife = Resources.Load<Texture2D>("ArtStyleExperiment/four-animals-poses-v2");
    }

    private void OnGUI()
    {
        LoadTextures();
        if (plots == null || peopleAndCar == null || wildlife == null)
        {
            GUI.Label(new Rect(24, 24, 700, 40), "Art style trial textures are missing from Resources/ArtStyleExperiment.");
            return;
        }

        var oldMatrix = GUI.matrix;
        var oldColor = GUI.color;
        var scale = Mathf.Min(Screen.width / CanvasWidth, Screen.height / CanvasHeight);
        var offsetX = (Screen.width - CanvasWidth * scale) * 0.5f;
        var offsetY = (Screen.height - CanvasHeight * scale) * 0.5f;
        GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity,
            new Vector3(scale, scale, 1f));

        Fill(new Rect(0, 0, CanvasWidth, CanvasHeight), Paper);
        Fill(new Rect(0, 0, CanvasWidth, 18), Navy);
        Fill(new Rect(0, CanvasHeight - 18, CanvasWidth, 18), Navy);


        DrawPanel(new Rect(52, 112, 370, 836));
        DrawPanel(new Rect(1498, 112, 370, 836));
        DrawBoard();
        DrawSideStudies();
        DrawActors();

        GUI.matrix = oldMatrix;
        GUI.color = oldColor;
    }

    private static void DrawPanel(Rect r)
    {
        Fill(r, Navy);
        Fill(Inset(r, 10), Gold);
        Fill(Inset(r, 16), PaperLight);
    }

    private void DrawBoard()
    {
        var outer = new Rect(BoardX - 15, BoardY - 15, Cell * 5 + 30, Cell * 5 + 30);
        Fill(outer, Navy);
        Fill(Inset(outer, 7), Gold);
        Fill(Inset(outer, 12), Grid);

        for (var row = 0; row < 5; row++)
        for (var col = 0; col < 5; col++)
        {
            var cell = new Rect(BoardX + col * Cell + 2, BoardY + row * Cell + 2,
                Cell - 4, Cell - 4);
            Fill(cell, Sage);
            var plot = Board[row, col];
            if (plot >= 0) DrawPlot(cell, plot);
        }
    }

    private void DrawSideStudies()
    {
        // Enlarged source details show what is static room art, separately from actors.
        DrawPlot(new Rect(91, 151, 290, 290), 3); // urban residence
        DrawPlot(new Rect(1541, 151, 283, 283), 0); // oak
        DrawPlot(new Rect(78, 548, 152, 152), 5); // restaurant
        DrawPlot(new Rect(244, 548, 152, 152), 4); // workshop
        DrawPlot(new Rect(78, 711, 152, 152), 6); // supermarket
        DrawPlot(new Rect(244, 711, 152, 152), 7); // waste

        Fill(new Rect(1534, 489, 298, 2), Gold);
        DrawWildlife(new Rect(1535, 510, 119, 119), 0, 0);
        DrawWildlife(new Rect(1693, 510, 119, 119), 1, 0);
        DrawWildlife(new Rect(1535, 657, 119, 119), 2, 0);
        DrawWildlife(new Rect(1693, 657, 119, 119), 3, 0);
    }

    private void DrawActors()
    {
        var walkFrame = 1 + Mathf.FloorToInt(elapsed * 5f) % 2;
        var animalFrame = Mathf.FloorToInt(elapsed * 2.4f) % 3;
        var roadCenter = BoardX + Cell * 2.5f;
        var cycle = Mathf.Repeat(elapsed, 12f);

        // The pedestrian and car take turns on the narrow trial street so
        // their silhouettes remain readable at the intended game scale.
        if (cycle < 6f)
        {
            var residentY = BoardY + Cell * (1.20f + cycle / 6f * 2.50f);
            DrawPerson(new Rect(roadCenter - 22, residentY - 26, 44, 52), walkFrame);
        }
        else
        {
            var carY = BoardY + Cell * (3.68f - (cycle - 6f) / 6f * 2.36f);
            DrawCar(new Rect(roadCenter - 29, carY - 28, 58, 56), 0);
        }

        // A second resident is visibly using the restaurant.
        DrawPerson(new Rect(BoardX + Cell * 3.36f, BoardY + Cell * 3.60f, 43, 54), 3);

        DrawWildlife(new Rect(BoardX + Cell * 0.30f, BoardY + Cell * 2.59f, 43, 45),
            0, animalFrame);
        DrawWildlife(new Rect(BoardX + Cell * (1.62f + 0.08f * Mathf.Sin(elapsed * 2.5f)),
            BoardY + Cell * 2.62f, 43, 46), 1, animalFrame);
        DrawWildlife(new Rect(BoardX + Cell * 4.55f, BoardY + Cell * 2.67f, 46, 43),
            2, animalFrame);
        DrawWildlife(new Rect(BoardX + Cell * 1.44f, BoardY + Cell * 1.55f, 53, 46),
            3, animalFrame);
    }

    private void DrawPlot(Rect destination, int index)
    {
        var col = index % 5;
        var row = index / 5;
        var source = new Rect(PlotX[col], row == 0 ? 116 : 459, 310, 310);
        DrawAtlas(plots, source, destination);
    }

    private void DrawPerson(Rect destination, int frame) =>
        DrawAtlas(peopleAndCar, Pose[0, Mathf.Clamp(frame, 0, 3)], destination);

    private void DrawCar(Rect destination, int frame) =>
        DrawAtlas(peopleAndCar, Pose[1, Mathf.Clamp(frame, 0, 3)], destination);

    private void DrawWildlife(Rect destination, int species, int frame) =>
        DrawAtlas(wildlife, WildlifePose[Mathf.Clamp(species, 0, 3), Mathf.Clamp(frame, 0, 2)],
            destination);

    private static void DrawAtlas(Texture2D atlas, Rect pixelsFromTopLeft, Rect destination)
    {
        var uv = new Rect(pixelsFromTopLeft.x / atlas.width,
            1f - (pixelsFromTopLeft.y + pixelsFromTopLeft.height) / atlas.height,
            pixelsFromTopLeft.width / atlas.width,
            pixelsFromTopLeft.height / atlas.height);
        GUI.DrawTextureWithTexCoords(destination, atlas, uv, true);
    }

    private static void Fill(Rect rect, Color color)
    {
        var old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    private static Rect Inset(Rect r, float amount) =>
        new Rect(r.x + amount, r.y + amount, r.width - amount * 2f, r.height - amount * 2f);

    private static Color Hex(uint rgb) => new Color(
        ((rgb >> 16) & 0xff) / 255f,
        ((rgb >> 8) & 0xff) / 255f,
        (rgb & 0xff) / 255f);

#if UNITY_EDITOR
    private const string PreviousPlayStartSceneKey = "Symbiosis49ArtStyleTrial.PreviousPlayStartScene";
    private const string TrialPlayPendingKey = "Symbiosis49ArtStyleTrial.PlayPending";

    [InitializeOnLoadMethod]
    private static void RegisterPlayStartRestore()
    {
        EditorApplication.playModeStateChanged -= RestorePlayStartScene;
        EditorApplication.playModeStateChanged += RestorePlayStartScene;
    }

    private static void RestorePlayStartScene(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredPlayMode &&
            change != PlayModeStateChange.EnteredEditMode) return;
        if (!SessionState.GetBool(TrialPlayPendingKey, false)) return;

        var previousPath = SessionState.GetString(PreviousPlayStartSceneKey, "");
        EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previousPath)
            ? null
            : AssetDatabase.LoadAssetAtPath<SceneAsset>(previousPath);
        SessionState.EraseBool(TrialPlayPendingKey);
        SessionState.EraseString(PreviousPlayStartSceneKey);
    }

    [MenuItem("SYMBIOSIS 49/Play Urban Art Style Trial")]
    public static void PlayTrialScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        const string trialPath = "Assets/ArtStyleExperiment/Scenes/UrbanArtStyleTrial.unity";
        var trial = AssetDatabase.LoadAssetAtPath<SceneAsset>(trialPath);
        if (trial == null)
        {
            Debug.LogError("Urban art style trial scene is missing: " + trialPath);
            return;
        }

        var previous = EditorSceneManager.playModeStartScene;
        SessionState.SetString(PreviousPlayStartSceneKey,
            previous == null ? "" : AssetDatabase.GetAssetPath(previous));
        SessionState.SetBool(TrialPlayPendingKey, true);
        EditorSceneManager.playModeStartScene = trial;
        EditorApplication.isPlaying = true;
    }

    [MenuItem("SYMBIOSIS 49/Create Urban Art Style Trial Scene")]
    public static void CreateScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Paper;
        camera.orthographic = true;
        cameraObject.AddComponent<AudioListener>();

        new GameObject("Urban Art Style Trial").AddComponent<Symbiosis49ArtStyleTrial>();

        Directory.CreateDirectory("Assets/ArtStyleExperiment/Scenes");
        EditorSceneManager.SaveScene(scene,
            "Assets/ArtStyleExperiment/Scenes/UrbanArtStyleTrial.unity");
        Debug.Log("Art style trial scene saved: Assets/ArtStyleExperiment/Scenes/UrbanArtStyleTrial.unity");
    }
#endif
}
