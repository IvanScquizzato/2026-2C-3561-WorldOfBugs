using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using TGC.MonoGame.TP.Bush;
using TGC.MonoGame.TP.Cameras;
using TGC.MonoGame.TP.Car;
using TGC.MonoGame.TP.Debri;
using TGC.MonoGame.TP.Fences;
using TGC.MonoGame.TP.Ground;
using TGC.MonoGame.TP.Terrain;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Monument;
using TGC.MonoGame.TP.Renderers;
using TGC.MonoGame.TP.Tanks;
using TGC.MonoGame.TP.Trees;
using TGC.MonoGame.TP.Forces;
using TGC.MonoGame.TP.Plants;
using TGC.MonoGame.TP.RuinHouse;
using TGC.MonoGame.TP.Editor;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using TGC.MonoGame.TP.Physics.Bepu;
using BepuUtilities.Memory;
using NumericVector3 = System.Numerics.Vector3;
using TGC.MonoGame.Utils;
using System.Linq;
using TGC.MonoGame.TP.TankRaycasts;
using TGC.MonoGame.TP.Missile;
using TGC.MonoGame.TP.ShapeRenderers;
using TGC.MonoGame.TP.Colliders;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Concurrent;
using System.Diagnostics;
namespace TGC.MonoGame.TP;

public struct ImpactData
{
    public ModelInScene modelA;
    public ModelInScene modelB;
    public Vector3 positionA;
    public Vector3 positionB;
    public Vector3 collisionOffsetA;
    public Vector3 collisionOffsetB;
    public float impactImpulse;
    public float destructiveImpactImpulse;
    public Vector3 normalFromAToB;
}
/// <summary>
///     Esta es la clase principal del juego.
///     Inicialmente puede ser renombrado o copiado para hacer mas ejemplos chicos, en el caso de copiar para que se
///     ejecute el nuevo ejemplo deben cambiar la clase que ejecuta Program <see cref="Program.Main()" /> linea 10.
/// </summary>
public class TGCGame : Game
{
    public const string ContentFolder3D = "Models/";
    public const string ContentFolderEffects = "Effects/";
    public const string ContentFolderMusic = "Music/";
    public const string ContentFolderSounds = "Sounds/";
    public const string ContentFolderSpriteFonts = "SpriteFonts/";
    public const string ContentFolderTextures = "Textures/";

    public static Simulation _simulation;
    private BufferPool _bufferPool;

    private readonly GraphicsDeviceManager _graphics;
    private Camera _camera;
    private Camera _camera2;
    public Camera _currentCamera;
    private Camera _cameraAntesDelEditor;
    private SceneEditor _sceneEditor;
    private SceneRepository _sceneRepository;
    // Solo objetos cuya transformacion cambio: al salir del editor se actualiza Bepu.
    private readonly HashSet<ModelInScene> _editorModificados = new();
    private readonly Dictionary<string, ModelInScene> _objetosEditor = new();
    // HUD y menú (recuperados del merge).
    private Texture2D _pixel;
    private HUD.HUD _hud;
    private GameState _gameState = GameState.MainMenu;
    private HUD.MainMenu _mainMenu;
    private HUD.PauseMenu _pauseMenu;
    private SpriteFont _menuFont;

    private Effect _effect;
    private BasicRenderer _basicRenderer;
    private TerrainRenderer _terrainRenderer;
    private KeyboardState _previousKeyboardState;
    private SpriteBatch _spriteBatch;
    private Effect _debugEffect;

    private List<SimpleTerrain> _terrains = new List<SimpleTerrain>();

    private List<Tank> _tanks = new List<Tank>();
    private List<Tree> _trees = new List<Tree>();
    private List<Plant> _plants = new List<Plant>();
    private List<ModelInScene> _decor = new List<ModelInScene>();
    private List<IEnumerable<ModelInScene>> _modelosEnEscenario = new List<IEnumerable<ModelInScene>>();
    private List<BodyHandle> _tanksHandles = new List<BodyHandle>();
    private Dictionary<Tree, StaticHandle> _treeHandles = new Dictionary<Tree, StaticHandle>();

