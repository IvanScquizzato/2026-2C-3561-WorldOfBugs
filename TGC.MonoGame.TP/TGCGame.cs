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

    private VertexBuffer cylinderVertexBuffer;
    private IndexBuffer cylinderIndexBuffer;
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
        _simulation = Simulation.Create(_bufferPool, new NarrowPhaseCallbacks(new SpringSettings(30, 1), 2f, 0.5f),
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
        // 1. Definir las formas (y corregí un pequeño bug donde repetías backTrackShape en fowardTrackIndex)
        var tankBodyShape = new Box(_tanks[0].Width * 0.58f, _tanks[0].Height * 0.3f, _tanks[0].Depth * 0.5f);
        var tankCenterShape = new Box(_tanks[0].Width * 0.9f, _tanks[0].Height * 0.1f, _tanks[0].Depth * 0.57f);
        var tankUpperShape = new Box(_tanks[0].Width * 0.9f, _tanks[0].Height * 0.18f, _tanks[0].Depth * 0.51f);
        var trackShape = new Box(_tanks[0].Width * 0.17f, _tanks[0].Height * 0.3f, _tanks[0].Depth * 0.37f);

        float trackRadius = _tanks[0].Height * 0.18f;
        float trackWidth = _tanks[0].Width * 0.17f;
        var trackWheelShape = new Cylinder(trackRadius, trackWidth);

        // 2. Distribuir la masa total del tanque entre sus componentes (ejemplo aproximado por volumen)
        float totalMass = _tanks[0]._mass;
        float bodyMass = totalMass * 0.40f;   // 40% al cuerpo
        float centerMass = totalMass * 0.20f; // 20% al centro
        float upperMass = totalMass * 0.10f;  // 10% a la torreta/parte superior
        float trackMass = totalMass * 0.10f;  // 10% a cada oruga principal (x2)
        float smallTrackMass = totalMass * 0.025f; // 2.5% a cada parte pequeña de oruga (x4)

        // 3. Construir el Compound
        using var compoundBuilder = new CompoundBuilder(_bufferPool, _simulation.Shapes, 10);

        compoundBuilder.Add(tankBodyShape, new RigidPose(new NumericVector3(0, -_tanks[0].Height * 0.2f, 0)), bodyMass);
        compoundBuilder.Add(tankCenterShape, new RigidPose(new NumericVector3(0, -_tanks[0].Height * 0.1f, -_tanks[0].Depth * 0.03f)), centerMass);
        compoundBuilder.Add(tankUpperShape, new RigidPose(new NumericVector3(0, _tanks[0].Height * 0.04f, -_tanks[0].Depth * 0.033f)), upperMass);

        // Orugas principales
        compoundBuilder.Add(trackShape, new RigidPose(new NumericVector3(-_tanks[0].Width * 0.39f, -_tanks[0].Height * 0.33f, 0)), trackMass);
        compoundBuilder.Add(trackShape, new RigidPose(new NumericVector3(_tanks[0].Width * 0.39f, -_tanks[0].Height * 0.33f, 0)), trackMass);

        var wheelRotation = System.Numerics.Quaternion.CreateFromAxisAngle(new NumericVector3(0, 0, 1), MathHelper.PiOver2);

        // Orugas traseras (Ahora son Cilindros redondeados)
        // Nota: Usamos la MISMA altura Y (-0.33f) que el centro para que encajen a la perfección.
        compoundBuilder.Add(trackWheelShape, new RigidPose(new NumericVector3(_tanks[0].Width * 0.39f, -_tanks[0].Height * 0.29f, -_tanks[0].Depth * 0.22f), wheelRotation), smallTrackMass);
        compoundBuilder.Add(trackWheelShape, new RigidPose(new NumericVector3(-_tanks[0].Width * 0.39f, -_tanks[0].Height * 0.29f, -_tanks[0].Depth * 0.22f), wheelRotation), smallTrackMass);

        // Orugas delanteras (Ahora son Cilindros redondeados)
        compoundBuilder.Add(trackWheelShape, new RigidPose(new NumericVector3(_tanks[0].Width * 0.39f, -_tanks[0].Height * 0.27f, _tanks[0].Depth * 0.20f), wheelRotation), smallTrackMass);
        compoundBuilder.Add(trackWheelShape, new RigidPose(new NumericVector3(-_tanks[0].Width * 0.39f, -_tanks[0].Height * 0.27f, _tanks[0].Depth * 0.20f), wheelRotation), smallTrackMass);

        // 4. USAR BUILD DYNAMIC COMPOUND!
        // Esto calculará la inercia perfecta combinada y te devolverá el nuevo centro de gravedad (centerOfMass)
        compoundBuilder.BuildDynamicCompound(out var compoundChildren, out var compoundInertia, out var centerOfMass);

        var compoundShape = new Compound(compoundChildren);
        var compoundIndex = _simulation.Shapes.Add(compoundShape);

        // 5. Compensar la posición inicial con el centro de masa calculado
        var basePosition = new NumericVector3(_tanks[0]._position.X, _tanks[0]._position.Y, _tanks[0]._position.Z);
        var bodyPose = new RigidPose(basePosition + centerOfMass); // ¡CRUCIAL!

        // 6. Crear el cuerpo dinámico usando la nueva inercia combinada
        var tankBoxHandle = _simulation.Bodies.Add(BodyDescription.CreateDynamic(
            bodyPose,
            compoundInertia, // Pasamos la inercia calculada por el builder, NO la del bodyShape individual
            new CollidableDescription(compoundIndex, 0.1f),
            new BodyActivityDescription(0.01f)
        ));

        _tanks[0]._bodyReference = _simulation.Bodies.GetBodyReference(tankBoxHandle);
        _tanks[0].centerOfMass = centerOfMass;
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

        CreateCylinderBuffers();

        base.LoadContent();

    }
    private void CreateCylinderBuffers()
    {
        int segments = 16;
        VertexPositionColor[] vertices = new VertexPositionColor[segments * 2];
        short[] indices = new short[segments * 6];

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * MathHelper.TwoPi;
            float x = (float)Math.Cos(angle) * 0.5f; // Radio de 0.5 (Diámetro de 1)
            float z = (float)Math.Sin(angle) * 0.5f;

            // Tapa superior (Y = 0.5)
            vertices[i] = new VertexPositionColor(new Vector3(x, 0.5f, z), Color.LimeGreen);
            // Tapa inferior (Y = -0.5)
            vertices[i + segments] = new VertexPositionColor(new Vector3(x, -0.5f, z), Color.LimeGreen);

            // Índices para dibujar las líneas
            int next = (i + 1) % segments;

            // Líneas del círculo superior
            indices[i * 6 + 0] = (short)i;
            indices[i * 6 + 1] = (short)next;

            // Líneas del círculo inferior
            indices[i * 6 + 2] = (short)(i + segments);
            indices[i * 6 + 3] = (short)(next + segments);

            // Líneas verticales conectando arriba y abajo
            indices[i * 6 + 4] = (short)i;
            indices[i * 6 + 5] = (short)(i + segments);
        }

        cylinderVertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), vertices.Length, BufferUsage.WriteOnly);
        cylinderVertexBuffer.SetData(vertices);

        cylinderIndexBuffer = new IndexBuffer(GraphicsDevice, typeof(short), indices.Length, BufferUsage.WriteOnly);
        cylinderIndexBuffer.SetData(indices);
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
            ref CompoundChild child = ref compoundShape.Children[i];

            Vector3 physicsScale = Vector3.One;
            bool isBox = false;
            bool isCylinder = false;

            // 1. Identificar el tipo de forma y calcular su escala
            if (child.ShapeIndex.Type == Box.Id)
            {
                ref Box boxShape = ref _simulation.Shapes.GetShape<Box>(child.ShapeIndex.Index);
                physicsScale = new Vector3(boxShape.HalfWidth * 2f, boxShape.HalfHeight * 2f, boxShape.HalfLength * 2f);
                isBox = true;
            }
            else if (child.ShapeIndex.Type == Cylinder.Id)
            {
                ref Cylinder cylinderShape = ref _simulation.Shapes.GetShape<Cylinder>(child.ShapeIndex.Index);
                // Bepu genera cilindros alineados al eje Y.
                // La escala X y Z corresponden al diámetro (Radio * 2), y la Y a la longitud.
                physicsScale = new Vector3(cylinderShape.Radius * 2f, cylinderShape.Length, cylinderShape.Radius * 2f);
                isCylinder = true;
            }

            // 2. Calcular posiciones globales
            // Asumiendo que tienes una conversión implícita de System.Numerics.Vector3 a Microsoft.Xna.Framework.Vector3
            Vector3 localPos = new Vector3(child.LocalPose.Position.X, child.LocalPose.Position.Y, child.LocalPose.Position.Z);
            Quaternion localRot = new Quaternion(child.LocalPose.Orientation.X, child.LocalPose.Orientation.Y, child.LocalPose.Orientation.Z, child.LocalPose.Orientation.W);

            Vector3 realPhysicsPos = physicsPos + Vector3.Transform(localPos, physicsRot);

            Matrix debugWorldMatrix = Matrix.CreateScale(physicsScale) *
                                      Matrix.CreateFromQuaternion(localRot) *
                                      Matrix.CreateFromQuaternion(physicsRot) *
                                      Matrix.CreateTranslation(realPhysicsPos);

            basicEffect.World = debugWorldMatrix;
            basicEffect.View = _currentCamera.View;
            basicEffect.Projection = _currentCamera.Projection;

            // 3. Dibujar la geometría correspondiente
            if (isBox)
            {
                GraphicsDevice.SetVertexBuffer(boxVertexBuffer);
                GraphicsDevice.Indices = boxIndexBuffer;

                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, 12); // 12 líneas del cubo
                }
            }
            else if (isCylinder)
            {
                GraphicsDevice.SetVertexBuffer(cylinderVertexBuffer);
                GraphicsDevice.Indices = cylinderIndexBuffer;

                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    // Si usas la función de abajo, genera 16 segmentos (16*3 = 48 líneas)
                    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, 48);
                }
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