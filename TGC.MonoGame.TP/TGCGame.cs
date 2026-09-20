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
namespace TGC.MonoGame.TP;

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

    private Simulation _simulation;
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

    private List<SimpleTerrain> _terrains = new List<SimpleTerrain>();

    private List<Tank> _tanks = new List<Tank>();
    private List<Tree> _trees = new List<Tree>();
    private List<Plant> _plants = new List<Plant>();
    private List<ModelInScene> _decor = new List<ModelInScene>();
    private List<IEnumerable<ModelInScene>> _modelosEnEscenario = new List<IEnumerable<ModelInScene>>();
    private List<BodyHandle> _tanksHandles = new List<BodyHandle>();
    private VertexBuffer boxVertexBuffer;
    private IndexBuffer boxIndexBuffer;
    private BasicEffect basicEffect;
    public static Dictionary<CollidableReference, object> PhysicsToGameObjects = new Dictionary<CollidableReference, object>();
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
        _tanks.Add(new Tank(Content, ContentFolder3D + "Tanks/Panzer/Panzer", new Vector3(0, altura_tanque + 300, -600), Matrix.Identity, new Vector3(0.5f), _basicRenderer, 4f, 6.5f, 11000f));
        _camera2 = new ThirdPersonCamera(_tanks[0], 1000f, 0.005f, GraphicsDevice.Viewport.AspectRatio, 500f, 400f, 1f, 20000f, GraphicsDevice);

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

        for (int i = 0; i < 100; i++)
        {
            _random_X = new Random();
            _random_Z = new Random();
            float x = (float)(_random_X.NextDouble() * 13000 - 6000);
            float z = (float)(_random_Z.NextDouble() * 12000 - 6000);
            float y = _terrains[0].Height(x, z) - 10;
            _decor.Add(new DeadBush(Content, ContentFolder3D + "DeadBush/source/Salix elaeagnos HD_Dead mat 50_LOD0", new Vector3(x, y, z), Matrix.Identity, new Vector3(1f), _basicRenderer));
        }

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

        base.LoadContent();

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
        _simulation = Simulation.Create(_bufferPool, new NarrowPhaseCallbacks(new SpringSettings(30, 1)),
        new PoseIntegratorCallbacks(new NumericVector3(0, -1000, 0)), new SolveDescription(8, 1));
        _tanks[0].LoadRaycastPoints(_simulation);

        //Añado terreno
        _bufferPool.Take(_terrains[0].triangles.Count, out Buffer<Triangle> triangles);
        for (int i = 0; i < _terrains[0].triangles.Count; i++)
        {
            triangles[i] = _terrains[0].triangles[i];
        }
        Mesh terrainMesh = new Mesh(triangles, NumericVector3.One, _bufferPool);
        TypedIndex shapeIndex = _simulation.Shapes.Add(terrainMesh);
        StaticDescription staticDescription = new StaticDescription(NumericVector3.Zero, shapeIndex);
        _simulation.Statics.Add(staticDescription);



        var tankBodyShape = new Box(_tanks[0].Width * 0.58f, _tanks[0].Height * 0.3f, _tanks[0].Depth * 0.5f);
        var tankInertia = tankBodyShape.ComputeInertia(_tanks[0]._mass);
        var tankIndex = _simulation.Shapes.Add(tankBodyShape);
        var tinyBox = new Box(1f, 1f, 1f);
        _simulation.Shapes.Add(tinyBox);

        var tankCenterShape = new Box(_tanks[0].Width * 0.9f, _tanks[0].Height * 0.1f, _tanks[0].Depth * 0.58f);
        var tankCenterIndex = _simulation.Shapes.Add(tankCenterShape);

        var tankUpperShape = new Box(_tanks[0].Width * 0.9f, _tanks[0].Height * 0.18f, _tanks[0].Depth * 0.51f);
        var tankUpperIndex = _simulation.Shapes.Add(tankUpperShape);

        var trackShape = new Box(_tanks[0].Width * 0.17f, 0.5f, _tanks[0].Depth * 0.36f);
        var trackIndex = _simulation.Shapes.Add(trackShape);

        using var compoundBuilder = new CompoundBuilder(_bufferPool, _simulation.Shapes, 4000);
        compoundBuilder.Add(tankBodyShape, new RigidPose(new NumericVector3(0, -_tanks[0].Height * 0.2f, 0)), _tanks[0]._mass);
        compoundBuilder.Add(tankCenterShape, new RigidPose(new NumericVector3(0, -_tanks[0].Height * 0.1f, 0)), 0f);
        compoundBuilder.Add(tankUpperShape, new RigidPose(new NumericVector3(0, _tanks[0].Height * 0.04f, -_tanks[0].Depth * 0.033f)), 0f);

        /*foreach (TankRaycast raycast in _tanks[0].raycastPointsLeft)
        {
            compoundBuilder.Add(tinyBox, new RigidPose(UtilsClass.ToNumericVector(raycast._positionLocal)), 0f);
        }*/
        /*
        compoundBuilder.Add(tinyBox, new RigidPose(new NumericVector3(_tanks[0].Width * 0.46f, -_tanks[0].Height * 0.49f, -_tanks[0].Depth * 0.18f) + UtilsClass.ToNumericVector(_tanks[0].Correction())), 1f);
        compoundBuilder.Add(tinyBox, new RigidPose(new NumericVector3(_tanks[0].Width * 0.31f, -_tanks[0].Height * 0.49f, -_tanks[0].Depth * 0.18f)), 1f);
        compoundBuilder.Add(tinyBox, new RigidPose(new NumericVector3(_tanks[0].Width * 0.31f, -_tanks[0].Height * 0.49f, _tanks[0].Depth * 0.18f)), 1f);
        compoundBuilder.Add(tinyBox, new RigidPose(new NumericVector3(_tanks[0].Width * 0.46f, -_tanks[0].Height * 0.49f, _tanks[0].Depth * 0.18f)), 1f);

        var rotacionFoward = System.Numerics.Quaternion.CreateFromAxisAngle(
            new NumericVector3(1, 0, 0),
            -MathHelper.Pi / 9.7f
        );

        compoundBuilder.Add(tinyBox, new RigidPose(new NumericVector3(_tanks[0].Width * 0.46f, -_tanks[0].Height * 0.47f, _tanks[0].Depth * 0.2f), rotacionFoward), 1f);
        compoundBuilder.Add(tinyBox, new RigidPose(new NumericVector3(_tanks[0].Width * 0.31f, -_tanks[0].Height * 0.47f, _tanks[0].Depth * 0.2f), rotacionFoward), 1f);

        compoundBuilder.Add(tinyBox, new RigidPose(new NumericVector3(_tanks[0].Width * 0.46f, -_tanks[0].Height * 0.47f, _tanks[0].Depth * 0.2f) + NumericVector3.Transform(new NumericVector3(0, 0, _tanks[0].Depth * 0.06f), rotacionFoward), rotacionFoward), 1f);
        compoundBuilder.Add(tinyBox, new RigidPose(new NumericVector3(_tanks[0].Width * 0.31f, -_tanks[0].Height * 0.47f, _tanks[0].Depth * 0.2f) + NumericVector3.Transform(new NumericVector3(0, 0, _tanks[0].Depth * 0.06f), rotacionFoward), rotacionFoward), 1f);

        var rotacionBack = System.Numerics.Quaternion.CreateFromAxisAngle(
            new NumericVector3(1, 0, 0),
            MathHelper.Pi / 13f
        );
        compoundBuilder.Add(tinyBox, new RigidPose(new NumericVector3(_tanks[0].Width * 0.46f, -_tanks[0].Height * 0.48f, -_tanks[0].Depth * 0.2f), rotacionBack), 1f);
        compoundBuilder.Add(tinyBox, new RigidPose(new NumericVector3(_tanks[0].Width * 0.31f, -_tanks[0].Height * 0.48f, -_tanks[0].Depth * 0.2f), rotacionBack), 1f);

        compoundBuilder.Add(tinyBox, new RigidPose(new NumericVector3(_tanks[0].Width * 0.46f, -_tanks[0].Height * 0.48f, -_tanks[0].Depth * 0.2f) + NumericVector3.Transform(new NumericVector3(0, 0, -_tanks[0].Depth * 0.07f), rotacionBack), rotacionBack), 1f);
        compoundBuilder.Add(tinyBox, new RigidPose(new NumericVector3(_tanks[0].Width * 0.31f, -_tanks[0].Height * 0.48f, -_tanks[0].Depth * 0.2f) + NumericVector3.Transform(new NumericVector3(0, 0, -_tanks[0].Depth * 0.07f), rotacionBack), rotacionBack), 1f);
*/
        /*
        var leftTrackOffset = new NumericVector3(-_tanks[0].Width * 0.39f, -_tanks[0].Height * 0.5f, -_tanks[0].Depth * 0.0005f);
        compoundBuilder.Add(trackShape, new RigidPose(leftTrackOffset), 0.1f);

        var rightTrackOffset = new NumericVector3(_tanks[0].Width * 0.39f, -_tanks[0].Height * 0.5f, -_tanks[0].Depth * 0.0005f);
        compoundBuilder.Add(trackShape, new RigidPose(rightTrackOffset), 0.1f);

        var fowardTrackShape = new Box(_tanks[0].Width * 0.17f, 0.5f, _tanks[0].Depth * 0.09f);

        var leftTrackFowardOffset = new NumericVector3(-_tanks[0].Width * 0.39f, -_tanks[0].Height * 0.45f, _tanks[0].Depth * 0.22f);
        var leftTrackFowardRotation = System.Numerics.Quaternion.CreateFromAxisAngle(
            new NumericVector3(1, 0, 0),
            -MathHelper.Pi / 10f
        );
        compoundBuilder.Add(fowardTrackShape, new RigidPose(leftTrackFowardOffset, leftTrackFowardRotation), 0.1f);

        var rightTrackFowardOffset = new NumericVector3(_tanks[0].Width * 0.39f, -_tanks[0].Height * 0.45f, _tanks[0].Depth * 0.22f);
        var rightTrackFowardRotation = System.Numerics.Quaternion.CreateFromAxisAngle(
            new NumericVector3(1, 0, 0),
            -MathHelper.Pi / 10f
        );
        compoundBuilder.Add(fowardTrackShape, new RigidPose(rightTrackFowardOffset, rightTrackFowardRotation), 0.1f);
        */
        compoundBuilder.BuildKinematicCompound(out var compoundChildren);
        var compoundShape = new Compound(compoundChildren);
        var compoundIndex = _simulation.Shapes.Add(compoundShape);
        var tankBoxHandle = _simulation.Bodies.Add(BodyDescription.CreateDynamic(
            new NumericVector3(_tanks[0]._position.X, _tanks[0]._position.Y, _tanks[0]._position.Z),
            tankInertia,
            new CollidableDescription(compoundIndex, 0.1f),
            new BodyActivityDescription(0.01f)
        ));
        _tanks[0]._bodyReference = _simulation.Bodies.GetBodyReference(tankBoxHandle);
        var tankCollidableRef = new CollidableReference(CollidableMobility.Dynamic, tankBoxHandle);
        PhysicsToGameObjects.Add(tankCollidableRef, _tanks[0]);


        basicEffect = new BasicEffect(GraphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false
        };

        // 2. Definir los 8 vértices del cubo
        VertexPositionColor[] vertices = new VertexPositionColor[8];
        Vector3[] corners = new Vector3[]
        {
            new Vector3(-0.5f,  0.5f, -0.5f), // 0: Arriba, Izquierda, Atrás
            new Vector3( 0.5f,  0.5f, -0.5f), // 1: Arriba, Derecha, Atrás
            new Vector3( 0.5f,  0.5f,  0.5f), // 2: Arriba, Derecha, Frente
            new Vector3(-0.5f,  0.5f,  0.5f), // 3: Arriba, Izquierda, Frente
            new Vector3(-0.5f, -0.5f, -0.5f), // 4: Abajo, Izquierda, Atrás
            new Vector3( 0.5f, -0.5f, -0.5f), // 5: Abajo, Derecha, Atrás
            new Vector3( 0.5f, -0.5f,  0.5f), // 6: Abajo, Derecha, Frente
            new Vector3(-0.5f, -0.5f,  0.5f)  // 7: Abajo, Izquierda, Frente
        };
        Color boxColor = Color.LimeGreen;
        for (int i = 0; i < 8; i++)
            vertices[i] = new VertexPositionColor(corners[i], boxColor);

        boxVertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), 8, BufferUsage.WriteOnly);
        boxVertexBuffer.SetData(vertices);

        // 3. Definir los índices para las 12 líneas (24 índices en total)
        short[] indices = new short[]
        {
            // Cuadrado superior
            0, 1, 1, 2, 2, 3, 3, 0,
            // Cuadrado inferior
            4, 5, 5, 6, 6, 7, 7, 4,
            // Pilares verticales conectando arriba y abajo
            0, 4, 1, 5, 2, 6, 3, 7
        };

        boxIndexBuffer = new IndexBuffer(GraphicsDevice, typeof(short), 24, BufferUsage.WriteOnly);
        boxIndexBuffer.SetData(indices);

        base.LoadContent();

    }
    /// <summary>
    ///     Se llama en cada frame.
    ///     Se debe escribir toda la logica de computo del modelo, asi como tambien verificar entradas del usuario y reacciones
    ///     ante ellas.
    /// </summary>
    protected override void Update(GameTime gameTime)
    {
        // Aca deberiamos poner toda la logica de actualizacion del juego.
        _camera.Update(gameTime);
        _camera2.Update(gameTime);
        KeyboardState currentKeyboardState = Keyboard.GetState();
        if (currentKeyboardState.IsKeyDown(Keys.C) && _previousKeyboardState.IsKeyUp(Keys.C))
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
        // Capturar Input teclado
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            //Salgo del juego.
            Exit();
        }
        _tanks[0].Update(gameTime);

        _previousKeyboardState = currentKeyboardState;

        _simulation.Timestep(1 / 60f);

        _tanks[0].updateBodyPosition();
        base.Update(gameTime);
    }
    private void RenderTank(Tank tank)
    {

        TypedIndex shapeIndex = tank._bodyReference.Collidable.Shape;

        ref Compound compoundShape = ref _simulation.Shapes.GetShape<Compound>(shapeIndex.Index);

        var bepuPose = tank._bodyReference.Pose;
        Vector3 physicsPos = new Vector3(bepuPose.Position.X, bepuPose.Position.Y, bepuPose.Position.Z);
        Quaternion physicsRot = new Quaternion(bepuPose.Orientation.X, bepuPose.Orientation.Y, bepuPose.Orientation.Z, bepuPose.Orientation.W);


        for (var i = 0; i < compoundShape.Children.Length; i++)
        {
            ref CompoundChild chassisChild = ref compoundShape.Children[i];
            ref Box chassisBox = ref _simulation.Shapes.GetShape<Box>(chassisChild.ShapeIndex.Index);
            float realWidth = chassisBox.HalfWidth * 2f;
            float realHeight = chassisBox.HalfHeight * 2f;
            float realDepth = chassisBox.HalfLength * 2f;
            Vector3 physicsScale = new Vector3(realWidth, realHeight, realDepth);
            Vector3 realPhysicsPos = physicsPos + Vector3.Transform(chassisChild.LocalPose.Position, physicsRot);
            Quaternion childPhysicsRotation = chassisChild.LocalPose.Orientation;
            Matrix debugWorldMatrix = Matrix.CreateScale(physicsScale) *
                                    Matrix.CreateFromQuaternion(childPhysicsRotation) *
                                   Matrix.CreateFromQuaternion(physicsRot) *
                                  Matrix.CreateTranslation(realPhysicsPos);

            basicEffect.World = debugWorldMatrix;
            basicEffect.View = _currentCamera.View;
            basicEffect.Projection = _currentCamera.Projection;

            GraphicsDevice.SetVertexBuffer(boxVertexBuffer);
            GraphicsDevice.Indices = boxIndexBuffer;

            // Aplicamos el efecto de depuración (NO _effect)
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawIndexedPrimitives(
                    primitiveType: PrimitiveType.LineList,
                    baseVertex: 0,
                    startIndex: 0,
                    primitiveCount: 12
                );
            }
        }
        //bepuPose = tank._bodyReference.Pose;
        //physicsPos = new Vector3(bepuPose.Position.X, bepuPose.Position.Y, bepuPose.Position.Z);
        //physicsRot = new Quaternion(bepuPose.Orientation.X, bepuPose.Orientation.Y, bepuPose.Orientation.Z, bepuPose.Orientation.W);

        // 2. Crear la matriz base del tanque
        Matrix tankWorldMatrix = _tanks[0]._world;

        GraphicsDevice.SetVertexBuffer(boxVertexBuffer);
        GraphicsDevice.Indices = boxIndexBuffer;

        // 3. Juntar ambas listas (asumo que tenés raycastPointsRight también)
        var allRaycasts = tank.raycastPointsLeft.Concat(tank.raycastPointsRight);

        foreach (TankRaycast raycast in allRaycasts)
        {
            // 4. Transformar la posición local del raycast a la posición global actual
            Vector3 globalRaycastPos = Vector3.Transform(raycast._positionLocal, tankWorldMatrix);

            // 5. Crear la matriz del cubito (escala de 1x1x1 como era tu tinyBox original)
            // Nota: Podés achicar el CreateScale(0.2f) si el cubo de 1x1x1 es muy grande visualmente
            Matrix debugWorldMatrix = Matrix.CreateScale(1f) * Matrix.CreateTranslation(globalRaycastPos);

            basicEffect.World = debugWorldMatrix;
            basicEffect.View = _currentCamera.View;
            basicEffect.Projection = _currentCamera.Projection;

            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawIndexedPrimitives(
                    primitiveType: PrimitiveType.LineList,
                    baseVertex: 0,
                    startIndex: 0,
                    primitiveCount: 12
                );
            }
        }

    }
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
        RenderTank(_tanks[0]);
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