    private List<Missil> _missiles = new List<Missil>();
    private MouseState _previousMouseState;
    private const float MissileSpeed = 1500f;
    public static Vector3 gravity = new Vector3(0, -1000f, 0);
    public static Dictionary<CollidableReference, ModelInScene> PhysicsToGameObjects = new Dictionary<CollidableReference, ModelInScene>();

    public static ConcurrentQueue<ImpactData> PendingImpacts { get; } = new();

    // sonidos los static los usan los objetos en sus eventos de colision
    public static SoundEffect ExplosionSound;
    public static SoundEffect CrashSound;
    public static SoundEffect TreeBreakSound;
    public static SoundEffect ClickSound;
    private SoundEffect _cannonSound;
    private SoundEffectInstance _engineSound;
    /// <summary>
    ///     Constructor del juego.
    /// </summary>
    public TGCGame()
    {
        // Maneja la configuracion y la administracion del dispositivo grafico.
        _graphics = new GraphicsDeviceManager(this);

        _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width - 100;
        _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - 100;

        // Para que el juego sea pantalla completa se puede usar Graphics IsFullScreen.
        // Carpeta raiz donde va a estar toda la Media.
        Content.RootDirectory = "Content";
        // Hace que el mouse sea visible.
        IsMouseVisible = true;
    }

    public enum GameState
    {
        MainMenu,
        Playing,
        Paused
    }

    /// <summary>
    ///     Se llama una sola vez, al principio cuando se ejecuta el ejemplo.
    ///     Escribir aqui el codigo de inicializacion: el procesamiento que podemos pre calcular para nuestro juego.
    /// </summary>
    protected override void Initialize()
    {
        // La logica de inicializacion que no depende del contenido se recomienda poner en este metodo.

        // Apago el backface culling.
        // Esto se hace por un problema en el diseno del modelo del logo de la materia.
        // Una vez que empiecen su juego, esto no es mas necesario y lo pueden sacar.
        var rasterizerState = new RasterizerState();
        rasterizerState.CullMode = CullMode.None;
        GraphicsDevice.RasterizerState = rasterizerState;
        // Seria hasta aca.

        _camera = new SimpleCamera(GraphicsDevice.Viewport.AspectRatio, Vector3.UnitY * 500, 200, 1f, 1, 1000000);
        _modelosEnEscenario.Add(_tanks);
        _modelosEnEscenario.Add(_trees);
        _modelosEnEscenario.Add(_plants);
        _modelosEnEscenario.Add(_terrains);
        _currentCamera = _camera;
        _modelosEnEscenario.Add(_decor);
        _modelosEnEscenario.Add(_missiles);

        _bufferPool = new BufferPool();

        base.Initialize();
    }

