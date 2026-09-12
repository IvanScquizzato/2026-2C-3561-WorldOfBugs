using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.Cameras;
using TGC.MonoGame.TP.Terrain;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Renderers;
using TGC.MonoGame.TP.Tanks;
using TGC.MonoGame.TP.Trees;
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
    private List<IEnumerable<ModelInScene>> _modelosEnEscenario = new List<IEnumerable<ModelInScene>>();
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

        _camera = new SimpleCamera(GraphicsDevice.Viewport.AspectRatio, Vector3.UnitY * 500, 400, 1f, 1, 20000);
        _modelosEnEscenario.Add(_tanks);
        _modelosEnEscenario.Add(_trees);
        _modelosEnEscenario.Add(_terrains);
        _currentCamera = _camera;
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
        _tanks.Add(new Tank(Content, ContentFolder3D + "Tanks/Panzer/Panzer", new Vector3(0, altura_tanque + 300, -300), Matrix.Identity, new Vector3(1f), _basicRenderer));
        _camera2 = new ThirdPersonCamera(_tanks[0], 1000f, 0.005f, GraphicsDevice.Viewport.AspectRatio, 500f, 400f, 1f, 20000f, GraphicsDevice);

        //cargo arbol
        _trees.Add(new Tree(Content, ContentFolder3D + "Tree/Tree", new Vector3(200, 500, 0), Matrix.Identity, new Vector3(150f), _basicRenderer));
        Random _random_X;
        Random _random_Z;
        for (int i = 0; i < 200; i++)
        {
            _random_X = new Random();
            _random_Z = new Random();
            float x = (float)(_random_X.NextDouble() * 13000 - 6000);
            float z = (float)(_random_Z.NextDouble() * 12000 - 6000);
            float y = _terrains[0].Height(x, z) - 10;
            _trees.Add(new Tree(Content, ContentFolder3D + "Tree/Tree", new Vector3(x, y, z), Matrix.Identity, new Vector3(150f), _basicRenderer));
        }
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
        base.Update(gameTime);
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