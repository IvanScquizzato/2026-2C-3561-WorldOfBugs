using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
        // Aca es donde deberiamos cargar todos los contenido necesarios antes de iniciar el juego.
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Cargo un efecto basico propio declarado en el Content pipeline.
        // En el juego no pueden usar BasicEffect de MG, deben usar siempre efectos propios.
        _effect = Content.Load<Effect>(ContentFolderEffects + "BasicShader");
        _basicRenderer = new BasicRenderer(GraphicsDevice, _effect);

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
        Random _rng = new Random();

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
            _random_X = new Random();
            _random_Z = new Random();
            float x = (float)(_random_X.NextDouble() * 13000 - 6000);
            float z = (float)(_random_Z.NextDouble() * 12000 - 6000);
            float y = _terrains[0].Height(x, z) - 10;
            _decor.Add(new Fence(Content, ContentFolder3D + "Rocks/source/stone_fence_old_low", new Vector3(x, y, z), Matrix.Identity, new Vector3(1f), _basicRenderer));
        }

        for (int i = 0; i < 10; i++)
        {
            _random_X = new Random();
            _random_Z = new Random();
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
            _random_X = new Random();
            _random_Z = new Random();
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
            }
        }

        // hitbox debug
        _debugEffect = Content.Load<Effect>(
            ContentFolderEffects + "DebugShader"
        );

        base.LoadContent();

    }

    /// <summary>
    ///     Se llama en cada frame.
    ///     Se debe escribir toda la logica de computo del modelo, asi como tambien verificar entradas del usuario y reacciones
    ///     ante ellas.
    /// </summary>

    protected override void Update(GameTime gameTime)
    {
        KeyboardState currentKeyboardState =
            Keyboard.GetState();

        MouseState currentMouseState =
            Mouse.GetState();

        if (
            currentKeyboardState.IsKeyDown(Keys.C) &&
            _previousKeyboardState.IsKeyUp(Keys.C)
        )
        {
            if (_currentCamera == _camera2)
            {
                _currentCamera = _camera;
            }
            else
            {
                _currentCamera = _camera2;
            }
        }

        if (currentKeyboardState.IsKeyDown(Keys.Escape))
        {
            Exit();
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


        if (
            currentKeyboardState.IsKeyDown(Keys.X) &&
            _previousKeyboardState.IsKeyUp(Keys.X)
        )
        {
            FireMissile();
        }


        UpdateMissiles(gameTime);


        _camera.Update(gameTime);
        _camera2.Update(gameTime);

        _previousKeyboardState =
            currentKeyboardState;

        _previousMouseState =
            currentMouseState;


        base.Update(gameTime);
    }

    private void FireMissile()
    {
        Tank tank = _tanks[0];

        Vector3 direction =
            tank.GetCannonDirection();

        Vector3 missilePosition =
            tank.GetCannonMuzzlePosition();

        Matrix missileRotation =
            tank.GetCannonRotation();

        Missil missile = new Missil(
            Content,
            ContentFolder3D + "missile/source/rocket2",
            missilePosition,
            missileRotation,
            new Vector3(4.2f),
            _basicRenderer,
            direction
        );

        _missiles.Add(missile);
    }

    private bool MissileCollided(Missil missile)
    {
        // -----------------------------
        // Colisión contra terreno
        // -----------------------------

        float terrainHeight = _terrains[0].Height(
            missile._position.X,
            missile._position.Z
        );

        const float groundTolerance = 30f;

        if (missile._position.Y <= terrainHeight + groundTolerance)
        {
            return true;
        }


        // -----------------------------
        // Colisión contra hitboxes
        // -----------------------------

        foreach (var lista in _modelosEnEscenario)
        {
            foreach (var modelo in lista)
            {
                if (modelo == missile)
                {
                    continue;
                }

                if (modelo.ContainsPoint(missile._position))
                {
                    return true;
                }
            }
        }


        return false;
    }

    private void UpdateMissiles(GameTime gameTime)
    {
        float deltaTime =
            (float)gameTime.ElapsedGameTime.TotalSeconds;

        for (int i = _missiles.Count - 1; i >= 0; i--)
        {
            Missil missile = _missiles[i];

            missile._position +=
                missile.Direction *
                MissileSpeed *
                deltaTime;

            if (MissileCollided(missile))
            {
                _missiles.RemoveAt(i);
            }
        }
    }

    private void DrawBoxHitbox(
        ModelInScene model,
        BoxHitbox hitbox
    )
    {
        Vector3[] corners =
            hitbox.GetWorldCorners(model._world);

        VertexPosition[] vertices =
        {
                new VertexPosition(corners[0]),
                new VertexPosition(corners[1]),

                new VertexPosition(corners[1]),
                new VertexPosition(corners[2]),

                new VertexPosition(corners[2]),
                new VertexPosition(corners[3]),

                new VertexPosition(corners[3]),
                new VertexPosition(corners[0]),

                new VertexPosition(corners[4]),
                new VertexPosition(corners[5]),

                new VertexPosition(corners[5]),
                new VertexPosition(corners[6]),

                new VertexPosition(corners[6]),
                new VertexPosition(corners[7]),

                new VertexPosition(corners[7]),
                new VertexPosition(corners[4]),

                new VertexPosition(corners[0]),
                new VertexPosition(corners[4]),

                new VertexPosition(corners[1]),
                new VertexPosition(corners[5]),

                new VertexPosition(corners[2]),
                new VertexPosition(corners[6]),

                new VertexPosition(corners[3]),
                new VertexPosition(corners[7])
            };

        _debugEffect.Parameters["World"]
            .SetValue(Matrix.Identity);

        _debugEffect.Parameters["View"]
            .SetValue(_currentCamera.View);

        _debugEffect.Parameters["Projection"]
            .SetValue(_currentCamera.Projection);

        _debugEffect.Parameters["DebugColor"]
            .SetValue(Color.Red.ToVector4());

        foreach (
            EffectPass pass
            in _debugEffect.CurrentTechnique.Passes
        )
        {
            pass.Apply();

            GraphicsDevice.DrawUserPrimitives(
                PrimitiveType.LineList,
                vertices,
                0,
                vertices.Length / 2
            );
        }
    }
    /*
        private void RenderTank(Tank tank)
        {

            // 2. Crear la matriz base del tanque
            Matrix tankWorldMatrix = _tanks[0]._world;

            // 3. Juntar ambas listas (asumo que tenés raycastPointsRight también)
            var allRaycasts = tank.raycastPointsLeft.Concat(tank.raycastPointsRight);

            BoxRenderer boxRenderer = new BoxRenderer(GraphicsDevice, _effect);
            foreach (TankRaycast raycast in allRaycasts)
            {
                boxRenderer.DrawWithWorld(raycast.DirectionGlobal(_tanks[0])* raycast._positionLocal * tankWorldMatrix, _currentCamera.View, _currentCamera.Projection,)
            }  

        }
        */
    /// <summary>
    ///     Se llama cada vez que hay que refrescar la pantalla.
    ///     Escribir aqui el codigo referido al renderizado.
    /// </summary>
    protected override void Draw(GameTime gameTime)
    {

        // Aca deberiamos poner toda la logia de renderizado del juego.
        GraphicsDevice.Clear(Color.Black);
        foreach (var lista in _modelosEnEscenario)
        {
            foreach (var modelo in lista)
            {
                modelo.Draw(_currentCamera.View, _currentCamera.Projection);
            }
        }

        // Dibujar hitbox del tanque
        // Dibujar todas las BoxHitbox del escenario
        foreach (var lista in _modelosEnEscenario)
        {
            foreach (var modelo in lista)
            {
                if (modelo._hitbox is BoxHitbox boxHitbox)
                {
                    DrawBoxHitbox(
                        modelo,
                        boxHitbox
                    );
                }
            }
        }

        //RenderTank(_tanks[0]);
    }

    /// <summary>
    ///     Libero los recursos que se cargaron en el juego.
    /// </summary>
    protected override void UnloadContent()
    {
        // Libero los recursos.
        Content.Unload();

        base.UnloadContent();
    }
}