    /// <summary>
    ///     Se llama una sola vez, al principio cuando se ejecuta el ejemplo, despues de Initialize.
    ///     Escribir aqui el codigo de inicializacion: cargar modelos, texturas, estructuras de optimizacion, el procesamiento
    ///     que podemos pre calcular para nuestro juego.
    /// </summary>
    protected override void LoadContent()
    {
        // hitbox debug
        _debugEffect = Content.Load<Effect>(
            ContentFolderEffects + "DebugShader"
        );
        // Aca es donde deberiamos cargar todos los contenido necesarios antes de iniciar el juego.
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Cargo un efecto basico propio declarado en el Content pipeline.
        // En el juego no pueden usar BasicEffect de MG, deben usar siempre efectos propios.
        _effect = Content.Load<Effect>(ContentFolderEffects + "BasicShader");
        _basicRenderer = new BasicRenderer(GraphicsDevice, _effect, _debugEffect);

        //Cargo terreno
        var terrainEffect = Content.Load<Effect>(ContentFolderEffects + "Terrain");

        _terrainRenderer = new TerrainRenderer(GraphicsDevice, terrainEffect);
        // heights
        var terrainHeigthmap = Content.Load<Texture2D>(ContentFolderTextures + "Heightmaps/heightmap");

        // basic color
        var terrainColorMap = Content.Load<Texture2D>(ContentFolderTextures + "Heightmaps/colormap");

        // blend texture 1
        var terrainGrass = Content.Load<Texture2D>(ContentFolderTextures + "Heightmaps/grass");

        // blend texture 2
        var terrainGround = Content.Load<Texture2D>(ContentFolderTextures + "Heightmaps/ground");
        _terrains.Add(new SimpleTerrain(Content, terrainHeigthmap, terrainColorMap, terrainGrass, terrainGround, _terrainRenderer));
        //Cargo tanque
        float altura_tanque = _terrains[0].Height(0, -300) + 30;
        _tanks.Add(new Tank(Content, ContentFolder3D + "Tanks/Panzer/Panzer", new Vector3(0, altura_tanque + 300, -600), Matrix.Identity, new Vector3(0.5f), _basicRenderer, 11000f));
        // tanques enemigos, por ahora quietos
        _tanks.Add(new Tank(Content, ContentFolder3D + "Tanks/Panzer/Panzer", new Vector3(1500, _terrains[0].Height(1500, -600) + 300, -600), Matrix.Identity, new Vector3(0.5f), _basicRenderer, 11000f));
        _tanks.Add(new Tank(Content, ContentFolder3D + "Tanks/Panzer/Panzer", new Vector3(-1500, _terrains[0].Height(-1500, -600) + 300, -600), Matrix.Identity, new Vector3(0.5f), _basicRenderer, 11000f));
        _tanks.Add(new Tank(Content, ContentFolder3D + "Tanks/Panzer/Panzer", new Vector3(0, _terrains[0].Height(0, 1000) + 300, 1000), Matrix.Identity, new Vector3(0.5f), _basicRenderer, 11000f));
        _camera2 = new ThirdPersonCamera(_tanks[0], 1000f, 0.005f, GraphicsDevice.Viewport.AspectRatio, 500f, 400f, 1f, 20000f, GraphicsDevice);
        _currentCamera = _camera2;
        // El JSON es la fuente de verdad del escenario estático.
        _sceneRepository = new SceneRepository();
        _sceneEditor = new SceneEditor(GraphicsDevice)
        {
            DuplicateFactory = CrearCopiaEditor,
            SaveRequested = GuardarEscena,
            LoadRequested = RecargarEscena
        };

        SceneRepository.SceneFile scene = _sceneRepository.Load();
        if (scene.Initialized)
        {
            CargarObjetosEscena(scene);
        }
        else
        {
            // Primera ejecución: conservar el escenario original, migrar el viejo
            // scene.json si existe y dejar toda la escena serializada en Scenes/scene.json.
            CrearEscenaInicial();
            RegistrarEscenaInicial();
            MigrarEscenaAnterior();
            GuardarEscena();
        }

        _simulation = Simulation.Create(_bufferPool, new NarrowPhaseCallbacks(new SpringSettings(30, 1), 1.5f, 0.5f),
        new PoseIntegratorCallbacks(UtilsClass.ToNumericVector(gravity)), new SolveDescription(8, 1));


        foreach (var lista in _modelosEnEscenario)
        {
            foreach (var modelo in lista)
            {
                modelo.CreateCollider(_simulation, _bufferPool, _effect, GraphicsDevice);
                if (modelo._collider != null)
                {
                    PhysicsToGameObjects[modelo._collider.GetCollidableReference()] = modelo;
                }
                modelo.CreateHitbox(_debugEffect, GraphicsDevice);
            }
        }



        _editorModificados.Clear(); // Los colliders iniciales ya tienen las transformaciones cargadas.

        // Interfaz: se carga después de inicializar el escenario y los tanques.
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _hud = new HUD.HUD(_pixel, _tanks[0]);
        _menuFont = Content.Load<SpriteFont>("MenuFont");
        _mainMenu = new HUD.MainMenu(_pixel, _menuFont);
        _pauseMenu = new HUD.PauseMenu(_pixel, _menuFont);

        // sonidos y musica cargados igual que en los samples de la catedra
        _cannonSound = Content.Load<SoundEffect>(ContentFolderSounds + "cannon_fire");
        ExplosionSound = Content.Load<SoundEffect>(ContentFolderSounds + "mechanical_explosion");
        CrashSound = Content.Load<SoundEffect>(ContentFolderSounds + "metal_hit_01");
        TreeBreakSound = Content.Load<SoundEffect>(ContentFolderSounds + "wood_breaking_01");
        ClickSound = Content.Load<SoundEffect>(ContentFolderSounds + "click");
        _engineSound = Content.Load<SoundEffect>(ContentFolderSounds + "engine_heavy_loop").CreateInstance();
        _engineSound.IsLooped = true;

        MediaPlayer.IsRepeating = true;
        MediaPlayer.Volume = 0.3f;
        MediaPlayer.Play(Content.Load<Song>(ContentFolderMusic + "determined_pursuit_loop"));

        base.LoadContent();

    }

    // Se ejecuta una sola vez para generar la escena de ejemplo anterior.
    // A partir del primer guardado, la escena se crea SOLO desde JSON.
    private void CrearEscenaInicial()
    {
        Random _rng = new Random(12345); // Semilla fija para que la escena guardada conserve sus IDs.

        //cargo arbol
        _trees.Add(new Tree(Content, ContentFolder3D + "Tree/Tree", new Vector3(200, 500, 0), Matrix.Identity, new Vector3(150f), _basicRenderer));
        for (int i = 0; i < 200; i++)
        {
            float x = (float)(_rng.NextDouble() * 13000 - 6000);
            float z = (float)(_rng.NextDouble() * 12000 - 6000);
            float y = _terrains[0].Height(x, z) - 10;
            _trees.Add(new Tree(Content, ContentFolder3D + "Tree/Tree", new Vector3(x, y, z), Matrix.Identity, new Vector3(150f), _basicRenderer));
        }

        // Estructuras de relleno
        _decor.Add(new RuinHouse1(Content, ContentFolder3D + "AbandonedHouse/source/abandonhouse", new Vector3(400, _terrains[0].Height(400, -5000) - 10, -5000), Matrix.Identity, new Vector3(1f), _basicRenderer));
        _decor.Add(new RuinHouse1(Content, ContentFolder3D + "AbandonedHouse/source/abandonhouse", new Vector3(5100, _terrains[0].Height(5100, 5000) - 10, 5000), Matrix.Identity, new Vector3(1f), _basicRenderer));
        _decor.Add(new RuinHouse1(Content, ContentFolder3D + "AbandonedHouse/source/abandonhouse", new Vector3(400, _terrains[0].Height(400, 5000) - 10, 5000), Matrix.Identity, new Vector3(1f), _basicRenderer));
        Random _random_X;
        Random _random_Z;
        for (int i = 0; i < 20; i++)
        {
            _random_X = _rng;
            _random_Z = _rng;
            float x = (float)(_random_X.NextDouble() * 13000 - 6000);
            float z = (float)(_random_Z.NextDouble() * 12000 - 6000);
            float y = _terrains[0].Height(x, z) - 10;
            _decor.Add(new Fence(Content, ContentFolder3D + "Rocks/source/stone_fence_old_low", new Vector3(x, y, z), Matrix.Identity, new Vector3(1f), _basicRenderer));
        }

        for (int i = 0; i < 10; i++)
        {
            _random_X = _rng;
            _random_Z = _rng;
            float x = (float)(_random_X.NextDouble() * 13000 - 6000);
            float z = (float)(_random_Z.NextDouble() * 12000 - 6000);
            float y = _terrains[0].Height(x, z) - 10;
            _decor.Add(new Debris(Content, ContentFolder3D + "RocksMedium/source/Rocks_medium/Rocks_medium", new Vector3(x, y, z), Matrix.Identity, new Vector3(1f), _basicRenderer));
        }

        /*for (int i = 0; i < 100; i++)
        {
            _random_X = new Random();
            _random_Z = new Random();
            float x = (float)(_random_X.NextDouble() * 13000 - 6000);
            float z = (float)(_random_Z.NextDouble() * 12000 - 6000);
            float y = _terrains[0].Height(x, z) - 10;
            _decor.Add(new DeadBush(Content, ContentFolder3D + "DeadBush/source/Salix elaeagnos HD_Dead mat 50_LOD0", new Vector3(x, y, z), Matrix.Identity, new Vector3(1f), _basicRenderer));
        }*/

        _decor.Add(new AbandonedCar(Content, ContentFolder3D + "Car/source/car_low", new Vector3(1700, _terrains[0].Height(1700, 4500) - 10, 4500), Matrix.Identity, new Vector3(.7f), _basicRenderer));
        _decor.Add(new AbandonedCar(Content, ContentFolder3D + "Car/source/car_low", new Vector3(3000, _terrains[0].Height(3000, 1500) - 10, 1500), Matrix.Identity, new Vector3(.7f), _basicRenderer));

        _decor.Add(new Obelisc(Content, ContentFolder3D + "Monumento/source/Monumento", new Vector3(2000, _terrains[0].Height(2000, 5000), 5000), Matrix.Identity, new Vector3(6f), _basicRenderer));

        for (int i = 0; i < 200; i++)
        {
            _random_X = _rng;
            _random_Z = _rng;
            float x = (float)(_random_X.NextDouble() * 13000 - 6000);
            float z = (float)(_random_Z.NextDouble() * 12000 - 6000);
            float y = _terrains[0].Height(x, z) - 10;
            _decor.Add(new Grass(Content, ContentFolder3D + "Grass/source/grassExampleScene", new Vector3(x, y, z), Matrix.Identity, new Vector3(.5f), _basicRenderer));
        }


        //cargo planta
        /*
        _plants.Add(new Plant(Content, ContentFolder3D + "Plant/source/plant1_afsTREE_xlod00", new Vector3(200, 500, 0), Matrix.Identity, new Vector3(0.5f), _basicRenderer));
        for (int i = 0; i < 200; i++)
        {
            float x = (float)(_rng.NextDouble() * 13000 - 6000);
            float z = (float)(_rng.NextDouble() * 12000 - 6000);
            float y = _terrains[0].Height(x, z) - 10;
            _plants.Add(new Plant(Content, ContentFolder3D + "Plant/source/plant1_afsTREE_xlod00", new Vector3(x, y, z), Matrix.Identity, new Vector3(0.5f), _basicRenderer));
        }
        */

    }

    private void RegistrarEscenaInicial()
    {
        for (int i = 0; i < _trees.Count; i++)
            RegistrarEnEditor(_trees[i], $"Tree_{i}");
        for (int i = 0; i < _decor.Count; i++)
            RegistrarEnEditor(_decor[i], $"Decor_{i}");
    }

    private void GuardarEscena()
    {
        _sceneRepository.Save(_objetosEditor);
        Console.WriteLine($"Escenario guardado en: {_sceneRepository.FilePath}");
    }

    // Ctrl+L reemplaza las instancias, no las agrega encima de las existentes.
    private void RecargarEscena()
    {
        SceneRepository.SceneFile scene = _sceneRepository.Load();
        if (!scene.Initialized)
        {
            Console.WriteLine("El archivo de escena todavía no está inicializado.");
            return;
        }
        CargarObjetosEscena(scene);
        Console.WriteLine($"Escenario cargado desde: {_sceneRepository.FilePath}");
    }

    private void CargarObjetosEscena(SceneRepository.SceneFile scene)
    {
        // Preparar primero todos los modelos. Si un modelo de JSON falla,
        // no destruimos la escena que ya estaba en memoria.
        var nuevos = new List<(string Id, ModelInScene Model)>();
        foreach (SceneRepository.SceneObject data in scene.Objects)
        {
            ModelInScene modelo = SceneRepository.Create(data, Content, _basicRenderer);
            nuevos.Add((data.Id, modelo));
        }

        // Quitar de BEPU exclusivamente objetos estáticos administrados por el editor.
        foreach (ModelInScene viejo in _objetosEditor.Values)
        {
            if (viejo._collider != null)
            {
                PhysicsToGameObjects.Remove(viejo._collider.GetCollidableReference());
                viejo._collider.Remove();
                viejo._collider = null;
            }
        }

        _trees.Clear();
        _decor.Clear();
        _objetosEditor.Clear();
        _editorModificados.Clear();
        _sceneEditor.ClearRegistered();

        foreach (var item in nuevos)
        {
            if (item.Model is Tree arbol)
                _trees.Add(arbol);
            else
                _decor.Add(item.Model);

            RegistrarEnEditor(item.Model, item.Id);
            _editorModificados.Add(item.Model);
        }

        // Al iniciar todavía no existe la Simulation; LoadContent crea todo después.
        // Con Ctrl+L ya existe, por lo que reconstruimos inmediatamente los colliders.
        if (_simulation != null)
            SincronizarFisicaEditor();
    }

    // Importa los guardados de la versión anterior del editor, si existen.
    // Primero modifica las instancias base; luego reconstruye las copias.
    private void MigrarEscenaAnterior()
    {
        IReadOnlyList<SceneRepository.LegacyTransform> antiguos = _sceneRepository.ReadLegacy();
        foreach (var data in antiguos)
        {
            if (string.IsNullOrWhiteSpace(data.Id) || data.Position?.Length != 3)
                continue;
            if (_objetosEditor.TryGetValue(data.Id, out ModelInScene modelo))
                AplicarTransformacionAnterior(modelo, data);
        }

        // Algunos guardados pueden contener copias de otras copias.
        var pendientes = antiguos.Where(d => !_objetosEditor.ContainsKey(d.Id) &&
            !string.IsNullOrWhiteSpace(d.SourceId) && d.Position?.Length == 3).ToList();
        bool cambio;
        do
        {
            cambio = false;
            for (int i = pendientes.Count - 1; i >= 0; i--)
            {
                var data = pendientes[i];
                if (!_objetosEditor.ContainsKey(data.SourceId!)) continue;

                Vector3 pos = new(data.Position[0], data.Position[1], data.Position[2]);
                if (CrearCopiaEditor(data.SourceId!, data.Id, pos,
                        SceneRepository.LegacyRotation(data), data.Scale ?? 1f))
                {
                    pendientes.RemoveAt(i);
                    cambio = true;
                }
            }
        } while (cambio);

        if (antiguos.Count > 0)
            Console.WriteLine($"Se importó el escenario anterior a {_sceneRepository.FilePath}");
        if (pendientes.Count > 0)
            Console.WriteLine($"Advertencia: {pendientes.Count} copias antiguas no pudieron importarse.");
    }

    private static void AplicarTransformacionAnterior(ModelInScene modelo,
        SceneRepository.LegacyTransform data)
    {
        modelo._position = new Vector3(data.Position[0], data.Position[1], data.Position[2]);
        modelo._rotation = SceneRepository.LegacyRotation(data);
        if (data.Scale.HasValue && data.Scale.Value > 0f)
            modelo._scale = new Vector3(data.Scale.Value);
    }

    // Único lugar donde se conecta un ModelInScene con el editor.
    private void RegistrarEnEditor(ModelInScene modelo, string id)
    {
        _objetosEditor.Add(id, modelo);
        float radio = Math.Max(10f, Math.Max(modelo.Width, Math.Max(modelo.Height, modelo.Depth)) * 0.5f);

        _sceneEditor.Register(
            id: id,
            getPosition: () => modelo._position,
            setPosition: posicion => { modelo._position = posicion; _editorModificados.Add(modelo); },
            getRotation: () => modelo._rotation,
            setRotation: rotacion => { modelo._rotation = rotacion; _editorModificados.Add(modelo); },
            getScale: () => modelo._scale.X,
            setScale: escala => { modelo._scale = new Vector3(escala); _editorModificados.Add(modelo); },
            pickRadius: radio,
            getBounds: () =>
            {
                float r = Math.Max(10f, Math.Max(modelo.Width, Math.Max(modelo.Height, modelo.Depth)) * 0.5f);
                Vector3 extent = new Vector3(r);
                return new BoundingBox(modelo._position - extent, modelo._position + extent);
            },
            model: modelo
        );
    }

    // Duplicación real, a partir del tipo y la ruta del modelo seleccionado.
    private bool CrearCopiaEditor(string sourceId, string newId, Vector3 position, Matrix rotation, float scale)
    {
        if (!_objetosEditor.TryGetValue(sourceId, out ModelInScene original) ||
            _objetosEditor.ContainsKey(newId))
            return false;

        SceneRepository.SceneObject datos = SceneRepository.Capture(newId, original);
        datos.Position = new[] { position.X, position.Y, position.Z };
        Quaternion q = Quaternion.CreateFromRotationMatrix(rotation);
        datos.Rotation = new[] { q.X, q.Y, q.Z, q.W };
        datos.Scale = new[] { scale, scale, scale };
        ModelInScene copia = SceneRepository.Create(datos, Content, _basicRenderer);

        if (copia is Tree arbol)
            _trees.Add(arbol);
        else
            _decor.Add(copia);
        RegistrarEnEditor(copia, newId);
        _editorModificados.Add(copia);
        return true;
    }

    private void SincronizarFisicaEditor()
    {
        foreach (ModelInScene modelo in _editorModificados)
        {
            if (modelo._collider != null)
            {
                var referencia = modelo._collider.GetCollidableReference();
                PhysicsToGameObjects.Remove(referencia);
                modelo._collider.Remove();
                modelo._collider = null;
            }

            modelo.CreateCollider(_simulation, _bufferPool, _effect, GraphicsDevice);
            if (modelo._collider != null)
                PhysicsToGameObjects[modelo._collider.GetCollidableReference()] = modelo;

            if (modelo._hitbox == null)
                modelo.CreateHitbox(_debugEffect, GraphicsDevice);
        }
        _editorModificados.Clear();
    }

    /// <summary>
    ///     Se llama en cada frame.
    ///     Se debe escribir toda la logica de computo del modelo, asi como tambien verificar entradas del usuario y reacciones
    ///     ante ellas.
    /// </summary>

    protected override void Update(GameTime gameTime)
    {
        KeyboardState currentKeyboardState = Keyboard.GetState();
        MouseState currentMouseState = Mouse.GetState();

        bool escapePresionado =
            currentKeyboardState.IsKeyDown(Keys.Escape) &&
            _previousKeyboardState.IsKeyUp(Keys.Escape);

        switch (_gameState)
        {
            case GameState.MainMenu:
                IsMouseVisible = true;
                _mainMenu.Update();

                if (_mainMenu.StartGame)
                {
                    _mainMenu.StartGame = false;
                    _gameState = GameState.Playing;
                }
                else if (_mainMenu.LeaveGame)
                {
                    Exit();
                }
                break;

            case GameState.Paused:
                IsMouseVisible = true;
                _pauseMenu.Update();

                if (_pauseMenu.StartMenu)
                {
                    _pauseMenu.StartMenu = false;
                    _pauseMenu.ResumeGame = false;
                    _gameState = GameState.MainMenu;
                }
                else if (_pauseMenu.ResumeGame || escapePresionado)
                {
                    _pauseMenu.ResumeGame = false;
                    _pauseMenu.StartMenu = false;
                    _gameState = GameState.Playing;
                }
                break;

            case GameState.Playing:
                // SceneEditor controla la cámara libre únicamente en modo edición.
                bool estabaEditando = _sceneEditor.Enabled;
                _sceneEditor.Update(gameTime, _camera);

                if (!estabaEditando && _sceneEditor.Enabled)
                {
                    _cameraAntesDelEditor = _currentCamera;
                    _currentCamera = _camera;
                }
                else if (estabaEditando && !_sceneEditor.Enabled)
                {
                    SincronizarFisicaEditor();
                    _currentCamera = _cameraAntesDelEditor ?? _camera2;
                }

                if (_sceneEditor.Enabled)
                {
                    // Evitar que las entradas del editor muevan el tanque o la simulación.
                    IsMouseVisible = currentMouseState.RightButton != ButtonState.Pressed;
                    break;
                }

                IsMouseVisible = false;

                if (escapePresionado)
                {
                    _pauseMenu.StartMenu = false;
                    _pauseMenu.ResumeGame = false;
                    _gameState = GameState.Paused;
                    IsMouseVisible = true;
                    break;
                }

                if (currentKeyboardState.IsKeyDown(Keys.C) &&
                    _previousKeyboardState.IsKeyUp(Keys.C))
                {
                    _currentCamera = _currentCamera == _camera2 ? _camera : _camera2;
                }

                // motor suena mientras el tanque se mueve
                bool moviendose = currentKeyboardState.IsKeyDown(Keys.W) || currentKeyboardState.IsKeyDown(Keys.A) ||
                                  currentKeyboardState.IsKeyDown(Keys.S) || currentKeyboardState.IsKeyDown(Keys.D);
                if (moviendose)
                {
                    _engineSound.Play();
                }
                else
                {
                    _engineSound.Pause();
                }

                _tanks[0].Update(gameTime);

                _simulation.Timestep(1 / 60f);

                HashSet<(ModelInScene, ModelInScene)> parejasProcesadas = new HashSet<(ModelInScene, ModelInScene)>();

                while (TGCGame.PendingImpacts.TryDequeue(out ImpactData impact))
                {
                    ModelInScene modelA = impact.modelA;
                    ModelInScene modelB = impact.modelB;
                    var parejaId = modelA.GetHashCode() < modelB.GetHashCode()
                        ? (modelA, modelB)
                        : (modelB, modelA);

                    if (parejasProcesadas.Contains(parejaId))
                    {
                        continue;
                    }
                    parejasProcesadas.Add(parejaId);
                    modelA.CollidedWith(modelB, impact.collisionOffsetA, impact.positionA, impact.impactImpulse, impact.destructiveImpactImpulse, impact.normalFromAToB);
                    modelB.CollidedWith(modelA, impact.collisionOffsetB, impact.positionB, impact.impactImpulse, impact.destructiveImpactImpulse, -impact.normalFromAToB);
                }

                foreach (var tanque in _tanks)
                {
                    tanque.updateBodyPosition();

                }
                if (_tanks[0].ShouldFireMissile(gameTime, out Vector3 direction, out Vector3 position, out Matrix rotation))
                {
                    Missil missile = new Missil(
                        Content,
                        ContentFolder3D + "missile/source/rocket2",
                        position,
                        rotation,
                        new Vector3(4.2f),
                        _basicRenderer,
                        direction
                    );

                    Matrix cameraWorld = Matrix.Invert(_camera.View);

                    Quaternion cameraRotation = Quaternion.CreateFromRotationMatrix(cameraWorld);
                    Missil missile2 = new Missil(
                        Content,
                        ContentFolder3D + "missile/source/rocket2",
                        cameraWorld.Translation,
                        Matrix.CreateFromQuaternion(cameraRotation),
                        new Vector3(4.2f),
                        _basicRenderer,
                        Matrix.CreateFromQuaternion(cameraRotation).Forward
                    );

                    _missiles.Add(missile);
                    _missiles.Add(missile2);
                    _cannonSound.Play();
                }
                for (int i = _missiles.Count - 1; i >= 0; i--)
                {
                    Missil missil = _missiles[i];

                    if (missil._isDisabled)
                    {
                        _missiles.RemoveAt(i);
                        continue;
                    }

                    missil.Update(gameTime);

                    foreach (var lista in _modelosEnEscenario)
                    {
                        foreach (ModelInScene model in lista)
                        {
                            if (model.ShouldCollideWithMissile(missil))
                            {
                                model.CollidedWithMissil(missil);
                                missil.CollidedWithObject(model);
                            }
                        }
                    }
                }


                _camera.Update(gameTime);
                _camera2.Update(gameTime);

                break;
        }

        // el motor no suena en los menus ni en el editor
        if (_gameState != GameState.Playing || _sceneEditor.Enabled)
        {
            _engineSound.Pause();
        }

        _previousKeyboardState = currentKeyboardState;
        _previousMouseState = currentMouseState;

        base.Update(gameTime);
    }

    /// <summary>
    ///     Se llama cada vez que hay que refrescar la pantalla.
    ///     Escribir aqui el codigo referido al renderizado.
    /// </summary>
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.LightBlue);

        if (_gameState == GameState.MainMenu)
        {
            _mainMenu.Draw(_spriteBatch);
        }
        else
        {
            // El escenario sigue visible debajo del menú de pausa.
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            foreach (var lista in _modelosEnEscenario)
            {
                foreach (var modelo in lista)
                {
                    modelo.Draw(_currentCamera.View, _currentCamera.Projection);
                }
            }

            if (_gameState == GameState.Paused)
            {
                _pauseMenu.Draw(_spriteBatch);
            }
            else if (_sceneEditor.Enabled)
            {
                _sceneEditor.Draw(_currentCamera.View, _currentCamera.Projection);
            }
            else
            {
                _hud.Draw(_spriteBatch, _tanks[0], _tanks);
            }
        }

        base.Draw(gameTime);
    }

    /// <summary>
    ///     Libero los recursos que se cargaron en el juego.
    /// </summary>
    protected override void UnloadContent()
    {
        // Libero los recursos.
        _sceneEditor?.Dispose();
        _pixel?.Dispose();
        _spriteBatch?.Dispose();
        Content.Unload();

        base.UnloadContent();
    }
}
